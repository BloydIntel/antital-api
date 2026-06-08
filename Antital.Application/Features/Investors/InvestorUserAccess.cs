using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;

namespace Antital.Application.Features.Investors;

public interface IInvestorUserAccess
{
    Task<(int UserId, User User)> RequireAuthenticatedUserAsync(CancellationToken cancellationToken = default);
}

public class InvestorUserAccess(
    IAntitalCurrentUser currentUser,
    IUserRepository userRepository
) : IInvestorUserAccess
{
    public async Task<(int UserId, User User)> RequireAuthenticatedUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId;
        if (!userId.HasValue)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        return (userId.Value, user);
    }
}
