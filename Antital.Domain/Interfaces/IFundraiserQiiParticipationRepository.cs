using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IFundraiserQiiParticipationRepository
{
    Task<IReadOnlyList<InvestorHolding>> ListQiiHoldingsAsync(
        int offeringId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvestmentOrder>> ListQiiPendingOrdersAsync(
        int offeringId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<int, UserInvestmentProfile>> GetProfilesByUserIdsAsync(
        IReadOnlyCollection<int> userIds,
        CancellationToken cancellationToken = default);
}
