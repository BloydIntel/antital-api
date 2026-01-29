using Antital.Application.DTOs.Authentication;
using Antital.Application.Features.Authentication.RefreshToken;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.RefreshToken;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IJwtTokenService> _jwtMock = new();
    private readonly Mock<IAntitalUnitOfWork> _uowMock = new();
    private readonly IConfiguration _configuration;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:RefreshTokenDays", "30" }
            })
            .Build();

        _handler = new RefreshTokenCommandHandler(_userRepoMock.Object, _jwtMock.Object, _uowMock.Object, _configuration);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_ValidRefreshToken_RotatesAndReturnsNewTokens()
    {
        // Arrange
        var refreshToken = "refresh_123";
        var hashed = HashToken(refreshToken);
        var user = BuildUser(hashed, DateTime.UtcNow.AddDays(1));

        _userRepoMock.Setup(r => r.GetByRefreshTokenHashAsync(hashed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _jwtMock.Setup(j => j.GenerateToken(user)).Returns("new_access");

        // Act
        var result = await _handler.Handle(new RefreshTokenCommand(refreshToken), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Token.Should().Be("new_access");
        result.Value.RefreshToken.Should().NotBe(refreshToken);
        _userRepoMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsUnauthorized()
    {
        // Arrange
        var refreshToken = "expired_123";
        var hashed = HashToken(refreshToken);
        var user = BuildUser(hashed, DateTime.UtcNow.AddDays(-1));
        _userRepoMock.Setup(r => r.GetByRefreshTokenHashAsync(hashed, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Func<Task> act = () => _handler.Handle(new RefreshTokenCommand(refreshToken), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_UnknownToken_ThrowsUnauthorized()
    {
        _userRepoMock.Setup(r => r.GetByRefreshTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Func<Task> act = () => _handler.Handle(new RefreshTokenCommand("does_not_exist"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }

    private static User BuildUser(string hash, DateTime? expiresAt)
    {
        var user = new User
        {
            Id = 1,
            Email = "user@example.com",
            PasswordHash = "hash",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            RefreshTokenHash = hash,
            RefreshTokenExpiresAt = expiresAt
        };
        user.Created("Test");
        return user;
    }
}
