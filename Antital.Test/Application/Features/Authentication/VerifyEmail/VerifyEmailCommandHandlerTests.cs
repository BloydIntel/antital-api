using Antital.Application.Features.Authentication.VerifyEmail;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.VerifyEmail;

public class VerifyEmailCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAntitalUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly VerifyEmailCommandHandler _handler;

    public VerifyEmailCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IAntitalUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _handler = new VerifyEmailCommandHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object
        );
    }

    [Fact]
    public async Task Handle_SuccessfulEmailVerification_UpdatesIsEmailVerifiedToTrue()
    {
        // Arrange
        var command = new VerifyEmailCommand(
            Email: "user@example.com",
            Token: "valid_verification_token_12345"
        );

        var user = new User
        {
            Id = 1,
            Email = command.Email,
            IsEmailVerified = false,
            EmailVerificationToken = command.Token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1) // Not expired
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _currentUserMock.Setup(x => x.UserName).Returns("user@example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue();
        user.EmailVerificationToken.Should().BeNull();
        user.EmailVerificationTokenExpiry.Should().BeNull();

        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsBadRequestException()
    {
        // Arrange
        var command = new VerifyEmailCommand(
            Email: "user@example.com",
            Token: "invalid_token"
        );

        var user = new User
        {
            Id = 1,
            Email = command.Email,
            IsEmailVerified = false,
            EmailVerificationToken = "different_token", // Different token
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1)
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsBadRequestException()
    {
        // Arrange
        var command = new VerifyEmailCommand(
            Email: "user@example.com",
            Token: "expired_token_12345"
        );

        var user = new User
        {
            Id = 1,
            Email = command.Email,
            IsEmailVerified = false,
            EmailVerificationToken = command.Token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1) // Expired
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var command = new VerifyEmailCommand(
            Email: "nonexistent@example.com",
            Token: "any_token"
        );

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TokenAndExpiryFieldsClearedAfterVerification()
    {
        // Arrange
        var command = new VerifyEmailCommand(
            Email: "user@example.com",
            Token: "valid_token_12345"
        );

        var user = new User
        {
            Id = 1,
            Email = command.Email,
            IsEmailVerified = false,
            EmailVerificationToken = command.Token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1)
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _currentUserMock.Setup(x => x.UserName).Returns("user@example.com");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        user.EmailVerificationToken.Should().BeNull();
        user.EmailVerificationTokenExpiry.Should().BeNull();
        user.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NullToken_ThrowsBadRequestException()
    {
        // Arrange
        var command = new VerifyEmailCommand(
            Email: "user@example.com",
            Token: "some_token"
        );

        var user = new User
        {
            Id = 1,
            Email = command.Email,
            IsEmailVerified = false,
            EmailVerificationToken = null, // Null token
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1)
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
    }
}
