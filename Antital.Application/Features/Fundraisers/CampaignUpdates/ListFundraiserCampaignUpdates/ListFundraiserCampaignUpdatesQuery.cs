using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.CampaignUpdates.ListFundraiserCampaignUpdates;

public record ListFundraiserCampaignUpdatesQuery(
    string? Status = "all",
    int Page = 1,
    int PageSize = 20
) : ICommandQuery<FundraiserCampaignUpdatesResponse>;

public class ListFundraiserCampaignUpdatesQueryHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository,
    IFundraiserCampaignUpdatesRepository updatesRepository
) : ICommandQueryHandler<ListFundraiserCampaignUpdatesQuery, FundraiserCampaignUpdatesResponse>
{
    private const int MaxPageSize = 50;

    public async Task<Result<FundraiserCampaignUpdatesResponse>> Handle(
        ListFundraiserCampaignUpdatesQuery request,
        CancellationToken cancellationToken)
    {
        if (!FundraiserCampaignUpdateMappers.TryParseStatusFilter(request.Status, out var statusFilter, out var statusError))
        {
            var invalid = new Result<FundraiserCampaignUpdatesResponse>();
            invalid.BadRequest(
                "Invalid status.",
                new Dictionary<string, string[]> { ["status"] = [statusError!] });
            return invalid;
        }

        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        if (offering == null)
        {
            var empty = new Result<FundraiserCampaignUpdatesResponse>();
            empty.AddValue(new FundraiserCampaignUpdatesResponse([], page, pageSize, 0));
            empty.OK();
            return empty;
        }

        var (items, totalCount) = await updatesRepository.ListUpdatesAsync(
            offering.Id,
            statusFilter,
            page,
            pageSize,
            cancellationToken);

        var result = new Result<FundraiserCampaignUpdatesResponse>();
        result.AddValue(new FundraiserCampaignUpdatesResponse(
            items.Select(FundraiserCampaignUpdateMappers.ToDto).ToList(),
            page,
            pageSize,
            totalCount));
        result.OK();
        return result;
    }
}
