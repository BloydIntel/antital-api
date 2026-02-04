using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;

namespace Antital.Application.Features.Onboarding;

/// <summary>
/// Shared access check for onboarding: requires authenticated, email-verified user.
/// Used by Get, Save, and Submit handlers to avoid duplicating auth logic.
/// </summary>
public interface IOnboardingUserAccess
{
    /// <summary>
    /// Returns the current user's id and entity, or throws Unauthorized/NotFound/Forbidden.
    /// </summary>
    Task<(int UserId, User User)> RequireVerifiedUserAsync(CancellationToken cancellationToken = default);
}

public class OnboardingUserAccess(
    IAntitalCurrentUser currentUser,
    IUserRepository userRepository
) : IOnboardingUserAccess
{
    public async Task<(int UserId, User User)> RequireVerifiedUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (!userId.HasValue)
            throw new UnauthorizedException("User is not authenticated.");

        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
            throw new NotFoundException("User not found.");
        if (!user.IsEmailVerified)
            throw new ForbiddenException("Email must be verified to access onboarding.");

        return (userId.Value, user);
    }
}
