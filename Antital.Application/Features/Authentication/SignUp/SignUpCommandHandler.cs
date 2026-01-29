using Antital.Application.DTOs.Authentication;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Resources;
using System.Security.Cryptography;
using System.Text;

namespace Antital.Application.Features.Authentication.SignUp;

public class SignUpCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IEmailService emailService,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<SignUpCommand, AuthResponseDto>
{
    public async Task<Result<AuthResponseDto>> Handle(SignUpCommand request, CancellationToken cancellationToken)
    {
        // 1. Check if email already exists
        var emailExists = await userRepository.EmailExistsAsync(request.Email, cancellationToken);
        if (emailExists)
        {
            throw new ConflictException(Messages.Conflict);
        }

        // 2. Hash password
        var passwordHash = passwordHasher.HashPassword(request.Password);

        // 3. Generate email verification token (secure random 32+ chars)
        var verificationToken = GenerateSecureToken();

        // 4. Set token expiry (24 hours from now)
        var tokenExpiry = DateTime.UtcNow.AddHours(24);

        // 4b. Generate refresh token
        var refreshToken = GenerateSecureToken();
        var refreshTokenHash = HashToken(refreshToken);
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(30);

        // 5. Create User entity
        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            UserType = UserTypeEnum.IndividualInvestor, // Starting with IndividualInvestor as per requirements
            IsEmailVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = tokenExpiry,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PreferredName = request.PreferredName,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Nationality = request.Nationality,
            CountryOfResidence = request.CountryOfResidence,
            StateOfResidence = request.StateOfResidence,
            ResidentialAddress = request.ResidentialAddress,
            HasAgreedToTerms = request.HasAgreedToTerms,
            RefreshTokenHash = refreshTokenHash,
            RefreshTokenExpiresAt = refreshTokenExpiry
        };

        // Set tracking fields
        // For signup, use email since user isn't authenticated yet, fallback to IP if email not available
        var createdBy = !string.IsNullOrEmpty(request.Email) ? request.Email : 
                       (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
        user.Created(createdBy);

        // 6. Save to database via UnitOfWork
        await userRepository.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Send verification email via IEmailService
        await emailService.SendVerificationEmailAsync(request.Email, verificationToken, cancellationToken);

        // 8. Generate JWT token (but mark as unverified in claims)
        var token = jwtTokenService.GenerateToken(user);

        // 9. Return AuthResponseDto
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

    private static string GenerateSecureToken()
    {
        // Generate a URL-safe token: 32 bytes (256 bits) -> 43 base64url chars (padding removed)
        const int TokenByteLength = 32;
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
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
