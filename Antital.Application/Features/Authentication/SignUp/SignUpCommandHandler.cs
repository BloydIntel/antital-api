using Antital.Application.DTOs.Authentication;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Antital.Application.Common.Security;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Resources;
using Microsoft.Extensions.Configuration;

namespace Antital.Application.Features.Authentication.SignUp;

public class SignUpCommandHandler(
    IUserRepository userRepository,
    IUserInvestmentProfileRepository userInvestmentProfileRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IEmailService emailService,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IConfiguration configuration
) : ICommandQueryHandler<SignUpCommand, AuthResponseDto>
{
    private readonly int _refreshTokenDays = configuration.GetValue<int>("Jwt:RefreshTokenDays", 30);
    private readonly int _emailVerificationHours = configuration.GetValue<int>("Email:VerificationExpiryHours", 24);

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
        var verificationToken = TokenGenerator.GenerateSecureToken();

        // 4. Set token expiry (24 hours from now)
        var tokenExpiry = DateTime.UtcNow.AddHours(_emailVerificationHours);

        // 4b. Generate refresh token
        var refreshToken = TokenGenerator.GenerateSecureToken();
        var refreshTokenHash = TokenGenerator.HashToken(refreshToken);
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_refreshTokenDays);

        var userType = MapUserType(request.UserType);
        var isFundRaiser = userType == UserTypeEnum.FundRaiser;

        // 5. Create User entity
        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            UserType = userType,
            IsEmailVerified = false,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = tokenExpiry,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PreferredName = request.PreferredName,
            PhoneNumber = request.PhoneNumber ?? string.Empty,
            DateOfBirth = request.DateOfBirth ?? (isFundRaiser ? DateTime.MinValue : throw new BadRequestException("Invalid personal information.", new Dictionary<string, string[]> { { "DateOfBirth", ["Date of birth is required."] } })),
            Nationality = request.Nationality ?? string.Empty,
            CountryOfResidence = request.CountryOfResidence ?? string.Empty,
            StateOfResidence = request.StateOfResidence ?? string.Empty,
            ResidentialAddress = request.ResidentialAddress ?? string.Empty,
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

        if (userType == UserTypeEnum.CorporateInvestor || userType == UserTypeEnum.FundRaiser)
        {
            var investorCategory = userType == UserTypeEnum.CorporateInvestor
                ? MapCorporateInvestorCategory(request.CorporateInvestorCategory)
                : InvestorCategory.OtherCorporateInvestor;
            var profile = new UserInvestmentProfile
            {
                User = user,
                InvestorCategory = investorCategory,
                CompanyLegalName = request.CompanyLegalName,
                TradingBrandName = request.TradingBrandName,
                RegistrationType = request.RegistrationType,
                RegistrationNumber = request.RegistrationNumber,
                CompanyLoginEmail = request.CompanyLoginEmail,
                DateOfRegistration = request.DateOfRegistration,
                CompanyWebsite = request.CompanyWebsite,
                BusinessAddress = request.BusinessAddress,
                RegisteredAddress = request.RegisteredAddress,
                CompanyEmail = request.CompanyEmail,
                CompanyPhone = request.CompanyPhone,
                RepresentativeFullName = request.RepresentativeFullName,
                RepresentativeJobTitle = request.RepresentativeJobTitle,
                RepresentativePhoneNumber = request.RepresentativePhoneNumber,
                RepresentativeDateOfBirth = request.RepresentativeDateOfBirth,
                RepresentativeEmail = request.RepresentativeEmail,
                RepresentativeNationality = request.RepresentativeNationality,
                RepresentativeCountryOfResidence = request.RepresentativeCountryOfResidence,
                RepresentativeAddress = request.RepresentativeAddress
            };
            await userInvestmentProfileRepository.AddAsync(profile, cancellationToken);
        }

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

    private static UserTypeEnum MapUserType(string userType)
    {
        if (userType.Equals("IndividualInvestor", StringComparison.OrdinalIgnoreCase))
            return UserTypeEnum.IndividualInvestor;

        if (userType.Equals("CorporateInvestor", StringComparison.OrdinalIgnoreCase))
            return UserTypeEnum.CorporateInvestor;

        if (userType.Equals("Fundraiser", StringComparison.OrdinalIgnoreCase))
            return UserTypeEnum.FundRaiser;

        throw new BadRequestException(
            "Invalid user type.",
            new Dictionary<string, string[]>
            {
                { "UserType", ["User type must be IndividualInvestor, CorporateInvestor, or Fundraiser."] }
            });
    }

    private static InvestorCategory MapCorporateInvestorCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new BadRequestException(
                "Invalid corporate investor category.",
                new Dictionary<string, string[]>
                {
                    { "CorporateInvestorCategory", ["Corporate investor category is required for CorporateInvestor signup."] }
                });
        }

        if (category.Equals("QualifiedInstitutionalInvestor", StringComparison.OrdinalIgnoreCase))
            return InvestorCategory.QualifiedInstitutionalInvestor;

        if (category.Equals("OtherCorporateInvestor", StringComparison.OrdinalIgnoreCase))
            return InvestorCategory.OtherCorporateInvestor;

        throw new BadRequestException(
            "Invalid corporate investor category.",
            new Dictionary<string, string[]>
            {
                { "CorporateInvestorCategory", ["Corporate investor category must be QualifiedInstitutionalInvestor or OtherCorporateInvestor."] }
            });
    }

}
