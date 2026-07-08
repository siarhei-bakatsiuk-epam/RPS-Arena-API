# RPS Arena API

Test assignment (Senior .NET, Pearson/Faethm role): two microservices for a rock-paper-scissors tournament platform.

## Status

**Implementation complete (2026-07-08).** All 21 steps of `docs/implementation-steps.md` are done and committed on `develop` (pushed to origin). Both services build (Release, 0 warnings), 103 tests pass (94 unit + 9 Testcontainers integration), and `docker-compose up --build` cold-starts the full stack with the happy path verified end-to-end. 58/58 requirements audited against code (see `docs/requirements-coverage.md`). `main` still points at the initial docs commit — merge `develop` → `main` via PR to update the default branch.

## Documentation (docs/)

- `docs/v3.task 1.md` — original assignment (Russian)
- `docs/v3.task.diagram 1.md` — architecture/sequence/ER diagrams (Mermaid)
- `docs/implementation-plan.md` — full phased implementation plan (authoritative design doc)
- `docs/requirements-coverage.md` — traceability matrix, 58/58 requirements mapped
- `docs/implementation-steps.md` — 21 small always-green steps (the execution checklist; work top to bottom)

## Locked decisions

- .NET 9, EF Core + Npgsql, PostgreSQL 16 (one container, `match_db` + `leaderboard_db`)
- RabbitMQ + **MassTransit** (transactional outbox on publish, dedup table on consume)
- CQRS via MediatR; FluentValidation through ValidationBehavior pipeline
- Clean Architecture: 4 projects per service (`Domain` ← `Application` ← `Api`, `Infrastructure` → `Domain`); shared `RpsArena.Contracts` for the `MatchRecorded` event only
- Concurrency: optimistic (`xmin` token) + retry via MassTransit redelivery — NOT pessimistic locks
- Idempotency: `idempotency_key` unique index on matches; replay returns 200 with existing match
- Leaderboard `rank` computed at query time (`DENSE_RANK`), never stored
- Tests: xUnit + Testcontainers (PostgreSQL, RabbitMQ); includes parallel-events concurrency test
- Migrations: `Database.Migrate()` on startup with retry; schema changes ONLY via EF migrations

## Conventions

- Keep all documentation in `docs/`; update this file whenever decisions or status change
- Feature folders (vertical slices) inside Application layer; one handler per command/query
- RFC 7807 ProblemDetails for all API errors
- Everything must run with `docker-compose up --build` (Swagger on :5001/:5002, RabbitMQ UI :15672)
