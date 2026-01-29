using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Resources;

namespace Antital.Application.Features.Authentication.VerifyEmail;

public class VerifyEmailCommandHandler(
    IUserRepository userRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<VerifyEmailCommand>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        // 1. Find user by email → throw NotFoundException if not found
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException(Messages.NotFound);
        }

        // 2. Verify token matches and not expired → throw BadRequestException if invalid
        if (string.IsNullOrEmpty(user.EmailVerificationToken) ||
            user.EmailVerificationToken != request.Token ||
            !user.EmailVerificationTokenExpiry.HasValue ||
            user.EmailVerificationTokenExpiry.Value < DateTime.UtcNow)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "Token", new[] { "Invalid or expired verification token." } }
            };
            throw new BadRequestException("Email verification failed.", errors);
        }

        // 3. Set IsEmailVerified = true
        user.IsEmailVerified = true;

        // 4. Clear EmailVerificationToken and EmailVerificationTokenExpiry
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;

        // 5. Update user via UnitOfWork
        // For email verification, use email since user might not be authenticated yet, fallback to IP or System
        var updatedBy = !string.IsNullOrEmpty(request.Email) ? request.Email :
                       (!string.IsNullOrEmpty(currentUser.UserName) ? currentUser.UserName :
                       (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System"));
        user.Updated(updatedBy);
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 6. Return success result
        var result = new Result();
        result.OK();
        return result;
    }
}
