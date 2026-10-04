using Antital.Application.DTOs.Admin;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Admin.GetAdminDashboard;

public sealed class GetAdminDashboardQueryHandler(
    IAdminDashboardRepository repository,
    TimeProvider timeProvider)
    : ICommandQueryHandler<GetAdminDashboardQuery, AdminDashboardResponse>
{
    private const int RecentActivityLimit = 8;

    public async Task<Result<AdminDashboardResponse>> Handle(
        GetAdminDashboardQuery request,
        CancellationToken cancellationToken)
    {
        if (!AdminDashboardPeriodResolver.TryResolve(
                request.Period,
                timeProvider.GetUtcNow(),
                out var period,
                out var validationError))
        {
            throw new BadRequestException(
                "Invalid dashboard period.",
                new Dictionary<string, string[]> { ["period"] = [validationError!] });
        }

        var investorMetrics = await repository.GetInvestorMetricsAsync(
            period.CurrentStartUtc,
            period.CurrentEndUtc,
            period.PreviousStartUtc,
            period.PreviousEndUtc,
            cancellationToken);
        var campaignMetrics = await repository.GetCampaignMetricsAsync(cancellationToken);
        var actionCounts = await repository.GetActionCountsAsync(
            period.CurrentStartUtc,
            period.CurrentEndUtc,
            cancellationToken);
        var events = await repository.GetRecentEventsAsync(RecentActivityLimit, cancellationToken);

        decimal? growth = investorMetrics.NewInvestorsInPreviousPeriod == 0
            ? null
            : decimal.Round(
                (decimal)(investorMetrics.NewInvestorsInPeriod - investorMetrics.NewInvestorsInPreviousPeriod)
                    / investorMetrics.NewInvestorsInPreviousPeriod * 100,
                2,
                MidpointRounding.AwayFromZero);

        var response = new AdminDashboardResponse(
            new AdminDashboardSummaryDto(
                investorMetrics.TotalInvestors,
                investorMetrics.NewInvestorsInPeriod,
                investorMetrics.NewInvestorsInPreviousPeriod,
                growth,
                campaignMetrics.ActiveCampaigns,
                campaignMetrics.ActiveCampaignRaisedAmount,
                campaignMetrics.TotalCampaigns,
                campaignMetrics.TotalFundsRaised,
                "NGN"),
            [
                new AdminDashboardActionDto(
                    AdminDashboardActionType.PendingOnboardingReview,
                    "Pending Onboarding Reviews",
                    $"{actionCounts.PendingKycReviews} onboarding submissions awaiting review",
                    actionCounts.PendingKycReviews,
                    null),
                new AdminDashboardActionDto(
                    AdminDashboardActionType.DraftCampaign,
                    "Draft Campaigns",
                    $"{actionCounts.DraftCampaigns} campaigns are still in draft",
                    actionCounts.DraftCampaigns,
                    null),
                new AdminDashboardActionDto(
                    AdminDashboardActionType.PaymentException,
                    "Payment Exceptions",
                    $"{actionCounts.PaymentExceptions} payment failures in the selected period",
                    actionCounts.PaymentExceptions,
                    null)
            ],
            events.Select(MapEvent).ToList());

        var result = new Result<AdminDashboardResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }

    private static AdminDashboardRecentActivityDto MapEvent(AdminDashboardEvent activity)
    {
        var (type, id, description) = activity.Kind switch
        {
            AdminDashboardEventKind.InvestorRegistered =>
                (AdminDashboardActivityType.InvestorRegistered, $"investor:{activity.EntityId}", $"{activity.Subject} registered as an investor."),
            AdminDashboardEventKind.OnboardingSubmitted =>
                (AdminDashboardActivityType.OnboardingSubmitted, $"onboarding:{activity.EntityId}", $"{activity.Subject} submitted onboarding for review."),
            AdminDashboardEventKind.CampaignPublished =>
                (AdminDashboardActivityType.CampaignPublished, $"campaign:{activity.EntityId}", $"Campaign {activity.Subject} was published."),
            AdminDashboardEventKind.InvestmentCompleted =>
                (AdminDashboardActivityType.InvestmentCompleted, $"investment:{activity.EntityId}", $"An investment was completed in {activity.Subject}."),
            _ => throw new ArgumentOutOfRangeException(nameof(activity.Kind), activity.Kind, null)
        };

        return new AdminDashboardRecentActivityDto(
            id,
            type,
            activity.Subject,
            description,
            activity.OccurredAtUtc,
            null);
    }
}
