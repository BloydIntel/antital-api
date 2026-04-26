using Antital.Application.Common.Security;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Resources;

namespace Antital.Application.Features.Authentication.DeleteUnverifiedUser;

public class DeleteUnverifiedUserCommandHandler(
    IUserRepository userRepository,
    IAntitalUnitOfWork unitOfWork
) : ICommandQueryHandler<DeleteUnverifiedUserCommand>
{
    public async Task<Result> Handle(DeleteUnverifiedUserCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // 1. Find user by email
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            throw new NotFoundException(Messages.NotFound);

        // 2. Block deletion of already-verified accounts via this unauthenticated endpoint
        if (user.IsEmailVerified)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "Email", new[] { "This account is already verified. Please log in to manage your account." } }
            };
            throw new BadRequestException("Account already verified.", errors);
        }

        // 3. Verify the one-time deletion OTP hash matches and has not expired/been consumed
        var hasOtpState =
            !string.IsNullOrWhiteSpace(user.UnverifiedOtpHash) &&
            user.UnverifiedOtpCreatedAtUtc.HasValue &&
            user.UnverifiedOtpExpiresAtUtc.HasValue;

        var isOtpValid = hasOtpState &&
            user.UnverifiedOtpExpiresAtUtc!.Value >= now &&
            TokenGenerator.VerifyTokenHash(request.Otp, user.UnverifiedOtpHash!);

        if (!isOtpValid)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "Otp", new[] { "Invalid, expired, or already-used OTP." } }
            };
            throw new BadRequestException("OTP validation failed.", errors);
        }

        // 4. Consume OTP and delete the unverified user account
        user.UnverifiedOtpHash = null;
        user.UnverifiedOtpCreatedAtUtc = null;
        user.UnverifiedOtpExpiresAtUtc = null;

        await userRepository.DeleteAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result();
        result.OK();
        return result;
    }
}
