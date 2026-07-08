using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using RpsArena.Match.Api.Middleware;
using RpsArena.Match.Application.Common.Exceptions;

namespace RpsArena.Match.UnitTests.Common;

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
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var body = JsonDocument.Parse(json).RootElement;

        return (context.Response.StatusCode, body);
    }

    [Fact]
    public async Task ValidationException_maps_to_400_with_errors()
    {
        var failures = new[]
        {
            new ValidationFailure("Username", "Username is required."),
            new ValidationFailure("Email", "Email is invalid."),
        };

        var (status, body) = await HandleAsync(new ValidationException(failures));

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.GetProperty("title").GetString().Should().Be("Validation failed");
        body.GetProperty("errors").GetProperty("Username")[0].GetString()
            .Should().Be("Username is required.");
        body.GetProperty("errors").GetProperty("Email")[0].GetString()
            .Should().Be("Email is invalid.");
    }

    [Fact]
    public async Task NotFoundException_maps_to_404()
    {
        var (status, body) = await HandleAsync(new NotFoundException("Player", Guid.Empty));

        status.Should().Be(StatusCodes.Status404NotFound);
        body.GetProperty("title").GetString().Should().Be("Resource not found");
    }

    [Fact]
    public async Task ConflictException_maps_to_409()
    {
        var (status, body) = await HandleAsync(new ConflictException("Username already taken."));

        status.Should().Be(StatusCodes.Status409Conflict);
        body.GetProperty("title").GetString().Should().Be("Conflict");
        body.GetProperty("detail").GetString().Should().Be("Username already taken.");
    }

    [Fact]
    public async Task UnhandledException_maps_to_500_without_leaking_message()
    {
        var (status, body) = await HandleAsync(new InvalidOperationException("secret internal detail"));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        body.GetProperty("detail").GetString().Should().NotContain("secret internal detail");
    }
}
