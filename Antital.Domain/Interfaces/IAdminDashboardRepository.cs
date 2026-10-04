namespace Antital.Domain.Interfaces;

public record AdminInvestorMetrics(
    int TotalInvestors,
    int NewInvestorsInPeriod,
    int NewInvestorsInPreviousPeriod);

public record AdminCampaignMetrics(
    int ActiveCampaigns,
    decimal ActiveCampaignRaisedAmount,
    int TotalCampaigns,
    decimal TotalFundsRaised);

public record AdminActionCounts(
    int PendingKycReviews,
    int DraftCampaigns,
    int PaymentExceptions);

public enum AdminDashboardEventKind
{
    InvestorRegistered,
    OnboardingSubmitted,
    CampaignPublished,
    InvestmentCompleted,
}

public record AdminDashboardEvent(
    AdminDashboardEventKind Kind,
    int EntityId,
    string Subject,
    DateTime OccurredAtUtc,
    decimal? Amount = null,
    string? Currency = null);

public record AdminActivityLogQuery(
    int Page,
    int PageSize,
    string? Search = null,
    string? EventType = null,
    string? Module = null,
    string? Priority = null,
    string? Status = null);

public record AdminActivityLogItem(
    string Id,
    AdminDashboardEventKind Kind,
    string Subject,
    string Description,
    DateTime OccurredAtUtc,
    string EventType,
    string Module,
    string Priority,
    string Status);

public record AdminActivityLogResult(
    IReadOnlyList<AdminActivityLogItem> Items,
    int TotalCount,
    IReadOnlyDictionary<string, int> CountsByEventType);

public interface IAdminDashboardRepository
{
    Task<AdminInvestorMetrics> GetInvestorMetricsAsync(
        DateTime currentStartUtc,
        DateTime currentEndUtc,
        DateTime previousStartUtc,
        DateTime previousEndUtc,
        CancellationToken cancellationToken = default);

    Task<AdminCampaignMetrics> GetCampaignMetricsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminActionCounts> GetActionCountsAsync(
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminDashboardEvent>> GetRecentEventsAsync(
        int limit,
        CancellationToken cancellationToken = default);

    Task<AdminActivityLogResult> GetActivityLogsAsync(
        AdminActivityLogQuery query,
        CancellationToken cancellationToken = default);
}
