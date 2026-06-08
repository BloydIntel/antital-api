using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class InvestorDashboardRepository(AntitalDBContext context) : IInvestorDashboardRepository
{
    public Task<InvestorWallet?> GetWalletAsync(int userId, CancellationToken cancellationToken = default) =>
        context.InvestorWallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId && !w.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<InvestorHolding>> GetHoldingsAsync(
        int userId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default) =>
        await context.InvestorHoldings
            .AsNoTracking()
            .Include(h => h.Offering)
            .ThenInclude(o => o.Funding)
            .Where(h =>
                h.UserId == userId
                && !h.IsDeleted
                && h.InvestedAt >= periodStartUtc
                && h.InvestedAt < periodEndUtc)
            .OrderByDescending(h => h.InvestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InvestorWatchlistItem>> GetWatchlistAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        await context.InvestorWatchlistItems
            .AsNoTracking()
            .Include(w => w.Offering)
            .ThenInclude(o => o.Funding)
            .Where(w => w.UserId == userId && !w.IsDeleted)
            .OrderByDescending(w => w.AddedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InvestorPortfolioPerformancePoint>> GetPerformancePointsAsync(
        int userId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default)
    {
        var start = periodStartUtc;
        var end = periodEndUtc;

        return await context.InvestorPortfolioPerformancePoints
            .AsNoTracking()
            .Where(p =>
                p.UserId == userId
                && !p.IsDeleted
                && new DateTime(p.Year, p.Month, 1, 0, 0, 0, DateTimeKind.Utc) >= new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                && new DateTime(p.Year, p.Month, 1, 0, 0, 0, DateTimeKind.Utc) < new DateTime(end.Year, end.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1))
            .OrderBy(p => p.Year)
            .ThenBy(p => p.Month)
            .ToListAsync(cancellationToken);
    }
}
