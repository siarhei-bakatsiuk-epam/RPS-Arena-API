# Requirements Traceability Matrix

Every requirement from [`v3.task 1.md`](./v3.task%201.md) and [`v3.task.diagram 1.md`](./v3.task.diagram%201.md) mapped to the [implementation plan](./implementation-plan.md). Status: ✅ covered, 📝 covered with a justified deviation.

## §2 Technical stack

| # | Requirement | Plan section | Status |
|---|---|---|---|
| S1 | .NET 9 | §2 | ✅ |
| S2 | EF Core | §2, §4 | ✅ |
| S3 | PostgreSQL | §2, §4, §8 | ✅ |
| S4 | CQRS + MediatR | §2, §6 | ✅ |
| S5 | FluentValidation | §2, §5.1, §6 | ✅ |
| S6 | RabbitMQ / Kafka | §2 (RabbitMQ + MassTransit), §7.4 | ✅ |
| S7 | Docker + Docker Compose | §8 | ✅ |
| S8 | xUnit/NUnit (optional) | §2, §9 (xUnit) | ✅ |

## §3.1 MatchService — players

| # | Requirement | Plan section | Status |
|---|---|---|---|
| P1 | Register player | §5.1 `POST /api/players`, §6 `RegisterPlayer` | ✅ |
| P2 | Get profile by ID | §5.1 `GET /api/players/{id}` | ✅ |
| P3 | List all players | §5.1 `GET /api/players` (paged) | ✅ |
| P4 | Update player | §5.1 `PUT /api/players/{id}` | ✅ |
| P5 | Delete player | §5.1 `DELETE /api/players/{id}` (RESTRICT policy documented) | ✅ |
| P6 | Model: id/username/email/createdAt | §4.1 players table | ✅ |
| P7 | username unique | §4.1 unique index | ✅ |
| P8 | email unique | §4.1 unique index (case-insensitive) | ✅ |

## §3.2 MatchService — matches

| # | Requirement | Plan section | Status |
|---|---|---|---|
| M1 | Record match result | §5.1 `POST /api/matches`, §6 `RecordMatch` | ✅ |
| M2 | Get match by ID | §5.1 `GET /api/matches/{id}` | ✅ |
| M3 | List matches with filtering | §5.1 `GET /api/matches` | ✅ |
| M4 | Player match history | §5.1 `GET /api/players/{id}/matches` | ✅ |
| M5 | Model: id, playerOneId/TwoId, scores, playedAt | §4.1 matches table | ✅ |
| M6 | Both players must exist | §5.1 validation (404), §9 handler test | ✅ |
| M7 | No self-play | §4.1 CHECK constraint + §5.1 validator (defense in depth) | ✅ |
| M8 | Scores non-negative integers | §4.1 CHECK + §5.1 validator | ✅ |
| M9 | Draws allowed (2:2, 0:0) | §5.1, §11 (0:0 edge case) | ✅ |
| M10 | Publish `MatchRecorded` after save | §4.3 contract, §7.4 outbox | ✅ |
| M11 | Filter `?playerId=` | §5.1 | ✅ |
| M12 | Filter `?from=&to=` | §5.1 (+ `from <= to` validation) | ✅ |
| M13 | Pagination `?page=&pageSize=` | §5.1 (bounds 1..100) | ✅ |

## §4 LeaderboardService

| # | Requirement | Plan section | Status |
|---|---|---|---|
| L1 | Standalone service subscribed to events | §3 (separate service), §7.4 (durable queue) | ✅ |
| L2 | Recalculate stats on `MatchRecorded` | §6 `ApplyMatchResult`, §7.2 | ✅ |
| L3 | Match points: win 3 / draw 1 / loss 0 | §5.2 (scheme kept, rationale given) | ✅ |
| L4 | `totalScore` = sum of rounds won | §4.2, §9 unit tests | ✅ |
| L5 | Scheme adjustable with justification | §5.2 — kept as-is, justified | ✅ |
| L6 | Top players endpoint | §5.2 `GET /api/leaderboard` | ✅ |
| L7 | Per-player stats endpoint | §5.2 `GET /api/leaderboard/players/{id}` | ✅ |
| L8 | `?sortBy=wins\|draws\|losses\|matchPoints\|totalScore` | §5.2 (all five values, default matchPoints) | ✅ |
| L9 | `?top=10` default 10 | §5.2 (default 10, max 100) | ✅ |
| L10 | Stats model incl. rank | §4.2 + §5.2 — rank computed at query time via `DENSE_RANK`, not stored | 📝 justified deviation, spec allows model changes |
| L11 | username in stats | §4.2 denormalized from event, §4.3 event carries usernames | ✅ |

## §5 Collision protection

| # | Requirement | Plan section | Status |
|---|---|---|---|
| C1 | Idempotent match recording — repeat request creates no duplicate | §7.1 (idempotency key + unique index + race-safe fallback) | ✅ |
| C2 | Race condition on parallel stats updates handled correctly | §7.2 (xmin optimistic concurrency + retry), §7.3 (consumer dedup), §9 concurrency test | ✅ |

## §6 Infrastructure

| # | Requirement | Plan section | Status |
|---|---|---|---|
| I1 | Compose: matchservice, leaderboardservice, postgres, rabbitmq | §8 | ✅ |
| I2 | Starts with `docker-compose up --build` | §8, phase 6 exit criteria | ✅ |
| I3 | EF migrations applied automatically at startup | §8 — `Database.Migrate()` on startup with retry; optional literal init-container profile | 📝 equivalent mechanism, documented |
| I4 | Ports exposed, Swagger reachable | §8 (5001/5002 + swagger URLs) | ✅ |

## §7 Architecture requirements

| # | Requirement | Plan section | Status |
|---|---|---|---|
| A1 | CQRS: commands/queries separated, one handler per class | §6 | ✅ |
| A2 | Everything through `IMediator` | §6 (controllers contain no logic) | ✅ |
| A3 | Each request has `AbstractValidator<T>` via pipeline behaviour | §3 (`ValidationBehavior`), §5.1, §6 | ✅ |
| A4 | Schema changes only via EF Core Migrations | §2, §4 (outbox tables also via migrations), §9 migration smoke test | ✅ |
| A5 | Layered: Infrastructure → Domain ← Application ← API | §3 (per-service 4 projects, Domain dependency-free) | ✅ |

## Diagrams file

| # | Requirement | Plan section | Status |
|---|---|---|---|
| D1 | Two services, own DBs, broker between (diagram 1) | §3, §4, §8 | ✅ |
| D2 | Layer dependency rules (diagram 2) | §3 | ✅ |
| D3 | Record-match sequence: validate → idempotency check + insert → publish → 201 (diagram 3) | §5.1, §7.1, §7.4 (publish via outbox in same tx — strictly stronger than diagram) | ✅ |
| D4 | Leaderboard recalc in transaction with locking (diagram 4) | §7.2 — optimistic instead of pessimistic lock | 📝 justified deviation (diagram's own ER shows `rowVersion`) |
| D5 | ER: USER, MATCH, PLAYER_STATS incl. rowVersion | §4.1, §4.2 (`xmin` = rowVersion) | ✅ |

## §8 Evaluation criteria → plan mapping

| Criterion (weight) | Where addressed |
|---|---|
| API correctness (25%) | §5, §9 unit+integration tests, RFC 7807 errors |
| Architecture quality (20%) | §3 Clean Architecture, §6 CQRS, phase 0 tooling |
| Collision protection (20%) | §7 (four distinct mechanisms), §9 dedicated tests |
| Async leaderboard (15%) | §7.2–7.4, §9 end-to-end async test |
| Docker Compose (10%) | §8, phase 6 |
| Code quality (10%) | Directory.Build.props (nullable, WAE), EditorConfig, vertical slices |
| **Bonus:** tests | §9 — unit + integration + concurrency |
| **Bonus:** OpenAPI | §2, §8 Swagger UI |
| **Bonus:** health checks | §2, §8 `/health/live`, `/health/ready` |

## §9 Submission format

| # | Requirement | Plan section | Status |
|---|---|---|---|
| F1 | Public repo | Existing git repo; push to GitHub at phase 8 | ✅ |
| F2 | README with `docker-compose up --build` instructions | Phase 8 | ✅ |

**Result: 58/58 requirements covered — 55 directly, 3 via justified deviations (L10 computed rank, I3 startup migration, D4 optimistic concurrency), all explicitly permitted by the spec's "changes welcome with rationale" clauses.**
