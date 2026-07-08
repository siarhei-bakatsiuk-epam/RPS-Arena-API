# RPS Arena API — Implementation Plan

Source requirements: [`v3.task 1.md`](./v3.task%201.md), diagrams: [`v3.task.diagram 1.md`](./v3.task.diagram%201.md).
Requirement coverage is verified in [`requirements-coverage.md`](./requirements-coverage.md).

## 1. Goals and scope

Build two independent .NET 9 microservices:

- **MatchService** — players CRUD, match recording/query, publishes `MatchRecorded`.
- **LeaderboardService** — consumes `MatchRecorded`, maintains per-player stats, serves leaderboard API.

Non-functional targets: idempotent match recording, race-safe stats updates, one-command startup via `docker-compose up --build`, automatic EF Core migrations, Swagger exposed, unit + integration tests, health checks.

## 2. Key technology decisions

| Area | Decision | Rationale |
|---|---|---|
| Runtime | .NET 9 (ASP.NET Core Minimal APIs or Controllers — Controllers chosen for clearer Swagger grouping) | Required by spec |
| ORM | EF Core 9 + Npgsql | Required by spec |
| DB | PostgreSQL 16 — **one container, two databases** (`match_db`, `leaderboard_db`) | Service isolation without extra containers; spec allows either |
| Messaging | **RabbitMQ + MassTransit** | Spec allows RabbitMQ/Kafka; MassTransit gives retry, outbox/inbox, test harness out of the box |
| CQRS | MediatR 12, one handler class per command/query | Required by spec |
| Validation | FluentValidation via MediatR `ValidationBehavior` pipeline | Required by spec |
| Concurrency | **Optimistic concurrency** (PostgreSQL `xmin` as concurrency token) + retry on conflict | Matches `rowVersion` in ER diagram; non-blocking under load (see §7.2) |
| Reliability | MassTransit **Transactional Outbox** (EF Core) on publish side, **Inbox/dedup table** on consume side | Atomic "save match + publish event"; exactly-once effect on consumer |
| Tests | xUnit + FluentAssertions + NSubstitute; integration via Testcontainers (PostgreSQL, RabbitMQ) + MassTransit TestHarness | Bonus criteria; senior-level signal |
| API docs | OpenAPI (Swashbuckle) + Swagger UI in all environments | Spec requires Swagger reachable |
| Health | AspNetCore.HealthChecks (Npgsql, RabbitMQ): `/health/live`, `/health/ready` | Bonus criteria; used by compose `depends_on` |

## 3. Solution structure (Clean Architecture)

Single solution, strict layer dependencies `Infrastructure → Domain ← Application ← API`:

```
RpsArena.sln
├── docker-compose.yml
├── src/
│   ├── Shared/
│   │   └── RpsArena.Contracts/            # MatchRecorded event (the only shared code)
│   ├── MatchService/
│   │   ├── RpsArena.Match.Domain/         # Entities, domain rules, repository interfaces. No dependencies.
│   │   ├── RpsArena.Match.Application/    # Commands/Queries/Handlers/Validators/DTOs. Depends on Domain.
│   │   ├── RpsArena.Match.Infrastructure/ # EF Core DbContext, migrations, repositories, MassTransit. Depends on Domain (+Application ports).
│   │   └── RpsArena.Match.Api/            # Controllers, DI composition root, middleware. Depends on Application (+Infrastructure for DI only).
│   └── LeaderboardService/
│       ├── RpsArena.Leaderboard.Domain/
│       ├── RpsArena.Leaderboard.Application/
│       ├── RpsArena.Leaderboard.Infrastructure/   # includes MatchRecordedConsumer
│       └── RpsArena.Leaderboard.Api/
└── tests/
    ├── RpsArena.Match.UnitTests/
    ├── RpsArena.Leaderboard.UnitTests/
    └── RpsArena.IntegrationTests/
```

Notes:
- `RpsArena.Contracts` holds only the `MatchRecorded` record — services stay independently deployable; contract is versioned additively.
- Domain has zero package references (no EF, no MediatR). Application defines ports (`IMatchRepository`, `IPlayerStatsRepository`, `IUnitOfWork`); Infrastructure implements them.
- Cross-cutting MediatR behaviors in Application: `ValidationBehavior`, `LoggingBehavior`.

## 4. Data model

### 4.1 MatchService (`match_db`)

**players**

| Column | Type | Constraints |
|---|---|---|
| id | uuid | PK |
| username | text | unique index, 3–32 chars |
| email | text | unique index (case-insensitive, `citext` or lowered), valid format |
| created_at | timestamptz | default now (UTC) |

**matches**

| Column | Type | Constraints |
|---|---|---|
| id | uuid | PK |
| player_one_id | uuid | FK → players, `ON DELETE RESTRICT` |
| player_two_id | uuid | FK → players, `ON DELETE RESTRICT` |
| player_one_score | int | CHECK >= 0 |
| player_two_score | int | CHECK >= 0 |
| played_at | timestamptz | indexed |
| idempotency_key | uuid | **unique index** (see §7.1) |
| CHECK | | `player_one_id <> player_two_id` |

Indexes: `(player_one_id)`, `(player_two_id)`, `(played_at)` for filter queries.
Player deletion policy: `DELETE RESTRICT` — a player with recorded matches cannot be deleted (returns 409); keeps match history consistent. Documented as a deliberate trade-off vs soft delete.

Plus MassTransit outbox tables (`OutboxMessage`, `OutboxState`, `InboxState`) added by `AddTransactionalOutbox` migrations.

### 4.2 LeaderboardService (`leaderboard_db`)

**player_stats**

| Column | Type | Notes |
|---|---|---|
| player_id | uuid | PK |
| username | text | denormalized from event (no cross-service DB reads) |
| wins / losses / draws | int | |
| total_matches | int | |
| match_points | int | 3/1/0 scheme |
| total_score | int | sum of rounds won |
| xmin | system | EF concurrency token (fulfils the diagram's `rowVersion`) |

`rank` is **not stored** — computed at query time with `DENSE_RANK() OVER (ORDER BY match_points DESC, total_score DESC)`. Storing rank would require rewriting all rows on every match; computing it keeps writes O(1). Deviation from ER diagram, justified (spec explicitly allows model changes with rationale).

**processed_messages** — consumer dedup: `message_id uuid PK`, `processed_at`. Inserted in the same transaction as the stats update (see §7.3).

### 4.3 Contract

```csharp
public sealed record MatchRecorded(
    Guid MatchId,
    Guid PlayerOneId, string PlayerOneUsername,
    Guid PlayerTwoId, string PlayerTwoUsername,
    int PlayerOneScore, int PlayerTwoScore,
    DateTime PlayedAt);
```

Usernames ride on the event so LeaderboardService never queries MatchService's DB (no shared-database coupling).

## 5. API design

### 5.1 MatchService

| Endpoint | Purpose | Responses |
|---|---|---|
| `POST /api/players` | Register player | 201 + Location; 409 on duplicate username/email; 400 validation |
| `GET /api/players/{id}` | Profile | 200 / 404 |
| `GET /api/players?page=&pageSize=` | List players (paged) | 200 |
| `PUT /api/players/{id}` | Update username/email | 200 / 404 / 409 / 400 |
| `DELETE /api/players/{id}` | Delete | 204 / 404 / 409 (has matches) |
| `POST /api/matches` | Record match result | 201; **200 on idempotent replay**; 400; 404 (unknown player); 409 (key reuse with different payload); 422 (self-play) |
| `GET /api/matches/{id}` | Match by id | 200 / 404 |
| `GET /api/matches?playerId=&from=&to=&page=&pageSize=` | Filtered, paged list | 200 |
| `GET /api/players/{id}/matches?page=&pageSize=` | Player match history | 200 / 404 |

`POST /api/matches` body includes client-supplied `idempotencyKey` (also accepted via `Idempotency-Key` header). Paged responses return `{ items, page, pageSize, totalCount }`.

Validation rules (FluentValidation):
- Players: username 3–32 chars alphanumeric/underscore; valid email; both required.
- Matches: both player ids present and distinct; scores `>= 0` integers; `playedAt` not in the future; draws allowed; both players must exist (handler check → 404).
- Queries: `page >= 1`, `1 <= pageSize <= 100`, `from <= to`.

Error shape: RFC 7807 `ProblemDetails` everywhere (exception-handling middleware maps `ValidationException` → 400, `NotFoundException` → 404, `ConflictException` → 409).

### 5.2 LeaderboardService

| Endpoint | Purpose | Responses |
|---|---|---|
| `GET /api/leaderboard?sortBy=matchPoints&top=10` | Top-N players | 200; 400 on bad sortBy/top |
| `GET /api/leaderboard/players/{playerId}` | Single player stats (incl. rank) | 200 / 404 |

`sortBy ∈ {wins, draws, losses, matchPoints, totalScore}` (default `matchPoints`), `top` default 10, max 100. Tie-break: `totalScore` desc, then `username` asc — deterministic ordering. Response model matches the spec's player-stats JSON including computed `rank`.

Scoring scheme kept as specified: win 3 / draw 1 / loss 0 (football-style; rewards decisive play; no change needed).

## 6. CQRS breakdown

**MatchService — Commands:** `RegisterPlayer`, `UpdatePlayer`, `DeletePlayer`, `RecordMatch`.
**MatchService — Queries:** `GetPlayerById`, `GetPlayers`, `GetMatchById`, `GetMatches` (filters), `GetPlayerMatches`.
**LeaderboardService — Command:** `ApplyMatchResult` (invoked by the MassTransit consumer).
**LeaderboardService — Queries:** `GetLeaderboard`, `GetPlayerStats`.

Each command/query: `record` + `Handler` + `Validator` in one feature folder (vertical slice inside the Application layer). All traffic goes through `IMediator`; controllers contain no logic.

## 7. Collision protection (core requirement)

### 7.1 Idempotent match recording

- Client sends `idempotencyKey` (uuid) with `POST /api/matches`; unique index on `matches.idempotency_key`.
- Handler flow: look up by key → if found and payload matches, return existing match with 200 (no duplicate, no re-publish); if found and payload differs → 409.
- Insert races (two identical concurrent requests) are resolved by the unique index: catch `UniqueViolation` (PG error 23505), re-read, return existing — no double insert, and because publish goes through the outbox in the same transaction, the losing transaction also rolls back its event.
- Fallback when client omits the key: deterministic key = SHA-256 over `(playerOneId, playerTwoId, scores, playedAt)` — repeated identical submissions still dedupe.

### 7.2 Race conditions on stats updates

- `player_stats` uses PostgreSQL `xmin` as EF Core concurrency token.
- Consumer flow: read stats row → apply deltas in domain code → `SaveChanges`; on `DbUpdateConcurrencyException` the consumer **throws**, and MassTransit retry policy (`immediate(3)` + exponential backoff, then error queue) redelivers — the retry re-reads fresh state, so no update is lost.
- First event for a player: `INSERT`; concurrent first-inserts resolved via PK unique violation → retry path updates instead.
- A match touches two players → two stat rows updated in **one transaction** together with the dedup insert (§7.3), so partial application is impossible.

### 7.3 Consumer idempotency (at-least-once delivery)

RabbitMQ redelivers on failure; without dedup a redelivered `MatchRecorded` would double-count. Consumer wraps in one DB transaction: `INSERT INTO processed_messages(message_id)` (PK violation → already processed → ack and skip) + both stat updates + commit. Exactly-once *effect* on top of at-least-once delivery.

### 7.4 Publisher reliability (outbox)

`MatchRecorded` publish must not be lost if the broker is down, and must not be sent if the DB commit fails. MassTransit EF Core Transactional Outbox writes the event into `match_db` in the same transaction as the match row; a delivery service relays it to RabbitMQ with retries. Broker topology: MassTransit publishes to a fanout exchange; LeaderboardService binds a durable queue `leaderboard-match-recorded`.

## 8. Infrastructure — Docker Compose

Services: `matchservice` (8080→5001), `leaderboardservice` (8080→5002), `postgres` (5432, init script creates `match_db` + `leaderboard_db`), `rabbitmq` (management image, 5672 + UI 15672).

- Healthchecks: `pg_isready`, `rabbitmq-diagnostics ping`; app services use `depends_on: condition: service_healthy`.
- **Migrations**: each API applies `Database.Migrate()` on startup behind a Polly retry (waits for PG) — functionally equivalent to the spec's entrypoint `dotnet ef database update`, with no extra init container; documented in README. (If the reviewer insists on the literal mechanism, an optional `migrator` init-container profile is included.)
- Multi-stage Dockerfiles (sdk → aspnet runtime), non-root user, `.dockerignore`.
- Config via environment variables (`ConnectionStrings__Default`, `RabbitMq__Host`); sane defaults for compose.
- Swagger UI: `http://localhost:5001/swagger`, `http://localhost:5002/swagger`. RabbitMQ UI: `http://localhost:15672` (guest/guest).
- Single command: `docker-compose up --build`.

## 9. Testing strategy

**Unit (xUnit, no I/O):**
- Domain: match outcome → win/draw/loss classification; stats delta math (matchPoints 3/1/0, totalScore accumulation).
- Validators: every rule, boundary values (score −1/0, self-play, empty username, bad email, pageSize 0/101).
- Handlers: `RecordMatchHandler` (idempotent replay, unknown player, event enqueued), `ApplyMatchResultHandler` (dedup skip, delta application) with substituted ports.

**Integration (Testcontainers PostgreSQL + RabbitMQ):**
- Full HTTP flow: register 2 players → record match → 201; repeat same idempotency key → 200, single row in DB.
- End-to-end async: record match → assert `player_stats` eventually updated in leaderboard DB (MassTransit TestHarness or polling with timeout).
- **Concurrency test**: publish N (e.g. 50) `MatchRecorded` events for the same player in parallel → final wins/totalScore exactly correct (proves §7.2/§7.3).
- Duplicate delivery test: same message id consumed twice → stats counted once.
- Migration smoke test: fresh container → app starts → schema exists.

## 10. Delivery phases

| Phase | Content | Exit criteria |
|---|---|---|
| 0 | Solution scaffold, projects, layer references, EditorConfig, Directory.Build.props (nullable, warnings-as-errors), CLAUDE.md/docs | `dotnet build` green |
| 1 | MatchService players: entities, DbContext, migrations, CRUD commands/queries, validators, ProblemDetails middleware | Player CRUD works locally; unit tests pass |
| 2 | Match recording + queries, idempotency key, filters/pagination | POST /matches idempotent; filters tested |
| 3 | Contracts project, MassTransit + RabbitMQ, transactional outbox wired into RecordMatch | Event visible in RabbitMQ after commit |
| 4 | LeaderboardService: consumer, `ApplyMatchResult`, optimistic concurrency + retry, processed_messages dedup | Concurrency unit tests pass |
| 5 | Leaderboard queries: top-N with sortBy/top, player stats with rank | API returns correct ranks |
| 6 | Dockerfiles, docker-compose, health checks, startup migrations, Swagger polish | `docker-compose up --build` → full happy path manually verified |
| 7 | Integration tests (Testcontainers), concurrency/duplicate-delivery tests | All tests green in CI-like run |
| 8 | README (run instructions, API examples, design decisions ADR-style), final review vs requirements-coverage.md | Every matrix row ✅ |

## 11. Risks / edge cases handled

- Broker down at publish time → outbox buffers, relay retries (no lost events).
- Consumer crash mid-processing → transaction rolls back, message redelivered, dedup prevents double-count.
- Two identical POST /matches racing → unique index wins the race, one row, one event.
- Player renamed after matches played → leaderboard username stale until next event; acceptable (documented); optional `PlayerUpdated` event listed as future work.
- Draw 0:0 → valid match, both players get draw +1, matchPoints +1, totalScore +0.
- Unknown player in event (LeaderboardService never saw registration) → stats row created from event data; no FK to players table by design.
- Poison message → after retry policy exhausted, moved to `_error` queue; visible in RabbitMQ UI.
