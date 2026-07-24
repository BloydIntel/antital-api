using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class FundraiserNotificationPreferencesRepository(AntitalDBContext context)
    : IFundraiserNotificationPreferencesRepository
{
    public Task<FundraiserNotificationPreferences?> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default) =>
        context.FundraiserNotificationPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted, cancellationToken);

    public async Task AddAsync(
        FundraiserNotificationPreferences entity,
        CancellationToken cancellationToken = default)
    {
        await context.FundraiserNotificationPreferences.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(
        FundraiserNotificationPreferences entity,
        CancellationToken cancellationToken = default)
    {
        context.FundraiserNotificationPreferences.Update(entity);
        return Task.CompletedTask;
    }
}
