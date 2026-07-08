using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using RpsArena.Contracts;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;
using MatchEntity = RpsArena.Match.Domain.Entities.Match;

namespace RpsArena.Match.Application.Features.Matches.Record;

public sealed class RecordMatchHandler(
    IMatchRepository matches,
    IPlayerRepository players,
    IUnitOfWork unitOfWork,
    IEventPublisher events)
    : IRequestHandler<RecordMatchCommand, RecordMatchResult>
{
    public async Task<RecordMatchResult> Handle(RecordMatchCommand request, CancellationToken cancellationToken)
    {
        // timestamptz requires UTC; normalize once so the stored value, the
        // derived idempotency key, and the replay comparison all agree.
        var playedAtUtc = NormalizeUtc(request.PlayedAt);
        var key = request.IdempotencyKey ?? DeriveKey(request, playedAtUtc);

        // Fast path: already recorded under this key.
        var existing = await matches.GetByIdempotencyKeyAsync(key, cancellationToken);
        if (existing is not null)
        {
            return Replay(existing, request, playedAtUtc);
        }

        // Both players must exist (404 otherwise). Fetched (not just existence-
        // checked) so their usernames can be denormalized onto the event.
        var playerOne = await players.GetByIdAsync(request.PlayerOneId, cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerOneId);

        var playerTwo = await players.GetByIdAsync(request.PlayerTwoId, cancellationToken)
            ?? throw new NotFoundException("Player", request.PlayerTwoId);

        var match = MatchEntity.Record(
            request.PlayerOneId, request.PlayerTwoId,
            request.PlayerOneScore, request.PlayerTwoScore,
            playedAtUtc, key);

        await matches.AddAsync(match, cancellationToken);

        // Enrolled in the transactional outbox: this is captured in the same
        // DbContext and committed atomically with the match by SaveChanges. On
        // rollback (e.g. idempotency race) the event is discarded too.
        await events.PublishAsync(
            new MatchRecorded(
                match.Id,
                playerOne.Id, playerOne.Username,
                playerTwo.Id, playerTwo.Username,
                match.PlayerOneScore, match.PlayerTwoScore,
                match.PlayedAt),
            cancellationToken);

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConflictException)
        {
            // Race: a concurrent request inserted the same idempotency key first.
            // Re-read and replay instead of surfacing a duplicate.
            var raced = await matches.GetByIdempotencyKeyAsync(key, cancellationToken);
            if (raced is null)
            {
                throw;
            }

            return Replay(raced, request, playedAtUtc);
        }

        return new RecordMatchResult(MatchDto.FromEntity(match), AlreadyExisted: false);
    }

    private static RecordMatchResult Replay(
        MatchEntity existing, RecordMatchCommand request, DateTime playedAtUtc)
    {
        if (!PayloadMatches(existing, request, playedAtUtc))
        {
            throw new ConflictException(
                "The idempotency key has already been used for a match with different data.");
        }

        return new RecordMatchResult(MatchDto.FromEntity(existing), AlreadyExisted: true);
    }

    private static bool PayloadMatches(MatchEntity m, RecordMatchCommand r, DateTime playedAtUtc) =>
        m.PlayerOneId == r.PlayerOneId &&
        m.PlayerTwoId == r.PlayerTwoId &&
        m.PlayerOneScore == r.PlayerOneScore &&
        m.PlayerTwoScore == r.PlayerTwoScore &&
        ToMicroseconds(m.PlayedAt) == ToMicroseconds(playedAtUtc);

    /// <summary>
    /// Deterministic fallback key when the client omits one: SHA-256 over the
    /// canonical payload (with the timestamp normalized to UTC), so repeated
    /// identical submissions still deduplicate.
    /// </summary>
    private static Guid DeriveKey(RecordMatchCommand r, DateTime playedAtUtc)
    {
        var canonical = string.Create(
            CultureInfo.InvariantCulture,
            $"{r.PlayerOneId:N}|{r.PlayerTwoId:N}|{r.PlayerOneScore}|{r.PlayerTwoScore}|{playedAtUtc:O}");

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return new Guid(hash.AsSpan(0, 16));
    }

    // Postgres timestamptz demands UTC. Treat an unspecified-kind timestamp as
    // UTC (the API's canonical zone); convert Local/offset values to UTC.
    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        _ => value.ToUniversalTime(),
    };

    // Postgres timestamptz has microsecond precision; align both sides before
    // comparing so a round-trip never causes a false "different payload".
    private static long ToMicroseconds(DateTime dt) =>
        dt.ToUniversalTime().Ticks / TimeSpan.TicksPerMicrosecond;
}
