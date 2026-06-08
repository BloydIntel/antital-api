using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IInvestorDashboardRepository
{
    Task<InvestorWallet?> GetWalletAsync(int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvestorHolding>> GetHoldingsAsync(
        int userId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvestorWatchlistItem>> GetWatchlistAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvestorPortfolioPerformancePoint>> GetPerformancePointsAsync(
        int userId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken = default);
}
