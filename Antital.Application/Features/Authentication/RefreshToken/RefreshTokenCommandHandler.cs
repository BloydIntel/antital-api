using Antital.Application.DTOs.Authentication;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Resources;
using System.Security.Cryptography;
using System.Text;

namespace Antital.Application.Features.Authentication.RefreshToken;

public class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService,
    IAntitalUnitOfWork unitOfWork
) : ICommandQueryHandler<RefreshTokenCommand, AuthResponseDto>
{
    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var incomingToken = request.RefreshToken;
        if (string.IsNullOrWhiteSpace(incomingToken))
            throw new UnauthorizedException(Messages.Unauthorized);

        var incomingHash = HashToken(incomingToken);
        var user = await userRepository.GetByRefreshTokenHashAsync(incomingHash, cancellationToken);

        if (user == null ||
            !user.RefreshTokenExpiresAt.HasValue ||
            user.RefreshTokenExpiresAt.Value < DateTime.UtcNow)
        {
            throw new UnauthorizedException(Messages.Unauthorized);
        }

        // Rotate refresh token
        var newRefreshToken = GenerateSecureToken();
        user.RefreshTokenHash = HashToken(newRefreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(30);
        user.Updated(user.Email);
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Issue new access token
        var accessToken = jwtTokenService.GenerateToken(user);

        var response = new AuthResponseDto
        {
            Token = accessToken,
            RefreshToken = newRefreshToken,
            UserId = user.Id,
            Email = user.Email,
            UserType = user.UserType,
            IsEmailVerified = user.IsEmailVerified
        };

        var result = new Result<AuthResponseDto>();
        result.AddValue(response);
        result.OK();
        return result;
    }

    private static string GenerateSecureToken()
    {
        const int TokenByteLength = 32;
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[TokenByteLength];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
