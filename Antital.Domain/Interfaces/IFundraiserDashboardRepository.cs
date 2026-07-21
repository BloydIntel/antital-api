using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IFundraiserDashboardRepository
{
    Task<InvestmentOffering?> GetPrimaryOfferingAsync(int ownerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InvestorHolding>> GetHoldingsForOfferingAsync(
        int offeringId,
        CancellationToken cancellationToken = default);
}
