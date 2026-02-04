using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class UserKycRepository(
    DBContext dbContext,
    ICurrentUser currentUser
) : Repository<UserKyc>(dbContext, currentUser), IUserKycRepository
{
    public async Task<UserKyc?> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await SetAsNoTracking
            .FirstOrDefaultAsync(e => e.UserId == userId && !e.IsDeleted, cancellationToken);
    }
}
