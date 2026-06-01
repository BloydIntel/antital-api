using Antital.Application.DTOs.Investments;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetOfferingContentBlocks;

public record GetOfferingContentBlocksQuery(string IdOrSlug) : ICommandQuery<IReadOnlyList<ContentBlockDto>>;

public class GetOfferingContentBlocksQueryHandler(
    InvestmentOfferingAccess offeringAccess,
    IInvestmentOfferingRepository repository)
    : ICommandQueryHandler<GetOfferingContentBlocksQuery, IReadOnlyList<ContentBlockDto>>
{
    public async Task<Result<IReadOnlyList<ContentBlockDto>>> Handle(
        GetOfferingContentBlocksQuery request,
        CancellationToken cancellationToken)
    {
        var offeringId = await offeringAccess.RequirePublishedOfferingIdAsync(request.IdOrSlug, cancellationToken);
        var blocks = await repository.GetContentBlocksAsync(offeringId, cancellationToken);
        var dtos = blocks.Select(InvestmentMappers.ToContentBlockDto).ToList();

        var result = new Result<IReadOnlyList<ContentBlockDto>>();
        result.AddValue(dtos);
        result.OK();
        return result;
    }
}
