using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using RpsArena.Leaderboard.Api.Middleware;
using RpsArena.Leaderboard.Application;
using RpsArena.Leaderboard.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RPS Arena — LeaderboardService",
        Version = "v1",
        Description = "Consumes MatchRecorded and serves the leaderboard.",
    }));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddLeaderboardApplication();
builder.Services.AddLeaderboardInfrastructure(builder.Configuration);

// Readiness checks: PostgreSQL + (via MassTransit) RabbitMQ. Liveness has no
// dependency checks.
builder.Services.AddHealthChecks()
    .AddNpgSql(
        sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")!,
        name: "postgres",
        tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "LeaderboardService v1");
    options.DocumentTitle = "RPS Arena — LeaderboardService";
});

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

// Apply migrations on startup (retries while PostgreSQL comes up).
await app.Services.MigrateLeaderboardDatabaseAsync();

app.Run();

// Exposed so the integration-test WebApplicationFactory can reference this entry point.
public partial class Program;
