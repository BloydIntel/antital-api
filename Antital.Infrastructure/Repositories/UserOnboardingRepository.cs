using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class UserOnboardingRepository(
    DBContext dbContext,
    ICurrentUser currentUser
) : Repository<UserOnboarding>(dbContext, currentUser), IUserOnboardingRepository
{
    public async Task<UserOnboarding?> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await SetAsNoTracking
            .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted, cancellationToken);
    }
}
