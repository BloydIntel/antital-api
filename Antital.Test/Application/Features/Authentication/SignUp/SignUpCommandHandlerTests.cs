using Antital.Application.DTOs.Authentication;
using Antital.Application.Features.Authentication.SignUp;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.SignUp;

public class SignUpCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUserInvestmentProfileRepository> _userInvestmentProfileRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IAntitalUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly IConfiguration _configuration;
    private readonly SignUpCommandHandler _handler;

    public SignUpCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userInvestmentProfileRepositoryMock = new Mock<IUserInvestmentProfileRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _emailServiceMock = new Mock<IEmailService>();
        _unitOfWorkMock = new Mock<IAntitalUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:RefreshTokenDays", "30" },
                { "Email:VerificationExpiryHours", "24" }
            })
            .Build();

        _handler = new SignUpCommandHandler(
            _userRepositoryMock.Object,
            _userInvestmentProfileRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object,
            _emailServiceMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _configuration
        );
    }

    [Fact]
    public async Task Handle_SuccessfulSignup_ReturnsAuthResponseDtoWithToken()
    {
        // Arrange
        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PreferredName: "Johnny",
            PhoneNumber: "+1234567890",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street, Lagos",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );

        var passwordHash = "hashed_password";
        var jwtToken = "jwt_token_12345";

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns(passwordHash);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns(jwtToken);

        _currentUserMock.Setup(x => x.IPAddress).Returns("127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be(jwtToken);
        result.Value.Email.Should().Be(command.Email);
        result.Value.UserType.Should().Be(UserTypeEnum.IndividualInvestor);
        result.Value.IsEmailVerified.Should().BeFalse();

        // Verify interactions
        _userRepositoryMock.Verify(x => x.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>()), Times.Once);
        _passwordHasherMock.Verify(x => x.HashPassword(command.Password), Times.Once);
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(command.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _jwtTokenServiceMock.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "existing@example.com",
            PreferredName: null,
            PhoneNumber: "+1234567890",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street, Lagos",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ConflictException>();

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmailVerificationTokenGeneratedAndSaved()
    {
        // Arrange
        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PreferredName: null,
            PhoneNumber: "+1234567890",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street, Lagos",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );

        User? capturedUser = null;

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, ct) => capturedUser = user);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("jwt_token");

        _currentUserMock.Setup(x => x.IPAddress).Returns("127.0.0.1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.EmailVerificationToken.Should().NotBeNullOrEmpty();
        capturedUser.EmailVerificationToken!.Length.Should().BeGreaterThanOrEqualTo(32);
        capturedUser.EmailVerificationTokenExpiry.Should().NotBeNull();
        capturedUser.EmailVerificationTokenExpiry!.Value.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_UserSavedWithCorrectProperties()
    {
        // Arrange
        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PreferredName: "Johnny",
            PhoneNumber: "+1234567890",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street, Lagos",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );

        User? capturedUser = null;

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, ct) => capturedUser = user);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("jwt_token");

        _currentUserMock.Setup(x => x.IPAddress).Returns("127.0.0.1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Email.Should().Be(command.Email);
        capturedUser.FirstName.Should().Be(command.FirstName);
        capturedUser.LastName.Should().Be(command.LastName);
        capturedUser.PreferredName.Should().Be(command.PreferredName);
        capturedUser.PhoneNumber.Should().Be(command.PhoneNumber);
        capturedUser.DateOfBirth.Should().Be(command.DateOfBirth);
        capturedUser.Nationality.Should().Be(command.Nationality);
        capturedUser.CountryOfResidence.Should().Be(command.CountryOfResidence);
        capturedUser.StateOfResidence.Should().Be(command.StateOfResidence);
        capturedUser.ResidentialAddress.Should().Be(command.ResidentialAddress);
        capturedUser.HasAgreedToTerms.Should().BeTrue();
        capturedUser.UserType.Should().Be(UserTypeEnum.IndividualInvestor);
        capturedUser.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_VerificationEmailSent()
    {
        // Arrange
        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PreferredName: null,
            PhoneNumber: "+1234567890",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street, Lagos",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );

        string? capturedToken = null;

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _emailServiceMock
            .Setup(x => x.SendVerificationEmailAsync(command.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((email, token, ct) => capturedToken = token);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("jwt_token");

        _currentUserMock.Setup(x => x.IPAddress).Returns("127.0.0.1");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendVerificationEmailAsync(command.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);

        capturedToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_JwtTokenGeneratedWithUnverifiedUser()
    {
        // Arrange
        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PreferredName: null,
            PhoneNumber: "+1234567890",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street, Lagos",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );

        User? capturedUser = null;

        _userRepositoryMock
            .Setup(x => x.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.HashPassword(command.Password))
            .Returns("hashed_password");

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Callback<User>(user => capturedUser = user)
            .Returns("jwt_token");

        _currentUserMock.Setup(x => x.IPAddress).Returns("127.0.0.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.IsEmailVerified.Should().BeFalse();
        result.Value!.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CorporateInvestor_CreatesCorporateInvestmentProfile()
    {
        var command = new SignUpCommand(
            FirstName: "Jane",
            LastName: "Corp",
            Email: "jane.corp@example.com",
            PreferredName: "JC",
            PhoneNumber: "+2348000000000",
            DateOfBirth: new DateTime(1992, 2, 2),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "1 Corporate Road, Lagos",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true,
            UserType: "CorporateInvestor",
            CorporateInvestorCategory: "QualifiedInstitutionalInvestor",
            CompanyLegalName: "Acme Ventures Ltd",
            TradingBrandName: "Acme Ventures",
            RegistrationType: "LTD",
            RegistrationNumber: "RC12345",
            CompanyLoginEmail: "ops@acme.com",
            DateOfRegistration: new DateTime(2020, 1, 15),
            CompanyWebsite: "https://acme.com",
            BusinessAddress: "23A Unity Crescent, Lekki",
            RegisteredAddress: "23A Unity Crescent, Lekki",
            CompanyEmail: "info@acme.com",
            CompanyPhone: "+2348012345678",
            RepresentativeFullName: "Jane Corp",
            RepresentativeJobTitle: "Director",
            RepresentativePhoneNumber: "+2348098765432",
            RepresentativeDateOfBirth: new DateTime(1990, 5, 10),
            RepresentativeEmail: "jane.rep@acme.com",
            RepresentativeNationality: "Nigerian",
            RepresentativeCountryOfResidence: "Nigeria",
            RepresentativeAddress: "Lekki, Lagos"
        );

        User? capturedUser = null;
        UserInvestmentProfile? capturedProfile = null;

        _userRepositoryMock.Setup(x => x.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasherMock.Setup(x => x.HashPassword(command.Password)).Returns("hashed_password");
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) =>
            {
                capturedUser = u;
                capturedUser.Id = 77;
            });
        _userInvestmentProfileRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserInvestmentProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserInvestmentProfile, CancellationToken>((p, _) => capturedProfile = p);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("jwt_token");
        _currentUserMock.Setup(x => x.IPAddress).Returns("127.0.0.1");

        await _handler.Handle(command, CancellationToken.None);

        capturedUser.Should().NotBeNull();
        capturedUser!.UserType.Should().Be(UserTypeEnum.CorporateInvestor);
        _userInvestmentProfileRepositoryMock.Verify(x => x.AddAsync(It.IsAny<UserInvestmentProfile>(), It.IsAny<CancellationToken>()), Times.Once);

        capturedProfile.Should().NotBeNull();
        capturedProfile!.User.Should().BeSameAs(capturedUser);
        capturedProfile.InvestorCategory.Should().Be(InvestorCategory.QualifiedInstitutionalInvestor);
        capturedProfile.CompanyLegalName.Should().Be("Acme Ventures Ltd");
        capturedProfile.RegistrationNumber.Should().Be("RC12345");
        capturedProfile.CompanyWebsite.Should().Be("https://acme.com");
        capturedProfile.RepresentativeFullName.Should().Be("Jane Corp");
    }

    [Fact]
    public async Task Handle_CorporateInvestor_WithOciCategory_SetsOciInvestorCategory()
    {
        var command = new SignUpCommand(
            FirstName: "Jane",
            LastName: "Corp",
            Email: "jane.oci@example.com",
            PreferredName: "JC",
            PhoneNumber: "+2348000000000",
            DateOfBirth: new DateTime(1992, 2, 2),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "1 Corporate Road, Lagos",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true,
            UserType: "CorporateInvestor",
            CorporateInvestorCategory: "OtherCorporateInvestor",
            CompanyLegalName: "OCI Ventures Ltd"
        );

        UserInvestmentProfile? capturedProfile = null;
        _userRepositoryMock.Setup(x => x.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasherMock.Setup(x => x.HashPassword(command.Password)).Returns("hashed_password");
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => u.Id = 88);
        _userInvestmentProfileRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<UserInvestmentProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserInvestmentProfile, CancellationToken>((p, _) => capturedProfile = p);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("jwt_token");
        _currentUserMock.Setup(x => x.IPAddress).Returns("127.0.0.1");

        await _handler.Handle(command, CancellationToken.None);

        capturedProfile.Should().NotBeNull();
        capturedProfile!.InvestorCategory.Should().Be(InvestorCategory.OtherCorporateInvestor);
    }

    [Theory]
    [InlineData("IndividualInvestor")]
    [InlineData("Fundraiser")]
    public async Task Handle_NonCorporateSignup_DoesNotCreateCorporateInvestmentProfile(string userType)
    {
        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: $"john.{userType.ToLower()}@example.com",
            PreferredName: "JD",
            PhoneNumber: "+2348000000000",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "1 Main Street, Lagos",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true,
            UserType: userType
        );

        _userRepositoryMock.Setup(x => x.EmailExistsAsync(command.Email, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _passwordHasherMock.Setup(x => x.HashPassword(command.Password)).Returns("hashed_password");
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((u, _) => u.Id = 11);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _jwtTokenServiceMock.Setup(x => x.GenerateToken(It.IsAny<User>())).Returns("jwt_token");
        _currentUserMock.Setup(x => x.IPAddress).Returns("127.0.0.1");

        await _handler.Handle(command, CancellationToken.None);

        _userInvestmentProfileRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<UserInvestmentProfile>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
