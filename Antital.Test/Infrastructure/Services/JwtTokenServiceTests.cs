using Antital.Domain.Enums;
using Antital.Domain.Models;
using Antital.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace Antital.Test.Infrastructure.Services;

public class JwtTokenServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly JwtTokenService _jwtTokenService;

    public JwtTokenServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        
        // Setup default JWT configuration
        _configurationMock.Setup(x => x["Jwt:Key"]).Returns("ThisIsAVeryLongSecretKeyForJwtTokenGeneration123456789");
        _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("AntitalAPI");
        _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("AntitalClient");
        _configurationMock.Setup(x => x["Jwt:TokenExpiryMinutes"]).Returns("15");

        _jwtTokenService = new JwtTokenService(_configurationMock.Object);
    }

    [Fact]
    public void GenerateToken_ValidUser_CreatesValidJWT()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true
        };

        // Act
        var token = _jwtTokenService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            Id = 123,
            Email = "user@example.com",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = false
        };

        // Act
        var token = _jwtTokenService.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        // Assert
        jsonToken.Claims.Should().Contain(c => c.Type == "UserId" && c.Value == "123");
        jsonToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "user@example.com");
        jsonToken.Claims.Should().Contain(c => c.Type == "UserType" && c.Value == UserTypeEnum.IndividualInvestor.ToString());
        jsonToken.Claims.Should().Contain(c => c.Type == "IsEmailVerified" && c.Value == "False");
        jsonToken.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateToken_HasCorrectExpiry()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true
        };
        var expectedExpiry = DateTime.UtcNow.AddMinutes(15);

        // Act
        var token = _jwtTokenService.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        // Assert
        jsonToken.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsClaimsPrincipal()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true
        };
        var token = _jwtTokenService.GenerateToken(user);

        // Act
        var principal = _jwtTokenService.ValidateToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Type == "UserId" && c.Value == "1");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _jwtTokenService.ValidateToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        // Arrange - Create a token that expires in the past
        var expiredConfigMock = new Mock<IConfiguration>();
        expiredConfigMock.Setup(x => x["Jwt:Key"]).Returns("ThisIsAVeryLongSecretKeyForJwtTokenGeneration123456789");
        expiredConfigMock.Setup(x => x["Jwt:Issuer"]).Returns("AntitalAPI");
        expiredConfigMock.Setup(x => x["Jwt:Audience"]).Returns("AntitalClient");
        expiredConfigMock.Setup(x => x["Jwt:TokenExpiryMinutes"]).Returns("-60"); // Expired 60 minutes ago
        
        var expiredService = new JwtTokenService(expiredConfigMock.Object);
        
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true
        };
        var token = expiredService.GenerateToken(user);

        // Use the same service to validate (with ValidateLifetime = true)
        // Act
        var principal = expiredService.ValidateToken(token);

        // Assert - Should be null because token is expired
        principal.Should().BeNull();
    }

    [Fact]
    public void GenerateToken_DifferentUsers_ProducesDifferentTokens()
    {
        // Arrange
        var user1 = new User { Id = 1, Email = "user1@example.com", UserType = UserTypeEnum.IndividualInvestor, IsEmailVerified = true };
        var user2 = new User { Id = 2, Email = "user2@example.com", UserType = UserTypeEnum.CorporateInvestor, IsEmailVerified = false };

        // Act
        var token1 = _jwtTokenService.GenerateToken(user1);
        var token2 = _jwtTokenService.GenerateToken(user2);

        // Assert
        token1.Should().NotBe(token2);
    }
}
