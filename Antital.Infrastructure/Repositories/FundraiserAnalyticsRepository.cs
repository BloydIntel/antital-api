using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class FundraiserAnalyticsRepository(AntitalDBContext context) : IFundraiserAnalyticsRepository
{
    public async Task<IReadOnlyList<OfferingEngagementDaily>> GetEngagementAsync(
        int offeringId,
        DateTime fromUtcInclusive,
        DateTime toUtcExclusive,
        CancellationToken cancellationToken = default) =>
        await context.OfferingEngagementDailies
            .AsNoTracking()
            .Where(e =>
                e.OfferingId == offeringId
                && !e.IsDeleted
                && e.Date >= fromUtcInclusive
                && e.Date < toUtcExclusive)
            .OrderBy(e => e.Date)
            .ToListAsync(cancellationToken);

    public async Task<int> GetCampaignLikesAsync(int offeringId, CancellationToken cancellationToken = default) =>
        await context.OfferingUpdates
            .AsNoTracking()
            .Where(u => u.OfferingId == offeringId && !u.IsDeleted)
            .SumAsync(u => (int?)u.LikeCount ?? 0, cancellationToken);

    public async Task<IReadOnlyList<InvestorHolding>> GetHoldingsWithUsersAsync(
        int offeringId,
        CancellationToken cancellationToken = default) =>
        await context.InvestorHoldings
            .AsNoTracking()
            .Include(h => h.User)
            .Where(h => h.OfferingId == offeringId && !h.IsDeleted)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<int, UserInvestmentProfile>> GetProfilesByUserIdsAsync(
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<int, UserInvestmentProfile>();
        }

        var profiles = await context.UserInvestmentProfiles
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId) && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        return profiles
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task<IReadOnlyList<InvestmentOrder>> GetPaidOrdersAsync(
        int offeringId,
        CancellationToken cancellationToken = default) =>
        await context.InvestmentOrders
            .AsNoTracking()
            .Where(o =>
                o.OfferingId == offeringId
                && !o.IsDeleted
                && o.Status == Domain.Enums.InvestmentOrderStatus.Paid
                && o.PaidAt != null)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(
        IEnumerable<OfferingEngagementDaily> rows,
        CancellationToken cancellationToken = default) =>
        await context.OfferingEngagementDailies.AddRangeAsync(rows, cancellationToken);
}
