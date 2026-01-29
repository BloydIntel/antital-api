using Antital.Application.Common.Security;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Resources;
using Microsoft.Extensions.Configuration;

namespace Antital.Application.Features.Authentication.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IConfiguration configuration,
    ResetTokenProtector tokenProtector
) : ICommandQueryHandler<ForgotPasswordCommand>
{
    private readonly int _resetExpiryHours = configuration.GetValue<int>("Email:ResetExpiryHours", 1);

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
        var rawToken = TokenGenerator.GenerateSecureToken();
        var tokenHash = TokenGenerator.HashToken(rawToken);

        user.PasswordResetTokenHash = tokenHash;
        var expiry = DateTime.UtcNow.AddHours(_resetExpiryHours);
        user.PasswordResetTokenExpiry = expiry;

        var updatedBy = !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
        user.Updated(updatedBy);

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var opaqueToken = tokenProtector.Protect(user.Email, rawToken, expiry);
        await emailService.SendPasswordResetEmailAsync(user.Email, opaqueToken, cancellationToken);

        var success = new Result();
        success.OK();
        return success;
    }

}
