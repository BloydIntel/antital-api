using Antital.Application.DTOs.Investments;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetOfferingUpdates;

public record GetOfferingUpdatesQuery(string IdOrSlug, int Page = 1, int PageSize = 20) : ICommandQuery<OfferingUpdatesResponse>;

public class GetOfferingUpdatesQueryHandler(
    InvestmentOfferingAccess offeringAccess,
    IInvestmentOfferingRepository repository)
    : ICommandQueryHandler<GetOfferingUpdatesQuery, OfferingUpdatesResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<OfferingUpdatesResponse>> Handle(
        GetOfferingUpdatesQuery request,
        CancellationToken cancellationToken)
    {
        var offeringId = await offeringAccess.RequirePublishedOfferingIdAsync(request.IdOrSlug, cancellationToken);
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var (items, totalCount) = await repository.GetUpdatesAsync(offeringId, page, pageSize, cancellationToken);
        var response = new OfferingUpdatesResponse(
            items.Select(InvestmentMappers.ToUpdateDto).ToList(),
            page,
            pageSize,
            totalCount);

        var result = new Result<OfferingUpdatesResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
