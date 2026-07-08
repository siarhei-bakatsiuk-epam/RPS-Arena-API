using MediatR;
using Microsoft.AspNetCore.Mvc;
using RpsArena.Match.Application.Features.Matches;
using RpsArena.Match.Application.Features.Matches.Record;

namespace RpsArena.Match.Api.Controllers;

[ApiController]
[Route("api/matches")]
[Produces("application/json")]
public sealed class MatchesController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Records a match result. Idempotent: replaying the same request (same
    /// idempotency key, or same payload when none is supplied) returns 200 with
    /// the existing match instead of creating a duplicate.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MatchDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Record(
        [FromBody] RecordMatchRequest request,
        [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKeyHeader,
        CancellationToken cancellationToken)
    {
        var command = new RecordMatchCommand(
            request.PlayerOneId,
            request.PlayerTwoId,
            request.PlayerOneScore,
            request.PlayerTwoScore,
            request.PlayedAt,
            request.IdempotencyKey ?? idempotencyKeyHeader);

        var result = await mediator.Send(command, cancellationToken);

        return result.AlreadyExisted
            ? Ok(result.Match)
            : Created($"/api/matches/{result.Match.Id}", result.Match);
    }
}

/// <summary>Request body for recording a match. The idempotency key may also be
/// supplied via the <c>Idempotency-Key</c> header.</summary>
public sealed record RecordMatchRequest(
    Guid PlayerOneId,
    Guid PlayerTwoId,
    int PlayerOneScore,
    int PlayerTwoScore,
    DateTime PlayedAt,
    Guid? IdempotencyKey);
