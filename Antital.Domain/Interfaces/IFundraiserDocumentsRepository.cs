using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IFundraiserDocumentsRepository
{
    Task<IReadOnlyList<OfferingDocument>> ListByOfferingAsync(
        int offeringId,
        CancellationToken cancellationToken = default);

    Task AddAsync(OfferingDocument document, CancellationToken cancellationToken = default);
}
