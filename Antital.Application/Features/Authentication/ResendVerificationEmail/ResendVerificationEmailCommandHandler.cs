using Antital.Domain.Interfaces;
using Antital.Application.Common.Security;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Resources;
using Microsoft.Extensions.Configuration;

namespace Antital.Application.Features.Authentication.ResendVerificationEmail;

public class ResendVerificationEmailCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IConfiguration configuration
) : ICommandQueryHandler<ResendVerificationEmailCommand>
{
    private readonly int _emailVerificationHours = configuration.GetValue<int>("Email:VerificationExpiryHours", 24);

    public async Task<Result> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        // 1. Find user by email
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException(Messages.NotFound);
        }

        // 2. If already verified, block resend
        if (user.IsEmailVerified)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "Email", new[] { "Email is already verified." } }
            };
            throw new BadRequestException("Email already verified.", errors);
        }

        // 3. Generate new token and expiry
        var verificationToken = TokenGenerator.GenerateSecureToken();
        var tokenExpiry = DateTime.UtcNow.AddHours(_emailVerificationHours);

        user.EmailVerificationToken = verificationToken;
        user.EmailVerificationTokenExpiry = tokenExpiry;

        // 4. Track updater (email/IP/system)
        var updatedBy = !string.IsNullOrEmpty(request.Email) ? request.Email :
                        (!string.IsNullOrEmpty(currentUser.UserName) ? currentUser.UserName :
                        (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System"));
        user.Updated(updatedBy);

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Send verification email
        await emailService.SendVerificationEmailAsync(request.Email, verificationToken, cancellationToken);

        var result = new Result();
        result.OK();
        return result;
    }
}
