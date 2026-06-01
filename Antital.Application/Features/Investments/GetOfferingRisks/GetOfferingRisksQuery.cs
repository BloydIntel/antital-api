using Antital.Application.DTOs.Investments;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetOfferingRisks;

public record GetOfferingRisksQuery(string IdOrSlug) : ICommandQuery<IReadOnlyList<OfferingRiskDto>>;

public class GetOfferingRisksQueryHandler(
    InvestmentOfferingAccess offeringAccess,
    IInvestmentOfferingRepository repository)
    : ICommandQueryHandler<GetOfferingRisksQuery, IReadOnlyList<OfferingRiskDto>>
{
    public async Task<Result<IReadOnlyList<OfferingRiskDto>>> Handle(
        GetOfferingRisksQuery request,
        CancellationToken cancellationToken)
    {
        var offeringId = await offeringAccess.RequirePublishedOfferingIdAsync(request.IdOrSlug, cancellationToken);
        var risks = await repository.GetRisksAsync(offeringId, cancellationToken);
        var dtos = risks.Select(InvestmentMappers.ToRiskDto).ToList();

        var result = new Result<IReadOnlyList<OfferingRiskDto>>();
        result.AddValue(dtos);
        result.OK();
        return result;
    }
}
