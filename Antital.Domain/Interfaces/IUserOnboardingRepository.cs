using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IUserOnboardingRepository
{
    Task<UserOnboarding?> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    Task AddAsync(UserOnboarding entity, CancellationToken cancellationToken);
    Task UpdateAsync(UserOnboarding entity, CancellationToken cancellationToken);
}
