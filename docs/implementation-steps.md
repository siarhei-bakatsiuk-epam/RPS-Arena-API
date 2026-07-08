# Implementation Steps — Interactive Breakdown

Splits [`implementation-plan.md`](./implementation-plan.md) phases 0–8 into **21 small steps**. Each step is sized for one focused interactive session (~15–45 min of agent work), leaves the repo **green** (builds + all tests pass), and ends with a concrete check you can run. Requirement IDs reference [`requirements-coverage.md`](./requirements-coverage.md).

Rules for every step: commit at the end; never edit DB schema outside EF migrations (A4); all new commands/queries go through MediatR (A2) with a validator (A3).

## Stage A — Foundation

**Step 1 — Solution scaffold.**
`RpsArena.sln`; 9 projects: `RpsArena.Contracts` + 4 per service (Domain/Application/Infrastructure/Api) with layer references per plan §3; `Directory.Build.props` (net9.0, nullable, warnings-as-errors), `.editorconfig`.
Covers: S1, A5, D2. ✔ `dotnet build` green.

**Step 2 — Test scaffold.**
3 xUnit test projects (`Match.UnitTests`, `Leaderboard.UnitTests`, `IntegrationTests`) + FluentAssertions, NSubstitute; one placeholder test each.
Covers: S8 (partial). ✔ `dotnet test` green.

## Stage B — MatchService: players

**Step 3 — Player domain + persistence.**
`Player` entity (id, username, email, createdAt); `MatchDbContext` (Npgsql), config: unique index on username, case-insensitive unique email; initial migration.
Covers: S2, S3, P6, P7, P8, D5 (USER). ✔ migration applies to local PG (docker one-liner), constraints visible in `\d players`.

**Step 4 — API plumbing.**
MediatR + `ValidationBehavior` + `LoggingBehavior`; FluentValidation; RFC 7807 exception middleware (400/404/409 mapping); Swagger UI; startup `Database.Migrate()` with retry.
Covers: S4, S5, A1, A2, A3, A4, I3 (mechanism), bonus OpenAPI. ✔ service runs, `/swagger` up, invalid request → ProblemDetails 400.

**Step 5 — Register + get player.**
`RegisterPlayer` command (+validator: username 3–32, valid email), `GetPlayerById`, `GetPlayers` (paged) queries; controllers; unit tests for validator + handler (duplicate → 409).
Covers: P1, P2, P3. ✔ tests green; manual POST/GET via Swagger.

**Step 6 — Update + delete player.**
`UpdatePlayer` (uniqueness re-check), `DeletePlayer` (RESTRICT → 409 if matches exist — FK arrives step 7, logic ready); unit tests.
Covers: P4, P5. ✔ tests green.

## Stage C — MatchService: matches

**Step 7 — Match domain + persistence.**
`Match` entity; migration: FKs `ON DELETE RESTRICT`, CHECKs (scores ≥ 0, no self-play), `idempotency_key` unique index, indexes on player ids + played_at.
Covers: M5, M7 (DB level), M8 (DB level), D5 (MATCH). ✔ migration applies; constraint test via SQL.

**Step 8 — RecordMatch command.**
Validator (distinct existing players → 404, scores ≥ 0, playedAt not future, draws allowed); idempotency: key lookup → replay returns 200 same match, differing payload → 409; race fallback via unique-violation catch → re-read; fallback SHA-256 key when client omits it. Unit tests incl. replay + 0:0 draw.
Covers: M1, M6, M7, M8, M9, C1 (logic), D3. ✔ tests green; double POST via Swagger → one row.

**Step 9 — Match queries.**
`GetMatchById`, `GetMatches` (playerId/from/to filters + pagination, validator: page ≥ 1, pageSize 1–100, from ≤ to), `GetPlayerMatches`; unit tests.
Covers: M2, M3, M4, M11, M12, M13. ✔ tests green; filtered queries via Swagger.

## Stage D — Messaging

**Step 10 — Contract + publish with outbox.**
`MatchRecorded` record in Contracts (ids, usernames, scores, playedAt); MassTransit + RabbitMQ in MatchService; EF transactional outbox (migration for outbox tables); publish inside `RecordMatch` transaction.
Covers: S6, M10, L11 (event side), C1 (no ghost events), D1, D3. ✔ local rabbit: event lands in exchange after POST /matches; DB rollback → no event.

## Stage E — LeaderboardService

**Step 11 — Stats domain + scoring.**
`PlayerStats` entity + pure domain logic: outcome classification, matchPoints 3/1/0, totalScore accumulation; exhaustive unit tests (win/loss/draw, 0:0).
Covers: L3, L4, L5. ✔ unit tests green.

**Step 12 — Leaderboard persistence.**
`LeaderboardDbContext`; migrations: `player_stats` (xmin concurrency token, denormalized username), `processed_messages`.
Covers: L11 (storage), D5 (PLAYER_STATS/rowVersion). ✔ migration applies.

**Step 13 — Consumer with dedup + concurrency.**
`MatchRecordedConsumer` → `ApplyMatchResult`: one transaction = dedup insert + both players' stats upsert; xmin conflict → throw → MassTransit retry (immediate 3 + backoff, then `_error` queue); first-insert PK race handled. Unit tests: dedup skip, delta math, both-players atomicity.
Covers: L1, L2, C2 (logic), D4, S4/S5 for this service (same plumbing as step 4, copied minimal). ✔ tests green; manual: POST match → stats row appears.

**Step 14 — Leaderboard API.**
`GetLeaderboard` (sortBy ∈ 5 values, default matchPoints; top default 10 max 100; DENSE_RANK + deterministic tie-break), `GetPlayerStats` (+rank, 404); validators; Swagger.
Covers: L6, L7, L8, L9, L10. ✔ manual: ranks correct for seeded data; bad sortBy → 400.

## Stage F — Infrastructure

**Step 15 — Dockerfiles.**
Multi-stage builds for both services, non-root, `.dockerignore`.
Covers: S7 (partial). ✔ `docker build` both images.

**Step 16 — docker-compose.**
postgres (init script: `match_db` + `leaderboard_db`) + rabbitmq (management) + both services; healthchecks + `depends_on: service_healthy`; env-var config; ports 5001/5002/15672.
Covers: S7, I1, I2, I3, I4, D1. ✔ **cold start test**: `docker-compose down -v && docker-compose up --build` → both Swaggers up, full happy path by hand.

## Stage G — Integration tests

**Step 17 — Testcontainers harness + HTTP flows.**
Shared fixture (PG + RabbitMQ containers, WebApplicationFactory); tests: player CRUD flow; record match 201; idempotent replay 200 + single row; migration smoke (fresh DB → schema exists).
Covers: S8, C1 (proof), I3 (proof). ✔ integration tests green.

**Step 18 — Async + concurrency proofs.**
End-to-end: POST match → leaderboard eventually consistent (poll w/ timeout); **50 parallel `MatchRecorded` for one player → exact final stats**; duplicate message id → counted once.
Covers: C2 (proof), L2 (proof), D4 (proof). ✔ integration tests green.

## Stage H — Polish

**Step 19 — Health checks.**
`/health/live` + `/health/ready` (Npgsql, RabbitMQ) in both services; wire into compose healthchecks.
Covers: bonus health checks. ✔ endpoints return Healthy in compose.

**Step 20 — README + submission.**
Run instructions (`docker-compose up --build`), API examples (curl/Swagger), design decisions summary (idempotency, outbox, optimistic concurrency, computed rank, .NET 9-per-spec + upgrade note), ports table; push to public repo.
Covers: F1, F2. ✔ README followed verbatim on clean machine works.

**Step 21 — Final audit.**
Walk `requirements-coverage.md` row by row against actual code/tests; fix gaps; update CLAUDE.md status.
Covers: everything — closes the loop. ✔ 58/58 verified against code, not just plan.

## Coverage checklist (all 58 IDs → steps)

| IDs | Steps |
|---|---|
| S1 | 1 · S2 3 · S3 3 · S4 4,13 · S5 4,13 · S6 10 · S7 15,16 · S8 2,17 |
| P1–P3 | 5 · P4–P5 6 · P6–P8 3 |
| M1 | 8 · M2–M4 9 · M5 7 · M6 8 · M7 7,8 · M8 7,8 · M9 8 · M10 10 · M11–M13 9 |
| L1 | 13 · L2 13,18 · L3–L5 11 · L6–L10 14 · L11 10,12 |
| C1 | 8,10,17 · C2 13,18 |
| I1 | 16 · I2 16 · I3 4,16,17 · I4 16 |
| A1–A3 | 4 (enforced every step) · A4 3,7,10,12 (all schema via migrations) · A5 1 |
| D1 | 10,16 · D2 1 · D3 8,10 · D4 13,18 · D5 3,7,12 |
| F1–F2 | 20 |
| Bonus | tests 2,17,18 · OpenAPI 4,14 · health 19 |

**Dependency order is linear (1→21)**; the only safe parallelization: Stage E steps 11–12 can start once step 10's contract exists, and step 15 anytime after step 4. Each step is independently committable — a pause after any step leaves a working, demonstrable state.
