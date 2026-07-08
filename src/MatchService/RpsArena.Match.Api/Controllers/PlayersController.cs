using MediatR;
using Microsoft.AspNetCore.Mvc;
using RpsArena.Match.Application.Common.Models;
using RpsArena.Match.Application.Features.Players;
using RpsArena.Match.Application.Features.Players.Delete;
using RpsArena.Match.Application.Features.Players.GetById;
using RpsArena.Match.Application.Features.Players.GetList;
using RpsArena.Match.Application.Features.Players.Register;
using RpsArena.Match.Application.Features.Players.Update;

namespace RpsArena.Match.Api.Controllers;

[ApiController]
[Route("api/players")]
[Produces("application/json")]
public sealed class PlayersController(ISender mediator) : ControllerBase
{
    /// <summary>Registers a new player.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterPlayerRequest request, CancellationToken cancellationToken)
    {
        var player = await mediator.Send(
            new RegisterPlayerCommand(request.Username, request.Email), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = player.Id }, player);
    }

    /// <summary>Gets a player's profile by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var player = await mediator.Send(new GetPlayerByIdQuery(id), cancellationToken);
        return Ok(player);
    }

    /// <summary>Lists players (paged).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PlayerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<PlayerDto>>> GetList(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetPlayersQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }

    /// <summary>Updates a player's username/email.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerDto>> Update(
        Guid id, [FromBody] UpdatePlayerRequest request, CancellationToken cancellationToken)
    {
        var player = await mediator.Send(
            new UpdatePlayerCommand(id, request.Username, request.Email), cancellationToken);

        return Ok(player);
    }

    /// <summary>Deletes a player (409 if they have recorded matches).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeletePlayerCommand(id), cancellationToken);
        return NoContent();
    }
}

/// <summary>Request body for player registration.</summary>
public sealed record RegisterPlayerRequest(string Username, string Email);

/// <summary>Request body for updating a player.</summary>
public sealed record UpdatePlayerRequest(string Username, string Email);
