using Antital.Application.DTOs.Investments;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetOfferingFinancials;

public record GetOfferingFinancialsQuery(string IdOrSlug) : ICommandQuery<OfferingFinancialsResponse>;

public class GetOfferingFinancialsQueryHandler(
    InvestmentOfferingAccess offeringAccess,
    IInvestmentOfferingRepository repository)
    : ICommandQueryHandler<GetOfferingFinancialsQuery, OfferingFinancialsResponse>
{
    public async Task<Result<OfferingFinancialsResponse>> Handle(
        GetOfferingFinancialsQuery request,
        CancellationToken cancellationToken)
    {
        var offeringId = await offeringAccess.RequirePublishedOfferingIdAsync(request.IdOrSlug, cancellationToken);
        var metrics = await repository.GetFinancialMetricsAsync(offeringId, cancellationToken);
        var useOfProceeds = await repository.GetUseOfProceedsItemsAsync(offeringId, cancellationToken);

        var response = new OfferingFinancialsResponse(
            metrics.Select(InvestmentMappers.ToFinancialMetricDto).ToList(),
            useOfProceeds.Select(InvestmentMappers.ToUseOfProceedsDto).ToList());

        var result = new Result<OfferingFinancialsResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
