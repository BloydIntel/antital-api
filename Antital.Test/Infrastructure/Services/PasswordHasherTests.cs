using Antital.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Infrastructure.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _passwordHasher;

    public PasswordHasherTests()
    {
        _passwordHasher = new PasswordHasher();
    }

    [Fact]
    public void HashPassword_ValidPassword_ReturnsHashedString()
    {
        // Arrange
        var password = "SecurePass123!";

        // Act
        var hash = _passwordHasher.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().NotBe(password);
        hash.Should().StartWith("$2"); // BCrypt hash starts with $2
    }

    [Fact]
    public void HashPassword_NullPassword_ThrowsArgumentException()
    {
        // Arrange
        string? password = null;

        // Act
        Action act = () => _passwordHasher.HashPassword(password!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot be null or empty.*");
    }

    [Fact]
    public void HashPassword_EmptyPassword_ThrowsArgumentException()
    {
        // Arrange
        var password = string.Empty;

        // Act
        Action act = () => _passwordHasher.HashPassword(password);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot be null or empty.*");
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "SecurePass123!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_IncorrectPassword_ReturnsFalse()
    {
        // Arrange
        var password = "SecurePass123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _passwordHasher.HashPassword(password);

        // Act
        var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_SamePassword_ProducesDifferentHashes()
    {
        // Arrange
        var password = "SecurePass123!";

        // Act
        var hash1 = _passwordHasher.HashPassword(password);
        var hash2 = _passwordHasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // Different salts should produce different hashes
        _passwordHasher.VerifyPassword(password, hash1).Should().BeTrue();
        _passwordHasher.VerifyPassword(password, hash2).Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_NullPassword_ReturnsFalse()
    {
        // Arrange
        var hash = _passwordHasher.HashPassword("SomePassword123!");

        // Act
        var result = _passwordHasher.VerifyPassword(null!, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_EmptyPassword_ReturnsFalse()
    {
        // Arrange
        var hash = _passwordHasher.HashPassword("SomePassword123!");

        // Act
        var result = _passwordHasher.VerifyPassword(string.Empty, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_NullHash_ReturnsFalse()
    {
        // Arrange
        var password = "SomePassword123!";

        // Act
        var result = _passwordHasher.VerifyPassword(password, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_EmptyHash_ReturnsFalse()
    {
        // Arrange
        var password = "SomePassword123!";

        // Act
        var result = _passwordHasher.VerifyPassword(password, string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_InvalidHashFormat_ReturnsFalse()
    {
        // Arrange
        var password = "SomePassword123!";
        var invalidHash = "not-a-valid-bcrypt-hash";

        // Act
        var result = _passwordHasher.VerifyPassword(password, invalidHash);

        // Assert
        result.Should().BeFalse();
    }
}
