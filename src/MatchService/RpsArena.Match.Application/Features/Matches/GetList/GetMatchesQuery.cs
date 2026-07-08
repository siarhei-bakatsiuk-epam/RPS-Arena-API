using FluentValidation;
using MediatR;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Models;

namespace RpsArena.Match.Application.Features.Matches.GetList;

public sealed record GetMatchesQuery(
    Guid? PlayerId = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<MatchDto>>;

public sealed class GetMatchesValidator : AbstractValidator<GetMatchesQuery>
{
    public GetMatchesValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From!.Value)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("'to' must be greater than or equal to 'from'.");
    }
}

public sealed class GetMatchesHandler(IMatchRepository matches)
    : IRequestHandler<GetMatchesQuery, PagedResult<MatchDto>>
{
    public async Task<PagedResult<MatchDto>> Handle(GetMatchesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await matches.GetPagedAsync(
            request.PlayerId,
            NormalizeUtc(request.From),
            NormalizeUtc(request.To),
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items.Select(MatchDto.FromEntity).ToList();
        return new PagedResult<MatchDto>(dtos, request.Page, request.PageSize, totalCount);
    }

    // timestamptz parameters must be UTC; treat an unspecified-kind filter as UTC.
    private static DateTime? NormalizeUtc(DateTime? value) => value switch
    {
        null => null,
        { Kind: DateTimeKind.Unspecified } dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
        { } dt => dt.ToUniversalTime(),
    };
}
