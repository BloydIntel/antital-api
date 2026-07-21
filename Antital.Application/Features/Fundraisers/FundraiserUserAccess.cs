using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;

namespace Antital.Application.Features.Fundraisers;

public interface IFundraiserUserAccess
{
    Task<(int UserId, User User)> RequireFundraiserAsync(CancellationToken cancellationToken = default);
}

public class FundraiserUserAccess(
    IAntitalCurrentUser currentUser,
    IUserRepository userRepository
) : IFundraiserUserAccess
{
    public async Task<(int UserId, User User)> RequireFundraiserAsync(CancellationToken cancellationToken = default)
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

        if (user.UserType != UserTypeEnum.FundRaiser)
        {
            throw new ForbiddenException("Only fundraisers can access this resource.");
        }

        return (userId.Value, user);
    }
}
