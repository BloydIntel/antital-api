using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class FundraiserQiiParticipationRepository(AntitalDBContext context)
    : IFundraiserQiiParticipationRepository
{
    public async Task<IReadOnlyList<InvestorHolding>> ListQiiHoldingsAsync(
        int offeringId,
        CancellationToken cancellationToken = default)
    {
        return await context.InvestorHoldings
            .AsNoTracking()
            .Include(h => h.User)
            .Where(h =>
                h.OfferingId == offeringId
                && !h.IsDeleted
                && context.UserInvestmentProfiles.Any(p =>
                    p.UserId == h.UserId
                    && !p.IsDeleted
                    && p.InvestorCategory == InvestorCategory.QualifiedInstitutionalInvestor))
            .OrderByDescending(h => h.InvestedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InvestmentOrder>> ListQiiPendingOrdersAsync(
        int offeringId,
        CancellationToken cancellationToken = default)
    {
        return await context.InvestmentOrders
            .AsNoTracking()
            .Include(o => o.User)
            .Where(o =>
                o.OfferingId == offeringId
                && !o.IsDeleted
                && o.Status == InvestmentOrderStatus.PendingPayment
                && context.UserInvestmentProfiles.Any(p =>
                    p.UserId == o.UserId
                    && !p.IsDeleted
                    && p.InvestorCategory == InvestorCategory.QualifiedInstitutionalInvestor))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

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
}
