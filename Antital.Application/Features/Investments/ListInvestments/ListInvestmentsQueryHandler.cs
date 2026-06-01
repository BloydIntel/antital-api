using Antital.Application.DTOs.Investments;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.ListInvestments;

public class ListInvestmentsQueryHandler(IInvestmentOfferingRepository repository)
    : ICommandQueryHandler<ListInvestmentsQuery, InvestmentListResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<InvestmentListResponse>> Handle(ListInvestmentsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 12 : Math.Min(request.PageSize, MaxPageSize);

        if (!InvestmentMappers.TryParseRiskFilter(request.Risk, out var risk))
        {
            var invalidResult = new Result<InvestmentListResponse>();
            invalidResult.BadRequest(
                "Invalid request.",
                new Dictionary<string, string[]>
                {
                    ["risk"] = ["Risk must be one of: low, moderate, high."],
                });
            return invalidResult;
        }

        var (items, totalCount) = await repository.ListPublishedAsync(
            page,
            pageSize,
            request.Category,
            risk,
            request.Search,
            cancellationToken);

        var response = new InvestmentListResponse(
            items.Select(InvestmentMappers.ToListItem).ToList(),
            page,
            pageSize,
            totalCount);

        var result = new Result<InvestmentListResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
