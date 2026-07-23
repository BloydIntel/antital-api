using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IFundraiserCampaignUpdatesRepository
{
    Task<(IReadOnlyList<OfferingUpdate> Items, int TotalCount)> ListUpdatesAsync(
        int offeringId,
        OfferingUpdateStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OfferingUpdate?> GetByIdAsync(int updateId, CancellationToken cancellationToken = default);

    Task AddAsync(OfferingUpdate update, CancellationToken cancellationToken = default);

    Task UpdateAsync(OfferingUpdate update, CancellationToken cancellationToken = default);
}
