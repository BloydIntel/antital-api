using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class FundraiserDashboardRepository(AntitalDBContext context) : IFundraiserDashboardRepository
{
    public async Task<InvestmentOffering?> GetPrimaryOfferingAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await context.InvestmentOfferings
            .AsNoTracking()
            .Include(o => o.Funding)
            .Include(o => o.DealTerms)
            .Where(o => o.OwnerUserId == ownerUserId && !o.IsDeleted)
            .OrderByDescending(o => o.Status == OfferingStatus.Published)
            .ThenBy(o => o.DealTerms != null && o.DealTerms.Deadline >= now ? 0 : 1)
            .ThenBy(o => o.DealTerms != null ? o.DealTerms.Deadline : DateTime.MaxValue)
            .ThenByDescending(o => o.PublishedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InvestorHolding>> GetHoldingsForOfferingAsync(
        int offeringId,
        CancellationToken cancellationToken = default) =>
        await context.InvestorHoldings
            .AsNoTracking()
            .Where(h => h.OfferingId == offeringId && !h.IsDeleted)
            .OrderByDescending(h => h.InvestedAt)
            .ToListAsync(cancellationToken);
}
