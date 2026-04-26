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

        // 3. Verify the email verification token matches and has not expired
        if (string.IsNullOrEmpty(user.EmailVerificationToken) ||
            user.EmailVerificationToken != request.Token ||
            !user.EmailVerificationTokenExpiry.HasValue ||
            user.EmailVerificationTokenExpiry.Value < now)
        {
            var errors = new Dictionary<string, string[]>
            {
                { "Token", new[] { "Invalid or expired verification token." } }
            };
            throw new BadRequestException("Token validation failed.", errors);
        }

        // 4. Delete the unverified user account
        await userRepository.DeleteAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result();
        result.OK();
        return result;
    }
}
