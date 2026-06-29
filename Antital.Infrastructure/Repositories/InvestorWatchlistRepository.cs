using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class InvestorWatchlistRepository(AntitalDBContext context) : IInvestorWatchlistRepository
{
    public async Task<IReadOnlyList<InvestorWatchlistItem>> ListByUserAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await context.InvestorWatchlistItems
            .AsNoTracking()
            .Include(w => w.Offering)
            .ThenInclude(o => o.Funding)
            .Include(w => w.Offering)
            .ThenInclude(o => o.DealTerms)
            .Where(w => w.UserId == userId && !w.IsDeleted)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync(cancellationToken);

    public Task<InvestorWatchlistItem?> GetActiveByUserAndOfferingAsync(
        int userId,
        int offeringId,
        CancellationToken cancellationToken = default) =>
        context.InvestorWatchlistItems
            .Include(w => w.Offering)
            .ThenInclude(o => o.Funding)
            .Include(w => w.Offering)
            .ThenInclude(o => o.DealTerms)
            .FirstOrDefaultAsync(
                w => w.UserId == userId && w.OfferingId == offeringId && !w.IsDeleted,
                cancellationToken);

    public Task<bool> IsWatchlistedAsync(
        int userId,
        int offeringId,
        CancellationToken cancellationToken = default) =>
        context.InvestorWatchlistItems
            .AsNoTracking()
            .AnyAsync(
                w => w.UserId == userId && w.OfferingId == offeringId && !w.IsDeleted,
                cancellationToken);

    public async Task<IReadOnlyDictionary<int, OfferingUpdate>> GetLatestUpdatesByOfferingIdsAsync(
        IReadOnlyList<int> offeringIds,
        CancellationToken cancellationToken = default)
    {
        if (offeringIds.Count == 0)
        {
            return new Dictionary<int, OfferingUpdate>();
        }

        var updates = await context.OfferingUpdates
            .AsNoTracking()
            .Where(u => offeringIds.Contains(u.OfferingId) && !u.IsDeleted)
            .OrderByDescending(u => u.PublishedAt)
            .ToListAsync(cancellationToken);

        return updates
            .GroupBy(u => u.OfferingId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    public async Task AddAsync(InvestorWatchlistItem item, CancellationToken cancellationToken = default)
    {
        await context.InvestorWatchlistItems.AddAsync(item, cancellationToken);
    }

    public Task UpdateAsync(InvestorWatchlistItem item, CancellationToken cancellationToken = default)
    {
        context.InvestorWatchlistItems.Update(item);
        return Task.CompletedTask;
    }
}
