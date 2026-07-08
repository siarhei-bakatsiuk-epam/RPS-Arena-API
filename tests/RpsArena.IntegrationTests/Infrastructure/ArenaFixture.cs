using Npgsql;
using RpsArena.Leaderboard.Api;
using RpsArena.Match.Api;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace RpsArena.IntegrationTests.Infrastructure;

/// <summary>
/// Shared harness for the whole integration suite: one PostgreSQL 16 container
/// (hosting match_db + leaderboard_db) and one RabbitMQ container, plus a hosted
/// factory per service wired to them.
/// </summary>
public sealed class ArenaFixture : IAsyncLifetime
{
    private const string RabbitUser = "rpsuser";
    private const string RabbitPass = "rpspass";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("postgres")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .WithUsername(RabbitUser)
        .WithPassword(RabbitPass)
        // Run as the image's rabbitmq user so the erlang cookie is readable
        // (Colima/rootless volume-ownership workaround).
        .WithCreateParameterModifier(p => p.User = "999:999")
        .Build();

    public ArenaWebAppFactory<MatchApiMarker> Match { get; private set; } = null!;
    public ArenaWebAppFactory<LeaderboardApiMarker> Leaderboard { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbit.StartAsync());

        await CreateDatabasesAsync("match_db", "leaderboard_db");

        var rabbitHost = _rabbit.Hostname;
        var rabbitPort = _rabbit.GetMappedPublicPort(5672).ToString();

        Match = new ArenaWebAppFactory<MatchApiMarker>(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ConnectionStringFor("match_db"),
            ["RabbitMq:Host"] = rabbitHost,
            ["RabbitMq:Port"] = rabbitPort,
            ["RabbitMq:Username"] = RabbitUser,
            ["RabbitMq:Password"] = RabbitPass,
        });

        Leaderboard = new ArenaWebAppFactory<LeaderboardApiMarker>(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ConnectionStringFor("leaderboard_db"),
            ["RabbitMq:Host"] = rabbitHost,
            ["RabbitMq:Port"] = rabbitPort,
            ["RabbitMq:Username"] = RabbitUser,
            ["RabbitMq:Password"] = RabbitPass,
        });
    }

    public async Task DisposeAsync()
    {
        await Match.DisposeAsync();
        await Leaderboard.DisposeAsync();
        await _rabbit.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public string ConnectionStringFor(string database) =>
        new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = database }
            .ConnectionString;

    public async Task<long> CountAsync(string database, string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionStringFor(database));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    public async Task<IReadOnlySet<string>> GetTablesAsync(string database)
    {
        await using var connection = new NpgsqlConnection(ConnectionStringFor(database));
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";

        var tables = new HashSet<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private async Task CreateDatabasesAsync(params string[] databases)
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        foreach (var database in databases)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE {database}";
            await command.ExecuteNonQueryAsync();
        }
    }
}

[CollectionDefinition(Name)]
public sealed class ArenaCollection : ICollectionFixture<ArenaFixture>
{
    public const string Name = "arena";
}
