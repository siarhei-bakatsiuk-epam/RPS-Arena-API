using FluentAssertions;
using RpsArena.Match.Application.Features.Matches.GetById;
using RpsArena.Match.Application.Features.Matches.GetList;
using RpsArena.Match.Application.Features.Matches.GetPlayerMatches;

namespace RpsArena.Match.UnitTests.Features.Matches;

public class MatchQueryValidatorTests
{
    [Fact]
    public void GetMatchById_rejects_empty_id()
    {
        new GetMatchByIdValidator().Validate(new GetMatchByIdQuery(Guid.Empty))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetMatchById_accepts_non_empty_id()
    {
        new GetMatchByIdValidator().Validate(new GetMatchByIdQuery(Guid.NewGuid()))
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", 1, 20, false)] // empty player id
    [InlineData("11111111-1111-1111-1111-111111111111", 0, 20, false)] // page < 1
    [InlineData("11111111-1111-1111-1111-111111111111", 1, 0, false)]  // pageSize < 1
    [InlineData("11111111-1111-1111-1111-111111111111", 1, 101, false)]// pageSize > 100
    [InlineData("11111111-1111-1111-1111-111111111111", 2, 50, true)]  // valid
    public void GetPlayerMatches_enforces_bounds(string playerId, int page, int pageSize, bool expected)
    {
        new GetPlayerMatchesValidator()
            .Validate(new GetPlayerMatchesQuery(Guid.Parse(playerId), page, pageSize))
            .IsValid.Should().Be(expected);
    }

    [Fact]
    public void GetMatches_rejects_from_after_to()
    {
        var query = new GetMatchesQuery(
            From: new DateTime(2026, 7, 8, 12, 0, 0, DateTimeKind.Utc),
            To: new DateTime(2026, 7, 8, 11, 0, 0, DateTimeKind.Utc));

        var result = new GetMatchesValidator().Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(GetMatchesQuery.To));
    }

    [Theory]
    [InlineData(true, true)]   // from <= to
    [InlineData(true, false)]  // only from
    [InlineData(false, true)]  // only to
    [InlineData(false, false)] // neither
    public void GetMatches_accepts_valid_date_ranges(bool hasFrom, bool hasTo)
    {
        var from = hasFrom ? new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) : (DateTime?)null;
        var to = hasTo ? new DateTime(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc) : (DateTime?)null;

        new GetMatchesValidator().Validate(new GetMatchesQuery(From: from, To: to))
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void GetMatches_enforces_paging_bounds(int page, int pageSize)
    {
        new GetMatchesValidator().Validate(new GetMatchesQuery(Page: page, PageSize: pageSize))
            .IsValid.Should().BeFalse();
    }
}
