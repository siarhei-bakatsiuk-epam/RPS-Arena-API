using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RpsArena.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts one of the APIs in-process, overriding its connection string and
/// RabbitMQ settings to point at the Testcontainers instances. The app's normal
/// startup (including EF migrations) runs, so each factory exercises the real
/// wiring end to end.
/// </summary>
public sealed class ArenaWebAppFactory<TMarker>(IReadOnlyDictionary<string, string?> settings)
    : WebApplicationFactory<TMarker>
    where TMarker : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(settings));
    }
}
