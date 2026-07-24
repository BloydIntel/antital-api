using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.Investors.GetFundraiserInvestorAnalytics;

public record GetFundraiserInvestorAnalyticsQuery : ICommandQuery<FundraiserInvestorAnalyticsResponse>;

public class GetFundraiserInvestorAnalyticsQueryHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository,
    IFundraiserInvestorMessagesRepository messagesRepository
) : ICommandQueryHandler<GetFundraiserInvestorAnalyticsQuery, FundraiserInvestorAnalyticsResponse>
{
    public async Task<Result<FundraiserInvestorAnalyticsResponse>> Handle(
        GetFundraiserInvestorAnalyticsQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);

        if (offering == null)
        {
            var empty = new Result<FundraiserInvestorAnalyticsResponse>();
            empty.AddValue(new FundraiserInvestorAnalyticsResponse(0m, null, 0, 0, 0));
            empty.OK();
            return empty;
        }

        var (totalCount, answeredCount, averageHours) = await messagesRepository.GetAnalyticsAsync(
            offering.Id,
            cancellationToken);

        var unansweredCount = totalCount - answeredCount;
        var responseRate = totalCount == 0
            ? 0m
            : decimal.Round((decimal)answeredCount / totalCount, 4, MidpointRounding.AwayFromZero);

        var result = new Result<FundraiserInvestorAnalyticsResponse>();
        result.AddValue(new FundraiserInvestorAnalyticsResponse(
            responseRate,
            averageHours.HasValue
                ? Math.Round(averageHours.Value, 2, MidpointRounding.AwayFromZero)
                : null,
            totalCount,
            answeredCount,
            unansweredCount));
        result.OK();
        return result;
    }
}
