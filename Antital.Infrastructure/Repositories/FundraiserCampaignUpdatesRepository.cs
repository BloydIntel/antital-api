using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class FundraiserCampaignUpdatesRepository(AntitalDBContext context) : IFundraiserCampaignUpdatesRepository
{
    public async Task<(IReadOnlyList<OfferingUpdate> Items, int TotalCount)> ListUpdatesAsync(
        int offeringId,
        OfferingUpdateStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.OfferingUpdates
            .AsNoTracking()
            .Where(u => u.OfferingId == offeringId && !u.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.Status == OfferingUpdateStatus.Draft)
            .ThenByDescending(u => u.PublishedAt ?? u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<OfferingUpdate?> GetByIdAsync(int updateId, CancellationToken cancellationToken = default) =>
        context.OfferingUpdates
            .FirstOrDefaultAsync(u => u.Id == updateId && !u.IsDeleted, cancellationToken);

    public async Task AddAsync(OfferingUpdate update, CancellationToken cancellationToken = default) =>
        await context.OfferingUpdates.AddAsync(update, cancellationToken);

    public Task UpdateAsync(OfferingUpdate update, CancellationToken cancellationToken = default)
    {
        context.OfferingUpdates.Update(update);
        return Task.CompletedTask;
    }
}
