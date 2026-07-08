# RPS Arena API

[![CI](https://github.com/siarhei-bakatsiuk-epam/RPS-Arena-API/actions/workflows/ci.yml/badge.svg)](https://github.com/siarhei-bakatsiuk-epam/RPS-Arena-API/actions/workflows/ci.yml)

Two .NET 9 microservices for a rock‑paper‑scissors tournament platform:

- **MatchService** — players CRUD, match recording & search; publishes `MatchRecorded`.
- **LeaderboardService** — consumes `MatchRecorded`, maintains per‑player stats, serves the leaderboard.

Communication is asynchronous over RabbitMQ; each service owns its own PostgreSQL database.

```
Client ──REST──▶ MatchService ──(MatchRecorded via RabbitMQ)──▶ LeaderboardService ◀──REST── Client
                     │                                                  │
                 match_db (PostgreSQL)                          leaderboard_db (PostgreSQL)
```

## Tech stack

.NET 9 · EF Core 9 + Npgsql · PostgreSQL 16 · RabbitMQ + MassTransit · CQRS (MediatR) ·
FluentValidation · xUnit + Testcontainers · Docker Compose · Swagger/OpenAPI · Health checks.

## Prerequisites

- **Docker** + **Docker Compose** (only requirement to run the system).
- To build/test outside containers: the **.NET 9 SDK** (`global.json` pins `9.0.310`).

## Quick start

```bash
docker-compose up --build
```

This starts PostgreSQL (with both databases), RabbitMQ, and both services. EF Core
migrations are applied automatically on startup. When it's up:

| Service | URL |
|---|---|
| MatchService — Swagger UI | http://localhost:5001/swagger |
| LeaderboardService — Swagger UI | http://localhost:5002/swagger |
| RabbitMQ management UI | http://localhost:15672 (guest / guest) |
| MatchService health | http://localhost:5001/health/ready |
| LeaderboardService health | http://localhost:5002/health/ready |

Exposed ports: **5001** (MatchService), **5002** (LeaderboardService), **15672**
(RabbitMQ UI), **5672** (AMQP), **5432** (PostgreSQL).

## API walkthrough (curl)

```bash
# 1. Register two players (MatchService)
ALICE=$(curl -s -X POST http://localhost:5001/api/players \
  -H 'Content-Type: application/json' \
  -d '{"username":"alice","email":"alice@example.com"}' | jq -r .id)

BOB=$(curl -s -X POST http://localhost:5001/api/players \
  -H 'Content-Type: application/json' \
  -d '{"username":"bob","email":"bob@example.com"}' | jq -r .id)

# 2. Record a match (alice beats bob 3:1). 201 Created.
curl -s -X POST http://localhost:5001/api/matches \
  -H 'Content-Type: application/json' \
  -d "{\"playerOneId\":\"$ALICE\",\"playerTwoId\":\"$BOB\",
       \"playerOneScore\":3,\"playerTwoScore\":1,
       \"playedAt\":\"2026-07-01T10:00:00Z\",
       \"idempotencyKey\":\"11111111-1111-1111-1111-111111111111\"}"

# 3. Replay the same request -> 200 OK, no duplicate (idempotent).
#    Re-run the exact command above; the response is the existing match.

# 4. Query matches with filters + pagination
curl -s "http://localhost:5001/api/matches?playerId=$ALICE&page=1&pageSize=20"

# 5. Leaderboard (eventually consistent — allow a moment for the event to flow)
curl -s "http://localhost:5002/api/leaderboard?sortBy=matchPoints&top=10"

# 6. A single player's stats incl. rank
curl -s "http://localhost:5002/api/leaderboard/players/$ALICE"
```

### Endpoints

**MatchService**

| Method | Route | Purpose |
|---|---|---|
| POST | `/api/players` | Register player (201; 409 duplicate; 400 invalid) |
| GET | `/api/players/{id}` | Get player (200 / 404) |
| GET | `/api/players?page=&pageSize=` | List players (paged) |
| PUT | `/api/players/{id}` | Update player (200 / 404 / 409 / 400) |
| DELETE | `/api/players/{id}` | Delete player (204 / 404 / 409 if matches exist) |
| POST | `/api/matches` | Record match (201; **200 idempotent replay**; 400 / 404 / 409) |
| GET | `/api/matches/{id}` | Get match (200 / 404) |
| GET | `/api/matches?playerId=&from=&to=&page=&pageSize=` | Filtered, paged list |
| GET | `/api/players/{id}/matches?page=&pageSize=` | Player match history (200 / 404) |

**LeaderboardService**

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/leaderboard?sortBy=&top=` | Top‑N (`sortBy` ∈ wins/draws/losses/matchPoints/totalScore, default matchPoints; `top` default 10, max 100) |
| GET | `/api/leaderboard/players/{playerId}` | Player stats incl. computed rank (200 / 404) |

The idempotency key for `POST /api/matches` may also be supplied via the
`Idempotency-Key` header. All errors use RFC 7807 `application/problem+json`.

## Running the tests

```bash
# Unit tests (no Docker required)
dotnet test tests/RpsArena.Match.UnitTests
dotnet test tests/RpsArena.Leaderboard.UnitTests

# Integration tests (Testcontainers — needs a running Docker daemon)
dotnet test tests/RpsArena.IntegrationTests
```

The integration tests spin up PostgreSQL + RabbitMQ via Testcontainers and host
both services in‑process. On Docker Desktop this works out of the box. On a
rootless/Colima setup, point Testcontainers at the socket:

```bash
export DOCKER_HOST="unix://$HOME/.colima/default/docker.sock"
export TESTCONTAINERS_DOCKER_SOCKET_OVERRIDE="/var/run/docker.sock"
```

**CI** — [`.github/workflows/ci.yml`](.github/workflows/ci.yml) builds in Release
(warnings‑as‑errors) and runs the full suite (unit + Testcontainers integration)
on every push/PR to `main`/`develop`, publishing a test‑results check, a coverage
summary in the job summary (and as a PR comment), and the HTML report as an
artifact. GitHub's Linux runners provide Docker, so the integration tests need no
extra configuration there.

## Design decisions

- **Idempotent match recording** — `matches.idempotency_key` has a unique index.
  A replay with the same key + payload returns `200` with the existing match; a
  key reused with a different payload returns `409`. Concurrent identical inserts
  are resolved by the unique index (catch → re‑read). When the client omits a key,
  a deterministic SHA‑256 key is derived from the payload so duplicates still
  dedupe.
- **Transactional outbox (publish side)** — `MatchRecorded` is written to
  `match_db` in the *same transaction* as the match (MassTransit EF outbox), then
  relayed to RabbitMQ. A rollback discards the event — no ghost events.
- **Optimistic concurrency (consume side)** — `player_stats` uses PostgreSQL's
  system `xmin` column as the concurrency token. On a conflict the consumer throws
  and MassTransit retries (fast immediate/short‑interval retries), so parallel
  events for one player converge to the exact result with no lost updates.
- **Exactly‑once effect** — the consumer writes a `processed_messages` row
  (message‑id PK) in the same transaction as the stats update; a redelivered
  message is a no‑op.
- **Computed rank** — `rank` is never stored; it's `DENSE_RANK() OVER (ORDER BY
  match_points DESC, total_score DESC)` at query time, keeping writes O(1).
- **One PostgreSQL container, two databases** (`match_db`, `leaderboard_db`) —
  service isolation without extra infrastructure.
- **Clean Architecture** — `Domain ← Application ← Api`, `Infrastructure → Domain`;
  a shared `RpsArena.Contracts` project holds only the `MatchRecorded` event.
  Usernames are denormalized onto the event so LeaderboardService never reads
  MatchService's database.
- **CQRS + FluentValidation** — every command/query is a MediatR request with its
  own validator, enforced through a `ValidationBehavior` pipeline.

### Deliberate trade‑offs

- **`.NET 9`** is used because the assignment specifies it. .NET 9 is out of
  standard support; for a production system I would target the current LTS (.NET
  10) — the upgrade is a `TargetFramework` bump plus a package roll‑forward.
- **Scoring** keeps the specified football scheme (win 3 / draw 1 / loss 0).
- **Player deletion is `RESTRICT`** — a player with recorded matches cannot be
  deleted (returns 409), preserving match history (vs. soft delete).
- **Rename propagation** — a renamed player's leaderboard username updates on their
  next match event (denormalized, eventually consistent).
- **Credentials** (`guest`/`postgres`) are local compose defaults only.

## Project structure

```
src/
  Shared/RpsArena.Contracts/            # MatchRecorded event (only shared code)
  MatchService/       RpsArena.Match.{Domain,Application,Infrastructure,Api}
  LeaderboardService/ RpsArena.Leaderboard.{Domain,Application,Infrastructure,Api}
tests/
  RpsArena.Match.UnitTests
  RpsArena.Leaderboard.UnitTests
  RpsArena.IntegrationTests             # Testcontainers (PostgreSQL + RabbitMQ)
docs/                                   # plan, requirements-coverage, diagrams
docker-compose.yml
```

See [`docs/`](docs/) for the full implementation plan, the 58‑requirement
traceability matrix, and architecture/sequence/ER diagrams.
