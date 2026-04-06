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
    /// <summary>Trim + lowercase for case-insensitive email matching (login, signup duplicate check, verify).</summary>
    private static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return email.Trim().ToLowerInvariant();
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(email);
        if (normalized is null) return null;
        return await SetAsNoTracking
            .FirstOrDefaultAsync(
                u => u.Email.ToLower() == normalized && !u.IsDeleted,
                cancellationToken);
    }

    public override async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await SetAsNoTracking
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken)
    {
        return await SetAsNoTracking
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == refreshTokenHash && !u.IsDeleted, cancellationToken);
    }

    public new async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await SetAsNoTracking.Where(u => !u.IsDeleted).ToListAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(email);
        if (normalized is null) return false;
        return await SetAsNoTracking
            .AnyAsync(u => u.Email.ToLower() == normalized && !u.IsDeleted, cancellationToken);
    }

    public async Task<bool> VerifyEmailAsync(string email, string token, CancellationToken cancellationToken)
    {
        var normalized = NormalizeEmail(email);
        if (normalized is null) return false;
        // Use tracking so we can persist changes on successful verification
        var user = await Set
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized && !u.IsDeleted, cancellationToken);

        if (user == null)
            return false;

        var isValid =
            user.EmailVerificationToken == token &&
            user.EmailVerificationTokenExpiry.HasValue &&
            user.EmailVerificationTokenExpiry.Value >= DateTime.UtcNow;

        if (!isValid)
            return false;

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        var updatedBy = !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : email);
        user.Updated(updatedBy);

        return true;
    }
}
