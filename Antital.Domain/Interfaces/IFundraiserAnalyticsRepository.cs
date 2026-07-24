using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IFundraiserAnalyticsRepository
{
    Task<IReadOnlyList<OfferingEngagementDaily>> GetEngagementAsync(
        int offeringId,
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive,
        CancellationToken cancellationToken = default);

    Task<int> GetCampaignLikesAsync(int offeringId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvestorHolding>> GetHoldingsWithUsersAsync(
        int offeringId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, UserInvestmentProfile>> GetProfilesByUserIdsAsync(
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvestmentOrder>> GetPaidOrdersAsync(
        int offeringId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IEnumerable<OfferingEngagementDaily> rows,
        CancellationToken cancellationToken = default);
}
