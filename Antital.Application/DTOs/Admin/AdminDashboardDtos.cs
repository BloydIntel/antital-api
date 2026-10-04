namespace Antital.Application.DTOs.Admin;

public enum AdminDashboardActionType
{
    PendingOnboardingReview,
    DraftCampaign,
    PaymentException,
}

public enum AdminDashboardActivityType
{
    InvestorRegistered,
    OnboardingSubmitted,
    CampaignPublished,
    InvestmentCompleted,
}

public record AdminDashboardSummaryDto(
    int TotalInvestors,
    int NewInvestorsInPeriod,
    int NewInvestorsInPreviousPeriod,
    decimal? InvestorGrowthPercent,
    int ActiveCampaigns,
    decimal ActiveCampaignRaisedAmount,
    int TotalCampaigns,
    decimal TotalFundsRaised,
    string Currency);

public record AdminDashboardActionDto(
    AdminDashboardActionType Type,
    string Title,
    string Description,
    int Count,
    string? Route);

public record AdminDashboardRecentActivityDto(
    string Id,
    AdminDashboardActivityType Type,
    string Subject,
    string Description,
    DateTime OccurredAtUtc,
    string? Route);

public record AdminDashboardResponse(
    AdminDashboardSummaryDto Summary,
    IReadOnlyList<AdminDashboardActionDto> Actions,
    IReadOnlyList<AdminDashboardRecentActivityDto> RecentActivity);
