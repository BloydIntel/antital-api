using BuildingBlocks.Domain.Interfaces;

namespace Antital.Domain.Interfaces;

/// <summary>
/// Extends current user context with UserId from JWT for onboarding and user-scoped operations.
/// </summary>
public interface IAntitalCurrentUser : ICurrentUser
{
    /// <summary>User id from the "UserId" claim; null if not authenticated or claim missing.</summary>
    int? UserId { get; }
}
