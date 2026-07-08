-- Runs once on first PostgreSQL initialization (docker-entrypoint-initdb.d).
-- One container hosts both service databases. Schemas are created by EF Core
-- migrations on service startup; the citext extension is enabled by the
-- MatchService migration.
CREATE DATABASE match_db;
CREATE DATABASE leaderboard_db;
