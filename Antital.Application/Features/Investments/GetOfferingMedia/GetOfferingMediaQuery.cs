using Antital.Application.DTOs.Investments;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetOfferingMedia;

public record GetOfferingMediaQuery(string IdOrSlug) : ICommandQuery<IReadOnlyList<MediaAssetDto>>;

public class GetOfferingMediaQueryHandler(
    InvestmentOfferingAccess offeringAccess,
    IInvestmentOfferingRepository repository)
    : ICommandQueryHandler<GetOfferingMediaQuery, IReadOnlyList<MediaAssetDto>>
{
    public async Task<Result<IReadOnlyList<MediaAssetDto>>> Handle(
        GetOfferingMediaQuery request,
        CancellationToken cancellationToken)
    {
        var offeringId = await offeringAccess.RequirePublishedOfferingIdAsync(request.IdOrSlug, cancellationToken);
        var media = await repository.GetMediaAssetsAsync(offeringId, cancellationToken);
        var dtos = media.Select(InvestmentMappers.ToMediaAssetDto).ToList();

        var result = new Result<IReadOnlyList<MediaAssetDto>>();
        result.AddValue(dtos);
        result.OK();
        return result;
    }
}
