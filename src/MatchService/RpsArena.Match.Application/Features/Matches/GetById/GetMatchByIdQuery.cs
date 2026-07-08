using FluentValidation;
using MediatR;
using RpsArena.Match.Application.Common.Abstractions;
using RpsArena.Match.Application.Common.Exceptions;

namespace RpsArena.Match.Application.Features.Matches.GetById;

public sealed record GetMatchByIdQuery(Guid Id) : IRequest<MatchDto>;

public sealed class GetMatchByIdValidator : AbstractValidator<GetMatchByIdQuery>
{
    public GetMatchByIdValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class GetMatchByIdHandler(IMatchRepository matches)
    : IRequestHandler<GetMatchByIdQuery, MatchDto>
{
    public async Task<MatchDto> Handle(GetMatchByIdQuery request, CancellationToken cancellationToken)
    {
        var match = await matches.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Match", request.Id);

        return MatchDto.FromEntity(match);
    }
}
