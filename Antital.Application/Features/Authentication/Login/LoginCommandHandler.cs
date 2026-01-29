using Antital.Application.Common.Security;
using Antital.Application.DTOs.Authentication;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Resources;
using Microsoft.Extensions.Configuration;

namespace Antital.Application.Features.Authentication.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IAntitalUnitOfWork unitOfWork,
    IConfiguration configuration
) : ICommandQueryHandler<LoginCommand, AuthResponseDto>
{
    private readonly int _refreshTokenDays = configuration.GetValue<int>("Jwt:RefreshTokenDays", 30);

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Find user by email → throw NotFoundException if not found
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException(Messages.NotFound);
        }

        // 2. Verify password using IPasswordHasher → throw UnauthorizedException if invalid
        var isPasswordValid = passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedException(Messages.Unauthorized);
        }

        // 3. Check if email is verified → throw UnauthorizedException if not verified
        if (!user.IsEmailVerified)
        {
            throw new UnauthorizedException("Email address has not been verified. Please verify your email before logging in.");
        }

        // 3b. Generate refresh token and persist
        var refreshToken = TokenGenerator.GenerateSecureToken();
        user.RefreshTokenHash = TokenGenerator.HashToken(refreshToken);
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays);
        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Generate JWT token using IJwtTokenService
        var token = jwtTokenService.GenerateToken(user);

        // 5. Return AuthResponseDto
        var response = new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
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
