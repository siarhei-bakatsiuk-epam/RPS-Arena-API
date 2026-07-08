using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using RpsArena.Match.Api.Middleware;
using RpsArena.Match.Application;
using RpsArena.Match.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RPS Arena — MatchService",
        Version = "v1",
        Description = "Players and match recording for the RPS Arena platform.",
    }));

// RFC 7807 ProblemDetails for all errors.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddMatchApplication();
builder.Services.AddMatchInfrastructure(builder.Configuration);

// Readiness checks: PostgreSQL + (via MassTransit) RabbitMQ. Liveness has no
// dependency checks.
builder.Services.AddHealthChecks()
    .AddNpgSql(
        sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")!,
        name: "postgres",
        tags: ["ready"]);

var app = builder.Build();

app.UseExceptionHandler();

// Swagger is exposed in every environment (spec requirement).
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MatchService v1");
    options.DocumentTitle = "RPS Arena — MatchService";
});

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

// Apply migrations on startup (retries while PostgreSQL comes up).
await app.Services.MigrateMatchDatabaseAsync();

app.Run();

// Exposed so the integration-test WebApplicationFactory can reference this entry point.
public partial class Program;
