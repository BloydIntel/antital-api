using Antital.Application.Common.Security;
using Antital.Application.DTOs.Authentication;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Resources;
using Microsoft.Extensions.Configuration;

namespace Antital.Application.Features.Authentication.RefreshToken;

public class RefreshTokenCommandHandler(
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService,
    IAntitalUnitOfWork unitOfWork,
    IConfiguration configuration
) : ICommandQueryHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly int _refreshTokenDays = configuration.GetValue<int>("Jwt:RefreshTokenDays", 30);

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var incomingToken = request.RefreshToken;
        if (string.IsNullOrWhiteSpace(incomingToken))
            throw new UnauthorizedException(Messages.Unauthorized);

        var incomingHash = TokenGenerator.HashToken(incomingToken);
        var user = await userRepository.GetByRefreshTokenHashAsync(incomingHash, cancellationToken);

        if (user == null ||
            !user.RefreshTokenExpiresAt.HasValue ||
            user.RefreshTokenExpiresAt.Value < DateTime.UtcNow)
        {
            throw new UnauthorizedException(Messages.Unauthorized);
        }

        // Rotate refresh token
        var newRefreshToken = TokenGenerator.GenerateSecureToken();
        user.RefreshTokenHash = TokenGenerator.HashToken(newRefreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays);
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

}
