using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RpsArena.Leaderboard.Application.Common.Abstractions;
using RpsArena.Leaderboard.Infrastructure.Messaging;
using RpsArena.Leaderboard.Infrastructure.Persistence;
using RpsArena.Leaderboard.Infrastructure.Persistence.Repositories;

namespace RpsArena.Leaderboard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddLeaderboardInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Resolve the connection string lazily from the final merged configuration.
        services.AddDbContext<LeaderboardDbContext>((sp, options) => options
            .UseNpgsql(ResolveConnectionString(sp), npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(LeaderboardDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            })
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPlayerStatsRepository, PlayerStatsRepository>();
        services.AddScoped<IProcessedMessageStore, ProcessedMessageStore>();

        AddMessaging(services);

        return services;
    }

    private static string ResolveConnectionString(IServiceProvider sp) =>
        sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

    private static void AddMessaging(IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<MatchRecordedConsumer>();
            x.SetKebabCaseEndpointNameFormatter();

            x.UsingRabbitMq((context, cfg) =>
            {
                var configuration = context.GetRequiredService<IConfiguration>();
                var port = ushort.TryParse(configuration["RabbitMq:Port"], out var p) ? p : (ushort)5672;
                cfg.Host(
                    configuration["RabbitMq:Host"] ?? "localhost",
                    port,
                    configuration["RabbitMq:VirtualHost"] ?? "/",
                    host =>
                    {
                        host.Username(configuration["RabbitMq:Username"] ?? "guest");
                        host.Password(configuration["RabbitMq:Password"] ?? "guest");
                    });

                // Retry on transient/concurrency failures, then dead-letter to _error.
                cfg.UseMessageRetry(r =>
                {
                    r.Immediate(3);
                    r.Exponential(
                        retryLimit: 5,
                        minInterval: TimeSpan.FromSeconds(1),
                        maxInterval: TimeSpan.FromSeconds(30),
                        intervalDelta: TimeSpan.FromSeconds(5));
                });

                cfg.ConfigureEndpoints(context);
            });
        });
    }
}
