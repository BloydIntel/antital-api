using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class FundraiserDocumentsRepository(AntitalDBContext context) : IFundraiserDocumentsRepository
{
    public async Task<IReadOnlyList<OfferingDocument>> ListByOfferingAsync(
        int offeringId,
        CancellationToken cancellationToken = default) =>
        await context.OfferingDocuments
            .AsNoTracking()
            .Where(d => d.OfferingId == offeringId && !d.IsDeleted)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .ThenBy(d => d.Title)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(OfferingDocument document, CancellationToken cancellationToken = default)
    {
        await context.OfferingDocuments.AddAsync(document, cancellationToken);
    }
}
