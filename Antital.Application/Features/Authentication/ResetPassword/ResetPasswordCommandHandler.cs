using Antital.Application.Common.Security;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Resources;

namespace Antital.Application.Features.Authentication.ResetPassword;

public class ResetPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ResetTokenProtector tokenProtector
) : ICommandQueryHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var (email, rawToken) = tokenProtector.Unprotect(request.Token);

        var user = await userRepository.GetByEmailAsync(email, cancellationToken)
            ?? throw new NotFoundException(Messages.NotFound);

        if (string.IsNullOrEmpty(user.PasswordResetTokenHash) || !user.PasswordResetTokenExpiry.HasValue)
        {
            throw new BadRequestException("Reset token is invalid or missing.", new Dictionary<string, string[]>
            {
                { "Token", new[] { "Reset token is invalid or missing." } }
            });
        }

        if (user.PasswordResetTokenExpiry.Value < DateTime.UtcNow)
        {
            throw new BadRequestException("Reset token has expired.", new Dictionary<string, string[]>
            {
                { "Token", new[] { "Reset token has expired." } }
            });
        }

        var incomingHash = TokenGenerator.HashToken(rawToken);
        if (!string.Equals(incomingHash, user.PasswordResetTokenHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Reset token is invalid.", new Dictionary<string, string[]>
            {
                { "Token", new[] { "Reset token is invalid." } }
            });
        }

        user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiry = null;

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
