using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IUserOnboardingRepository
{
    Task<UserOnboarding?> GetByUserIdAsync(int userId, CancellationToken cancellationToken);
    /// <summary>
    /// Gets existing onboarding for the user or creates one. Handles concurrent requests safely
    /// (unique constraint violation is caught and the existing record is returned).
    /// </summary>
    Task<UserOnboarding> GetOrCreateForUserAsync(int userId, CancellationToken cancellationToken);
    Task AddAsync(UserOnboarding entity, CancellationToken cancellationToken);
    Task UpdateAsync(UserOnboarding entity, CancellationToken cancellationToken);
}
