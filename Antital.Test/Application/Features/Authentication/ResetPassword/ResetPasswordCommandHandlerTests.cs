using Antital.Application.Common.Security;
using Antital.Application.Features.Authentication.ResetPassword;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.ResetPassword;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IAntitalUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ResetTokenProtector _protector;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Key", "ThisIsAVeryLongSecretKeyForJwtTokenGeneration123456789" },
                { "Jwt:Issuer", "AntitalAPI" },
                { "Jwt:Audience", "AntitalClient" }
            })
            .Build();

        _protector = new ResetTokenProtector(config);
        _handler = new ResetPasswordCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _protector);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFound()
    {
        var token = _protector.Protect("missing@example.com", "raw", DateTime.UtcNow.AddMinutes(10));

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Func<Task> act = () => _handler.Handle(new ResetPasswordCommand(token, "P@ssw0rd", "P@ssw0rd"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InvalidTokenHash_ThrowsBadRequest()
    {
        var token = _protector.Protect("user@example.com", "raw", DateTime.UtcNow.AddMinutes(10));
        var user = new User
        {
            Email = "user@example.com",
            PasswordResetTokenHash = TokenGenerator.HashToken("different"),
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15)
        };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        Func<Task> act = () => _handler.Handle(new ResetPasswordCommand(token, "P@ssw0rd", "P@ssw0rd"), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_ValidToken_UpdatesPasswordAndClearsToken()
    {
        var raw = "raw-token";
        var token = _protector.Protect("user@example.com", raw, DateTime.UtcNow.AddMinutes(10));
        var user = new User
        {
            Email = "user@example.com",
            PasswordResetTokenHash = TokenGenerator.HashToken(raw),
            PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(15)
        };
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("newhash");
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await _handler.Handle(new ResetPasswordCommand(token, "NewP@ssw0rd1", "NewP@ssw0rd1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.PasswordHash.Should().Be("newhash");
        user.PasswordResetTokenHash.Should().BeNull();
        user.PasswordResetTokenExpiry.Should().BeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
