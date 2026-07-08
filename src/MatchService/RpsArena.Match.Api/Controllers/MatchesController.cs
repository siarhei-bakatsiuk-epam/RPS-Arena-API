using MediatR;
using Microsoft.AspNetCore.Mvc;
using RpsArena.Match.Application.Common.Models;
using RpsArena.Match.Application.Features.Matches;
using RpsArena.Match.Application.Features.Matches.GetById;
using RpsArena.Match.Application.Features.Matches.GetList;
using RpsArena.Match.Application.Features.Matches.GetPlayerMatches;
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

    /// <summary>Gets a match by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var match = await mediator.Send(new GetMatchByIdQuery(id), cancellationToken);
        return Ok(match);
    }

    /// <summary>Lists matches with optional player/date filters and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<MatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<MatchDto>>> GetList(
        [FromQuery] Guid? playerId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetMatchesQuery(playerId, from, to, page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets a specific player's match history (404 if the player is unknown).</summary>
    [HttpGet("/api/players/{playerId:guid}/matches")]
    [ProducesResponseType(typeof(PagedResult<MatchDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<MatchDto>>> GetPlayerMatches(
        Guid playerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetPlayerMatchesQuery(playerId, page, pageSize), cancellationToken);
        return Ok(result);
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
