using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RpsArena.Leaderboard.Api.Middleware;
using RpsArena.Leaderboard.Application.Common.Exceptions;

namespace RpsArena.Leaderboard.UnitTests.Common;

public class GlobalExceptionHandlerTests
{
    private static async Task<(int Status, JsonElement Body)> HandleAsync(Exception exception)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        var provider = services.BuildServiceProvider();

        var handler = new GlobalExceptionHandler(
            provider.GetRequiredService<IProblemDetailsService>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<GlobalExceptionHandler>>());

        var context = new DefaultHttpContext { RequestServices = provider };
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);
        handled.Should().BeTrue();

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = JsonDocument.Parse(await new StreamReader(context.Response.Body).ReadToEndAsync()).RootElement;
        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task ValidationException_maps_to_400_with_errors()
    {
        var (status, body) = await HandleAsync(
            new ValidationException([new ValidationFailure("SortBy", "Invalid sort field.")]));

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.GetProperty("title").GetString().Should().Be("Validation failed");
        body.GetProperty("errors").GetProperty("SortBy")[0].GetString().Should().Be("Invalid sort field.");
    }

    [Fact]
    public async Task NotFoundException_maps_to_404()
    {
        var (status, body) = await HandleAsync(new NotFoundException("PlayerStats", Guid.Empty));

        status.Should().Be(StatusCodes.Status404NotFound);
        body.GetProperty("title").GetString().Should().Be("Resource not found");
    }

    [Fact]
    public async Task UnhandledException_maps_to_500_without_leaking_message()
    {
        var (status, body) = await HandleAsync(new InvalidOperationException("secret internal detail"));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        body.GetProperty("detail").GetString().Should().NotContain("secret internal detail");
    }
}
