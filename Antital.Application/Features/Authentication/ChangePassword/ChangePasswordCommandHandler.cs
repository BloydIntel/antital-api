using Antital.Application.Features.Investors;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Authentication.ChangePassword;

public class ChangePasswordCommandHandler(
    IInvestorUserAccess investorUserAccess,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var (_, user) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        if (!passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new BadRequestException("Current password is incorrect.", new Dictionary<string, string[]>
            {
                { "CurrentPassword", new[] { "Current password is incorrect." } },
            });
        }

        user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;

        var updatedBy = !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
        user.Updated(updatedBy);

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result();
        result.OK();
        return result;
    }
}
