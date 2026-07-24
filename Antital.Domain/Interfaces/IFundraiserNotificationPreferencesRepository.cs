using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IFundraiserNotificationPreferencesRepository
{
    Task<FundraiserNotificationPreferences?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);

    Task AddAsync(FundraiserNotificationPreferences entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(FundraiserNotificationPreferences entity, CancellationToken cancellationToken = default);
}
