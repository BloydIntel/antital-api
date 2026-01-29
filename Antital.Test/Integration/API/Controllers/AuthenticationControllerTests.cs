using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Antital.Application.DTOs.Authentication;
using Antital.Application.Features.Authentication.Login;
using Antital.Application.Features.Authentication.SignUp;
using Antital.Application.Features.Authentication.VerifyEmail;
using Antital.Application.Features.Authentication.ForgotPassword;
using Antital.Application.Features.Authentication.ResetPassword;
using Antital.Domain.Enums;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Test.Integration;
using BuildingBlocks.Application.Features;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Antital.Test.Integration.API.Controllers;

/// <summary>
/// Integration tests for AuthenticationController using a real test database.
/// Data is cleaned up after all tests complete (similar to Python project's conftest.py with scope="session").
/// Uses AntitalDB_Test database which can be safely deleted/recreated.
/// </summary>
[Collection("IntegrationTests")]
public class AuthenticationControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;
    private readonly AntitalDBContext _context;
    
    // JSON options for deserializing API responses (handles camelCase and enums)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public AuthenticationControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        
        // Clean up before each test class runs to ensure fresh state
        CleanupDatabase();
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    [Fact]
    public async Task SignUp_SuccessfulSignup_Returns200WithToken()
    {
        // Arrange
        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PreferredName: "Johnny",
            PhoneNumber: "+2348012345678",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street, Victoria Island, Lagos",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/signup", command);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Request failed with status {response.StatusCode}. Response: {errorContent}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<AuthResponseDto>>(JsonOptions);
        Console.WriteLine($"Deserialized result: IsSuccess={result?.IsSuccess}, Value is null: {result?.Value == null}"); // Debug
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().NotBeNullOrEmpty();
        result.Value.UserId.Should().BeGreaterThan(0);
        result.Value.Email.Should().Be(command.Email);
        result.Value.UserType.Should().Be(UserTypeEnum.IndividualInvestor);
        result.Value.IsEmailVerified.Should().BeFalse();
    }

    [Fact]
    public async Task SignUp_DuplicateEmail_Returns409Conflict()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "existing@example.com",
            PasswordHash = "hashed",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "Existing",
            LastName = "User",
            PhoneNumber = "+2348012345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street",
            HasAgreedToTerms = true
        };
        existingUser.Created("System");
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "existing@example.com",
            PreferredName: null,
            PhoneNumber: "+2348012345678",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/signup", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SignUp_InvalidData_Returns400BadRequest()
    {
        // Arrange
        var command = new SignUpCommand(
            FirstName: "", // Invalid: empty
            LastName: "Doe",
            Email: "invalid-email", // Invalid: wrong format
            PreferredName: null,
            PhoneNumber: "+2348012345678",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street",
            Password: "weak", // Invalid: too short
            ConfirmPassword: "weak",
            HasAgreedToTerms: false // Invalid: must be true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/signup", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignUp_PasswordMismatch_Returns400BadRequest()
    {
        // Arrange
        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PreferredName: null,
            PhoneNumber: "+2348012345678",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street",
            Password: "SecurePass123!",
            ConfirmPassword: "DifferentPass123!", // Mismatch
            HasAgreedToTerms: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/signup", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignUp_TermsNotAgreed_Returns400BadRequest()
    {
        // Arrange
        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PreferredName: null,
            PhoneNumber: "+2348012345678",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: false // Must be true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/signup", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SignUp_Under18YearsOld_Returns400BadRequest()
    {
        // Arrange
        var command = new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PreferredName: null,
            PhoneNumber: "+2348012345678",
            DateOfBirth: DateTime.UtcNow.AddYears(-17), // Under 18
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/signup", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_SuccessfulLogin_Returns200WithToken()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePass123!"),
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+2348012345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street",
            HasAgreedToTerms = true
        };
        user.Created("System");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new LoginCommand(
            Email: "test@example.com",
            Password: "SecurePass123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<AuthResponseDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().NotBeNullOrEmpty();
        result.Value.Email.Should().Be(command.Email);
        result.Value.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task Login_InvalidEmail_Returns404NotFound()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "nonexistent@example.com",
            Password: "SecurePass123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401Unauthorized()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+2348012345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street",
            HasAgreedToTerms = true
        };
        user.Created("System");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new LoginCommand(
            Email: "test@example.com",
            Password: "WrongPassword123!" // Wrong password
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ValidToken_Revokes()
    {
        // Arrange: create user with refresh token
        var refreshToken = "logout_token_123";
        var refreshHash = ComputeHash(refreshToken);

        var user = new User
        {
            Email = "logout@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePass123!"),
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+2348012345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street",
            HasAgreedToTerms = true,
            RefreshTokenHash = refreshHash,
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(5)
        };
        user.Created("System");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new Antital.Application.Features.Authentication.Logout.LogoutCommand(refreshToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/logout", command);

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Expected 200 OK but got {(int)response.StatusCode}. Body: {body}");
        }
        _context.ChangeTracker.Clear();
        var updated = await _context.Users.FirstAsync(u => u.Email == user.Email);
        updated.RefreshTokenHash.Should().BeNull();
        updated.RefreshTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Login_UnverifiedEmail_Returns401Unauthorized()
    {
        // Arrange
        var user = new User
        {
            Email = "unverified@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePass123!"),
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = false,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+2348012345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street",
            HasAgreedToTerms = true
        };
        user.Created("System");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new LoginCommand(
            Email: "unverified@example.com",
            Password: "SecurePass123!"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_InvalidData_Returns400BadRequest()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "invalid-email", // Invalid format
            Password: "" // Empty password
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyEmail_SuccessfulVerification_Returns200()
    {
        // Arrange
        var token = Guid.NewGuid().ToString();
        var user = new User
        {
            Email = "verify@example.com",
            PasswordHash = "hashed",
            UserType = UserTypeEnum.IndividualInvestor,
            EmailVerificationToken = token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+2348012345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street",
            HasAgreedToTerms = true
        };
        user.Created("System");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new VerifyEmailCommand(
            Email: "verify@example.com",
            Token: token
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();

        // Verify user is marked as verified
        _context.ChangeTracker.Clear(); // ensure we read fresh values instead of the tracked pre-request entity
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "verify@example.com");
        updatedUser.Should().NotBeNull();
        updatedUser!.IsEmailVerified.Should().BeTrue();
        updatedUser.EmailVerificationToken.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_Returns400BadRequest()
    {
        // Arrange
        var user = new User
        {
            Email = "verify@example.com",
            PasswordHash = "hashed",
            UserType = UserTypeEnum.IndividualInvestor,
            EmailVerificationToken = "correct-token",
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+2348012345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street",
            HasAgreedToTerms = true
        };
        user.Created("System");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new VerifyEmailCommand(
            Email: "verify@example.com",
            Token: "wrong-token" // Invalid token
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyEmail_ExpiredToken_Returns400BadRequest()
    {
        // Arrange
        var token = Guid.NewGuid().ToString();
        var user = new User
        {
            Email = "verify@example.com",
            PasswordHash = "hashed",
            UserType = UserTypeEnum.IndividualInvestor,
            EmailVerificationToken = token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1), // Expired
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+2348012345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street",
            HasAgreedToTerms = true
        };
        user.Created("System");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new VerifyEmailCommand(
            Email: "verify@example.com",
            Token: token
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VerifyEmail_UserNotFound_Returns404NotFound()
    {
        // Arrange
        var command = new VerifyEmailCommand(
            Email: "nonexistent@example.com",
            Token: "some-token"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ForgotPassword_AlwaysReturns200()
    {
        // Arrange
        var command = new ForgotPasswordCommand("missing@example.com");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_UpdatesPasswordAndClearsToken()
    {
        // Arrange: seed user with reset token
        var email = "reset-user@example.com";
        var token = "valid-reset-token";
        var hash = HashToken(token);
        var newPassword = "NewStrongP@ss1";

        var user = new User
        {
            Email = email,
            PasswordHash = "old-hash",
            UserType = UserTypeEnum.IndividualInvestor,
            PasswordResetTokenHash = hash,
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(30),
            HasAgreedToTerms = true,
            FirstName = "Reset",
            LastName = "User",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Street",
            PhoneNumber = "+2348012345678",
            IsEmailVerified = true
        };
        user.Created("System");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new ResetPasswordCommand(email, token, newPassword, newPassword);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/reset-password", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Use a fresh scope/context to avoid stale tracking
        await using var scope = _factory.Services.CreateAsyncScope();
        var freshContext = scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        var updated = await freshContext.Users.FirstAsync(u => u.Email == email);
        updated.PasswordHash.Should().NotBe("old-hash");
        updated.PasswordResetTokenHash.Should().BeNull();
        updated.PasswordResetTokenExpiry.Should().BeNull();
    }

    [Fact]
    public async Task FullFlow_SignUp_VerifyEmail_Login_Success()
    {
        // Arrange & Act - SignUp
        var signUpCommand = new SignUpCommand(
            FirstName: "Flow",
            LastName: "Test",
            Email: "flow@example.com",
            PreferredName: null,
            PhoneNumber: "+2348012345678",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );

        var signUpResponse = await _client.PostAsJsonAsync("/api/auth/signup", signUpCommand);
        signUpResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var signUpResult = await signUpResponse.Content.ReadFromJsonAsync<Result<AuthResponseDto>>(JsonOptions);
        signUpResult!.Value!.IsEmailVerified.Should().BeFalse();

        // Get verification token from database
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "flow@example.com");
        user.Should().NotBeNull();
        var verificationToken = user!.EmailVerificationToken;
        verificationToken.Should().NotBeNullOrEmpty();

        // Act - VerifyEmail
        var verifyCommand = new VerifyEmailCommand(
            Email: "flow@example.com",
            Token: verificationToken!
        );

        var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyCommand);
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Login
        var loginCommand = new LoginCommand(
            Email: "flow@example.com",
            Password: "SecurePass123!"
        );

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<Result<AuthResponseDto>>(JsonOptions);
        loginResult!.IsSuccess.Should().BeTrue();
        loginResult.Value!.IsEmailVerified.Should().BeTrue();
        loginResult.Value.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_Valid_ReturnsNewTokens()
    {
        // Arrange: create verified user with refresh token
        var refreshToken = "refresh_token_123";
        var refreshHash = ComputeHash(refreshToken);

        var user = new User
        {
            Email = "refresh@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SecurePass123!"),
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            FirstName = "Test",
            LastName = "User",
            PhoneNumber = "+2348012345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street",
            HasAgreedToTerms = true,
            RefreshTokenHash = refreshHash,
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(5)
        };
        user.Created("System");
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var command = new Antital.Application.Features.Authentication.RefreshToken.RefreshTokenCommand(refreshToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<AuthResponseDto>>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().NotBeNullOrEmpty();
        result.Value.RefreshToken.Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task RefreshToken_Invalid_Returns401()
    {
        var command = new Antital.Application.Features.Authentication.RefreshToken.RefreshTokenCommand("invalid");

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string ComputeHash(string token)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }

    [Fact]
    public async Task FullFlow_SignUp_TryLoginWithoutVerification_ShouldFail()
    {
        // Arrange & Act - SignUp
        var signUpCommand = new SignUpCommand(
            FirstName: "Flow",
            LastName: "Test",
            Email: "flow2@example.com",
            PreferredName: null,
            PhoneNumber: "+2348012345678",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );

        var signUpResponse = await _client.PostAsJsonAsync("/api/auth/signup", signUpCommand);
        signUpResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Try Login without verification
        var loginCommand = new LoginCommand(
            Email: "flow2@example.com",
            Password: "SecurePass123!"
        );

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private void CleanupDatabase()
    {
        // Delete all data in correct order (child tables first if any foreign keys exist)
        // For now, we only have Users table
        _context.Users.RemoveRange(_context.Users);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        // Clean up after each test class
        CleanupDatabase();
        _scope.Dispose();
        _client.Dispose();
    }
}
