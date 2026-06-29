using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IInvestorWatchlistRepository
{
    Task<IReadOnlyList<InvestorWatchlistItem>> ListByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<InvestorWatchlistItem?> GetActiveByUserAndOfferingAsync(
        int userId,
        int offeringId,
        CancellationToken cancellationToken = default);

    Task<bool> IsWatchlistedAsync(int userId, int offeringId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, OfferingUpdate>> GetLatestUpdatesByOfferingIdsAsync(
        IReadOnlyList<int> offeringIds,
        CancellationToken cancellationToken = default);

    Task AddAsync(InvestorWatchlistItem item, CancellationToken cancellationToken = default);

    Task UpdateAsync(InvestorWatchlistItem item, CancellationToken cancellationToken = default);
}
