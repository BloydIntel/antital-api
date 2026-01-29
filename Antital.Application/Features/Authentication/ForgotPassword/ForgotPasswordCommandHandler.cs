using Antital.Application.Common.Security;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Resources;

namespace Antital.Application.Features.Authentication.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<ForgotPasswordCommand>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Find user by email; return OK even if not found to avoid leaking existence
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
        {
            var result = new Result();
            result.OK();
            return result;
        }

        // Generate secure reset token and hash
        var token = TokenGenerator.GenerateSecureToken();
        var tokenHash = TokenGenerator.HashToken(token);

        user.PasswordResetTokenHash = tokenHash;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        var updatedBy = !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
        user.Updated(updatedBy);

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailService.SendPasswordResetEmailAsync(user.Email, token, cancellationToken);

        var success = new Result();
        success.OK();
        return success;
    }

}
