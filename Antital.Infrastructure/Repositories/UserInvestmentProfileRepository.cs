using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class UserInvestmentProfileRepository(
    DBContext dbContext,
    ICurrentUser currentUser
) : Repository<UserInvestmentProfile>(dbContext, currentUser), IUserInvestmentProfileRepository
{
    public async Task<UserInvestmentProfile?> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await SetAsNoTracking
            .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted, cancellationToken);
    }
}
