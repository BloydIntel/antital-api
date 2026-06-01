using Antital.Application.DTOs.Investments;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetOfferingTeam;

public record GetOfferingTeamQuery(string IdOrSlug) : ICommandQuery<IReadOnlyList<TeamMemberDto>>;

public class GetOfferingTeamQueryHandler(
    InvestmentOfferingAccess offeringAccess,
    IInvestmentOfferingRepository repository)
    : ICommandQueryHandler<GetOfferingTeamQuery, IReadOnlyList<TeamMemberDto>>
{
    public async Task<Result<IReadOnlyList<TeamMemberDto>>> Handle(
        GetOfferingTeamQuery request,
        CancellationToken cancellationToken)
    {
        var offeringId = await offeringAccess.RequirePublishedOfferingIdAsync(request.IdOrSlug, cancellationToken);
        var team = await repository.GetTeamMembersAsync(offeringId, cancellationToken);
        var dtos = team.Select(InvestmentMappers.ToTeamMemberDto).ToList();

        var result = new Result<IReadOnlyList<TeamMemberDto>>();
        result.AddValue(dtos);
        result.OK();
        return result;
    }
}
