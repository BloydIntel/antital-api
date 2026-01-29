using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class UserRepository(
    DBContext dbContext,
    ICurrentUser currentUser
) : Repository<User>(dbContext, currentUser), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await SetAsNoTracking
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
    }

    public new async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await SetAsNoTracking
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return await SetAsNoTracking
            .AnyAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);
    }

    public async Task<bool> VerifyEmailAsync(string email, string token, CancellationToken cancellationToken)
    {
        var user = await SetAsNoTracking
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

        if (user == null)
            return false;

        return user.EmailVerificationToken == token &&
               user.EmailVerificationTokenExpiry.HasValue &&
               user.EmailVerificationTokenExpiry.Value >= DateTime.UtcNow;
    }
}
