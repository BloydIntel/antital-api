using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Resources;
using System.Security.Cryptography;
using System.Text;

namespace Antital.Application.Features.Authentication.Logout;

public class LogoutCommandHandler(
    IUserRepository userRepository,
    IAntitalUnitOfWork unitOfWork
) : ICommandQueryHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new UnauthorizedException(Messages.Unauthorized);

        var hash = HashToken(request.RefreshToken);
        var user = await userRepository.GetByRefreshTokenHashAsync(hash, cancellationToken);

        if (user == null)
            throw new UnauthorizedException(Messages.Unauthorized);

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        user.Updated(user.Email);
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result();
        result.OK();
        return result;
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }
}
