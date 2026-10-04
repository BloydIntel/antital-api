using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class AdminDashboardRepository(AntitalDBContext context) : IAdminDashboardRepository
{
    private const int MaxActivityEventsPerSource = 1000;

    public async Task<AdminInvestorMetrics> GetInvestorMetricsAsync(
        DateTime currentStartUtc,
        DateTime currentEndUtc,
        DateTime previousStartUtc,
        DateTime previousEndUtc,
        CancellationToken cancellationToken = default)
    {
        var investors = context.Users
            .AsNoTracking()
            .Where(user =>
                !user.IsDeleted
                && user.Role != UserRoleEnum.Admin
                && (user.UserType == UserTypeEnum.IndividualInvestor
                    || user.UserType == UserTypeEnum.CorporateInvestor));

        var totalInvestors = await investors.CountAsync(cancellationToken);
        var newInvestorsInPeriod = await investors.CountAsync(
            user => user.CreatedAt >= currentStartUtc && user.CreatedAt < currentEndUtc,
            cancellationToken);
        var newInvestorsInPreviousPeriod = await investors.CountAsync(
            user => user.CreatedAt >= previousStartUtc && user.CreatedAt < previousEndUtc,
            cancellationToken);

        return new AdminInvestorMetrics(
            totalInvestors,
            newInvestorsInPeriod,
            newInvestorsInPreviousPeriod);
    }

    public async Task<AdminCampaignMetrics> GetCampaignMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var campaigns = context.InvestmentOfferings
            .AsNoTracking()
            .Where(offering => !offering.IsDeleted);

        var totalCampaigns = await campaigns.CountAsync(cancellationToken);
        var activeCampaigns = await campaigns.CountAsync(
            offering => offering.Status == OfferingStatus.Published,
            cancellationToken);
        var totalFundsRaised = await campaigns.SumAsync(
            offering => offering.Funding != null && !offering.Funding.IsDeleted
                ? offering.Funding.RaisedAmount
                : 0m,
            cancellationToken);
        var activeCampaignRaisedAmount = await campaigns
            .Where(offering => offering.Status == OfferingStatus.Published)
            .SumAsync(
                offering => offering.Funding != null && !offering.Funding.IsDeleted
                    ? offering.Funding.RaisedAmount
                    : 0m,
                cancellationToken);

        return new AdminCampaignMetrics(
            activeCampaigns,
            activeCampaignRaisedAmount,
            totalCampaigns,
            totalFundsRaised);
    }

    public async Task<AdminActionCounts> GetActionCountsAsync(
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default)
    {
        var pendingKycReviews = await context.UserOnboardings
            .AsNoTracking()
            .CountAsync(
                onboarding =>
                    !onboarding.IsDeleted
                    && !onboarding.User.IsDeleted
                    && (onboarding.Status == OnboardingStatus.Submitted
                        || onboarding.Status == OnboardingStatus.UnderReview),
                cancellationToken);

        var draftCampaigns = await context.InvestmentOfferings
            .AsNoTracking()
            .CountAsync(
                offering => !offering.IsDeleted && offering.Status == OfferingStatus.Draft,
                cancellationToken);

        var paymentExceptions = await context.PaymentTransactions
            .AsNoTracking()
            .CountAsync(
                transaction =>
                    !transaction.IsDeleted
                    && !transaction.Order.IsDeleted
                    && !transaction.Order.User.IsDeleted
                    && !transaction.Order.Offering.IsDeleted
                    && transaction.Status == PaymentTransactionStatus.Failed
                    && (transaction.ProcessedAt ?? transaction.UpdatedAt ?? transaction.CreatedAt) >= periodStartUtc
                    && (transaction.ProcessedAt ?? transaction.UpdatedAt ?? transaction.CreatedAt) < periodEndUtc,
                cancellationToken);

        return new AdminActionCounts(
            pendingKycReviews,
            draftCampaigns,
            paymentExceptions);
    }

    public async Task<IReadOnlyList<AdminDashboardEvent>> GetRecentEventsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0)
        {
            return [];
        }

        var registrations = await context.Users
            .AsNoTracking()
            .Where(user =>
                !user.IsDeleted
                && user.Role != UserRoleEnum.Admin
                && (user.UserType == UserTypeEnum.IndividualInvestor
                    || user.UserType == UserTypeEnum.CorporateInvestor))
            .OrderByDescending(user => user.CreatedAt)
            .Take(limit)
            .Select(user => new AdminDashboardEvent(
                AdminDashboardEventKind.InvestorRegistered,
                user.Id,
                (user.FirstName + " " + user.LastName).Trim(),
                user.CreatedAt,
                null,
                null))
            .ToListAsync(cancellationToken);

        var onboardingSubmissions = await context.UserOnboardings
            .AsNoTracking()
            .Where(onboarding =>
                !onboarding.IsDeleted
                && !onboarding.User.IsDeleted
                && onboarding.SubmittedAt != null)
            .OrderByDescending(onboarding => onboarding.SubmittedAt)
            .Take(limit)
            .Select(onboarding => new AdminDashboardEvent(
                AdminDashboardEventKind.OnboardingSubmitted,
                onboarding.Id,
                (onboarding.User.FirstName + " " + onboarding.User.LastName).Trim(),
                onboarding.SubmittedAt!.Value,
                null,
                null))
            .ToListAsync(cancellationToken);

        var campaignPublications = await context.InvestmentOfferings
            .AsNoTracking()
            .Where(offering => !offering.IsDeleted && offering.PublishedAt != null)
            .OrderByDescending(offering => offering.PublishedAt)
            .Take(limit)
            .Select(offering => new AdminDashboardEvent(
                AdminDashboardEventKind.CampaignPublished,
                offering.Id,
                offering.Name,
                offering.PublishedAt!.Value,
                null,
                null))
            .ToListAsync(cancellationToken);

        var completedInvestments = await context.InvestmentOrders
            .AsNoTracking()
            .Where(order =>
                !order.IsDeleted
                && !order.User.IsDeleted
                && !order.Offering.IsDeleted
                && order.Status == InvestmentOrderStatus.Paid
                && order.PaidAt != null)
            .OrderByDescending(order => order.PaidAt)
            .Take(limit)
            .Select(order => new AdminDashboardEvent(
                AdminDashboardEventKind.InvestmentCompleted,
                order.Id,
                order.Offering.Name,
                order.PaidAt!.Value,
                order.TotalAmount,
                order.Currency))
            .ToListAsync(cancellationToken);

        return registrations
            .Concat(onboardingSubmissions)
            .Concat(campaignPublications)
            .Concat(completedInvestments)
            .OrderByDescending(activity => activity.OccurredAtUtc)
            .ThenBy(activity => activity.Kind)
            .ThenByDescending(activity => activity.EntityId)
            .Take(limit)
            .ToList();
    }

    public async Task<AdminActivityLogResult> GetActivityLogsAsync(
        AdminActivityLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var events = await GetRecentEventsAsync(MaxActivityEventsPerSource, cancellationToken);
        var mapped = events.Select(MapActivity).ToList();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            mapped = mapped.Where(item =>
                item.Subject.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.EventType.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.Module.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        mapped = mapped
            .Where(item => Matches(item.EventType, query.EventType)
                && Matches(item.Module, query.Module)
                && Matches(item.Priority, query.Priority)
                && Matches(item.Status, query.Status))
            .OrderByDescending(item => item.OccurredAtUtc)
            .ThenBy(item => item.Id)
            .ToList();

        var counts = mapped
            .GroupBy(item => item.EventType)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var items = mapped
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new AdminActivityLogResult(items, mapped.Count, counts);
    }

    private static bool Matches(string value, string? filter) =>
        string.IsNullOrWhiteSpace(filter)
        || value.Equals(filter.Trim(), StringComparison.OrdinalIgnoreCase);

    private static AdminActivityLogItem MapActivity(AdminDashboardEvent activity)
    {
        return activity.Kind switch
        {
            AdminDashboardEventKind.InvestorRegistered => new(
                $"investor-{activity.EntityId}", activity.Kind, activity.Subject,
                $"Investor {activity.Subject} registered", activity.OccurredAtUtc,
                "Investment", "Investor Management", "Low", "Completed"),
            AdminDashboardEventKind.OnboardingSubmitted => new(
                $"onboarding-{activity.EntityId}", activity.Kind, activity.Subject,
                $"KYC review submitted for {activity.Subject}", activity.OccurredAtUtc,
                "Compliance", "Compliance", "Medium", "Pending Review"),
            AdminDashboardEventKind.CampaignPublished => new(
                $"campaign-{activity.EntityId}", activity.Kind, activity.Subject,
                $"Campaign {activity.Subject} was published", activity.OccurredAtUtc,
                "Fundraising", "Fundraiser Management", "Low", "Completed"),
            AdminDashboardEventKind.InvestmentCompleted => new(
                $"investment-{activity.EntityId}", activity.Kind, activity.Subject,
                $"Investment completed for {activity.Subject}", activity.OccurredAtUtc,
                "Investment", "Financial Operations", "Low", "Completed"),
            _ => throw new ArgumentOutOfRangeException(nameof(activity.Kind), activity.Kind, null)
        };
    }
}
