using Antital.Application.Common.Security;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Resources;
using Microsoft.Extensions.Configuration;

namespace Antital.Application.Features.Authentication.RequestUnverifiedUserOtp;

public class RequestUnverifiedUserOtpCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IConfiguration configuration
) : ICommandQueryHandler<RequestUnverifiedUserOtpCommand>
{
    private readonly int _otpExpiryMinutes = configuration.GetValue<int>("Email:UnverifiedOtpExpiryMinutes", 10);

    public async Task<Result> Handle(RequestUnverifiedUserOtpCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null)
            throw new NotFoundException(Messages.NotFound);

        if (user.IsEmailVerified)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "Email", new[] { "This account is already verified. Please log in to manage your account." } }
            };
            throw new BadRequestException("Account already verified.", errors);
        }

        var otp = TokenGenerator.GenerateSixDigitOtp();
        user.UnverifiedOtpHash = TokenGenerator.HashToken(otp);
        user.UnverifiedOtpCreatedAtUtc = now;
        user.UnverifiedOtpExpiresAtUtc = now.AddMinutes(_otpExpiryMinutes);

        var updatedBy = !string.IsNullOrWhiteSpace(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrWhiteSpace(currentUser.IPAddress) ? currentUser.IPAddress : "System");
        user.Updated(updatedBy);

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await emailService.SendUnverifiedOtpEmailAsync(user.Email, otp, _otpExpiryMinutes, cancellationToken);

        var result = new Result();
        result.OK();
        return result;
    }
}
