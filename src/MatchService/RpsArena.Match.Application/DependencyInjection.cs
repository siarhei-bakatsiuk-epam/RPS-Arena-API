using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RpsArena.Match.Application.Common.Behaviors;

namespace RpsArena.Match.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers MediatR handlers, FluentValidation validators, and the
    /// cross-cutting pipeline behaviors (logging outermost, validation next).
    /// </summary>
    public static IServiceCollection AddMatchApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            // Order matters: logging wraps validation wraps the handler.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Injectable clock so handlers are deterministic under test.
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
