using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Infrastructure.Messaging;
using RpsArena.Match.Infrastructure.Persistence;
using RpsArena.Match.Infrastructure.Persistence.Repositories;

namespace RpsArena.Match.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMatchInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Resolve the connection string lazily (per DbContext) from the final
        // merged configuration rather than capturing it at registration time.
        services.AddDbContext<MatchDbContext>((sp, options) => options
            .UseNpgsql(ResolveConnectionString(sp), npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(MatchDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            })
            // Must match the design-time factory so the runtime model lines up
            // with the generated migrations.
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();

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
            // Transactional outbox: MatchRecorded is written to match_db in the
            // same transaction as the match, then relayed to RabbitMQ.
            x.AddEntityFrameworkOutbox<MatchDbContext>(o =>
            {
                o.UsePostgres();
                o.UseBusOutbox();
            });

            x.SetKebabCaseEndpointNameFormatter();

            x.UsingRabbitMq((context, cfg) =>
            {
                // Read from the built container's configuration (reflects all
                // sources, including test overrides).
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

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
    }
}
