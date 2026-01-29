using Antital.Application.Features.Authentication.ResetPassword;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.ResetPassword;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IAntitalUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _handler = new ResetPasswordCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object
        );
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFound()
    {
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Func<Task> act = () => _handler.Handle(new ResetPasswordCommand("a@b.com", "t", "P@ssw0rd", "P@ssw0rd"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsBadRequest()
    {
        var user = new User
        {
            Email = "user@example.com",
            PasswordResetTokenHash = "hash",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(-1)
        };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        Func<Task> act = () => _handler.Handle(new ResetPasswordCommand(user.Email, "t", "P@ssw0rd", "P@ssw0rd"), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_InvalidTokenHash_ThrowsBadRequest()
    {
        var user = new User
        {
            Email = "user@example.com",
            PasswordResetTokenHash = "different",
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(5)
        };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        Func<Task> act = () => _handler.Handle(new ResetPasswordCommand(user.Email, "wrong", "P@ssw0rd", "P@ssw0rd"), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_ValidToken_UpdatesPasswordAndClearsToken()
    {
        var user = new User
        {
            Email = "user@example.com",
            PasswordResetTokenHash = ComputeHash("token"),
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(5)
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("newhash");
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var cmd = new ResetPasswordCommand(user.Email, "token", "NewP@ssw0rd1", "NewP@ssw0rd1");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("newhash");
        user.PasswordResetTokenHash.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static string ComputeHash(string token)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
