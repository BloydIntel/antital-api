using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Infrastructure.Repositories;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Infrastructure.Implementations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Antital.Test.Infrastructure.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly AntitalDBContext _dbContext;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AntitalDBContext(options);
        _currentUserMock = new Mock<ICurrentUser>();
        _currentUserMock.Setup(x => x.UserName).Returns("TestUser");
        
        _repository = new UserRepository(_dbContext, _currentUserMock.Object);
    }

    [Fact]
    public async Task AddAsync_SavesUserToDatabase()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true
        };
        user.Created("TestUser");

        // Act
        await _repository.AddAsync(user, CancellationToken.None);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        var savedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingEmail_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Email = "existing@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "Jane",
            LastName = "Doe",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true
        };
        user.Created("TestUser");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _repository.GetByEmailAsync("existing@example.com", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("existing@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_NonExistingEmail_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@example.com", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_DeletedUser_ReturnsNull()
    {
        // Arrange
        var user = new User
        {
            Email = "deleted@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "Deleted",
            LastName = "User",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true
        };
        user.Created("TestUser");
        user.Deleted("TestUser");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _repository.GetByEmailAsync("deleted@example.com", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Email = "byid@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "ById",
            LastName = "User",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true
        };
        user.Created("TestUser");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _repository.GetByIdAsync(user.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(99999, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EmailExistsAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Email = "exists@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "Exists",
            LastName = "User",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true
        };
        user.Created("TestUser");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _repository.EmailExistsAsync("exists@example.com", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailExistsAsync_NonExistingEmail_ReturnsFalse()
    {
        // Act
        var result = await _repository.EmailExistsAsync("notexists@example.com", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUserCorrectly()
    {
        // Arrange
        var user = new User
        {
            Email = "update@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "Original",
            LastName = "Name",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true
        };
        user.Created("TestUser");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        user.FirstName = "Updated";
        user.Updated("TestUser");
        await _repository.UpdateAsync(user, CancellationToken.None);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        var updatedUser = await _dbContext.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.FirstName.Should().Be("Updated");
        updatedUser.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyEmailAsync_ValidToken_ReturnsTrue()
    {
        // Arrange
        var token = "valid_token_12345";
        var user = new User
        {
            Email = "verify@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "Verify",
            LastName = "User",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true,
            EmailVerificationToken = token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1)
        };
        user.Created("TestUser");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _repository.VerifyEmailAsync("verify@example.com", token, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        await _dbContext.SaveChangesAsync(); // mimic unit of work commit
        var updatedUser = await _dbContext.Users.FirstAsync(u => u.Email == "verify@example.com");
        updatedUser.IsEmailVerified.Should().BeTrue();
        updatedUser.EmailVerificationToken.Should().BeNull();
        updatedUser.EmailVerificationTokenExpiry.Should().BeNull();
    }

    [Fact]
    public async Task VerifyEmailAsync_InvalidToken_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Email = "invalid@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "Invalid",
            LastName = "User",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true,
            EmailVerificationToken = "correct_token",
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1)
        };
        user.Created("TestUser");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _repository.VerifyEmailAsync("invalid@example.com", "wrong_token", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task VerifyEmailAsync_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        var token = "expired_token";
        var user = new User
        {
            Email = "expired@example.com",
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "Expired",
            LastName = "User",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true,
            EmailVerificationToken = token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1) // Expired
        };
        user.Created("TestUser");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _repository.VerifyEmailAsync("expired@example.com", token, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task VerifyEmailAsync_NonExistingEmail_ReturnsFalse()
    {
        // Act
        var result = await _repository.VerifyEmailAsync("nonexistent@example.com", "any_token", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        await _dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByRefreshTokenHashAsync_ReturnsUser()
    {
        // Arrange
        var tokenHash = "abc123";
        var user = new User
        {
            Email = "refresh@example.com",
            PasswordHash = "hashed",
            UserType = UserTypeEnum.IndividualInvestor,
            FirstName = "Refresh",
            LastName = "User",
            PhoneNumber = "+1234567890",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street, Lagos",
            HasAgreedToTerms = true,
            RefreshTokenHash = tokenHash,
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(5)
        };
        user.Created("TestUser");
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _repository.GetByRefreshTokenHashAsync(tokenHash, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(user.Email);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
