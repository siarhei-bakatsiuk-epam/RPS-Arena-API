using MediatR;
using Microsoft.AspNetCore.Mvc;
using RpsArena.Leaderboard.Application.Features.Leaderboard;
using RpsArena.Leaderboard.Application.Features.Leaderboard.GetLeaderboard;
using RpsArena.Leaderboard.Application.Features.Leaderboard.GetPlayerStats;

namespace RpsArena.Leaderboard.Api.Controllers;

[ApiController]
[Route("api/leaderboard")]
[Produces("application/json")]
public sealed class LeaderboardController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Top players. <paramref name="sortBy"/> ∈ {wins, draws, losses,
    /// matchPoints, totalScore} (default matchPoints); <paramref name="top"/>
    /// default 10, max 100. Each entry carries its computed rank.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PlayerStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PlayerStatsDto>>> GetLeaderboard(
        [FromQuery] string? sortBy = null,
        [FromQuery] int top = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetLeaderboardQuery(sortBy, top), cancellationToken);
        return Ok(result);
    }

    /// <summary>A single player's stats including rank (404 if the player has no stats).</summary>
    [HttpGet("players/{playerId:guid}")]
    [ProducesResponseType(typeof(PlayerStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerStatsDto>> GetPlayerStats(
        Guid playerId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPlayerStatsQuery(playerId), cancellationToken);
        return Ok(result);
    }
}
