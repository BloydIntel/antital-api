using Antital.Application.Features.Authentication.Logout;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using FluentAssertions;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.Logout;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IAntitalUnitOfWork> _uowMock = new();
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _handler = new LogoutCommandHandler(_userRepoMock.Object, _uowMock.Object);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_ValidToken_Revokes()
    {
        var token = "refresh123";
        var hash = Hash(token);
        var user = BuildUser(hash, DateTime.UtcNow.AddDays(1));
        _userRepoMock.Setup(r => r.GetByRefreshTokenHashAsync(hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new LogoutCommand(token), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.RefreshTokenHash.Should().BeNull();
        user.RefreshTokenExpiresAt.Should().BeNull();
        _userRepoMock.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsUnauthorized()
    {
        _userRepoMock.Setup(r => r.GetByRefreshTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Func<Task> act = () => _handler.Handle(new LogoutCommand("bad"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    private static string Hash(string token)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
    }

    private static User BuildUser(string hash, DateTime? expires)
    {
        var user = new User
        {
            Id = 1,
            Email = "user@example.com",
            PasswordHash = "hash",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            RefreshTokenHash = hash,
            RefreshTokenExpiresAt = expires
        };
        user.Created("Test");
        return user;
    }
}
