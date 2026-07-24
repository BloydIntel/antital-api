using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.GetFundraiserAnalytics;

public record GetFundraiserAnalyticsQuery(string? Period = "last-7-days")
    : ICommandQuery<FundraiserAnalyticsResponse>;

public class GetFundraiserAnalyticsQueryHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository,
    IFundraiserAnalyticsRepository analyticsRepository
) : ICommandQueryHandler<GetFundraiserAnalyticsQuery, FundraiserAnalyticsResponse>
{
    public async Task<Result<FundraiserAnalyticsResponse>> Handle(
        GetFundraiserAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        if (!FundraiserAnalyticsMappers.TryParsePeriod(
                request.Period,
                out var fromUtc,
                out var toUtcExclusive,
                out var periodError))
        {
            var invalid = new Result<FundraiserAnalyticsResponse>();
            invalid.BadRequest(
                "Invalid period.",
                new Dictionary<string, string[]> { ["period"] = [periodError!] });
            return invalid;
        }

        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);

        if (offering == null)
        {
            var empty = new Result<FundraiserAnalyticsResponse>();
            empty.AddValue(FundraiserAnalyticsMappers.Empty());
            empty.OK();
            return empty;
        }

        var engagement = await analyticsRepository.GetEngagementAsync(
            offering.Id,
            fromUtc,
            toUtcExclusive,
            cancellationToken);
        var likes = await analyticsRepository.GetCampaignLikesAsync(offering.Id, cancellationToken);
        var holdings = await analyticsRepository.GetHoldingsWithUsersAsync(offering.Id, cancellationToken);
        var userIds = holdings.Select(h => h.UserId).Distinct().ToList();
        var profiles = await analyticsRepository.GetProfilesByUserIdsAsync(userIds, cancellationToken);
        var paidOrders = await analyticsRepository.GetPaidOrdersAsync(offering.Id, cancellationToken);

        var response = FundraiserAnalyticsMappers.ToResponse(
            offering,
            engagement,
            fromUtc,
            toUtcExclusive,
            likes,
            holdings,
            profiles,
            paidOrders);

        var result = new Result<FundraiserAnalyticsResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
