using Antital.Application.DTOs.Investments;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetOfferingHighlights;

public record GetOfferingHighlightsQuery(string IdOrSlug) : ICommandQuery<IReadOnlyList<HighlightDto>>;

public class GetOfferingHighlightsQueryHandler(
    InvestmentOfferingAccess offeringAccess,
    IInvestmentOfferingRepository repository)
    : ICommandQueryHandler<GetOfferingHighlightsQuery, IReadOnlyList<HighlightDto>>
{
    public async Task<Result<IReadOnlyList<HighlightDto>>> Handle(
        GetOfferingHighlightsQuery request,
        CancellationToken cancellationToken)
    {
        var offeringId = await offeringAccess.RequirePublishedOfferingIdAsync(request.IdOrSlug, cancellationToken);
        var highlights = await repository.GetHighlightsAsync(offeringId, cancellationToken);
        var dtos = highlights.Select(InvestmentMappers.ToHighlightDto).ToList();

        var result = new Result<IReadOnlyList<HighlightDto>>();
        result.AddValue(dtos);
        result.OK();
        return result;
    }
}
