using Antital.Application.Features.Authentication.DeleteUnverifiedUser;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.DeleteUnverifiedUser;

public class DeleteUnverifiedUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IAntitalUnitOfWork> _unitOfWorkMock = new();
    private readonly DeleteUnverifiedUserCommandHandler _handler;

    public DeleteUnverifiedUserCommandHandlerTests()
    {
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new DeleteUnverifiedUserCommandHandler(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Fact]
    public async Task Handle_ValidTokenUnverifiedUser_DeletesUser()
    {
        var command = new DeleteUnverifiedUserCommand(
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

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userRepositoryMock.Verify(x => x.DeleteAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteUnverifiedUserCommand(
            Email: "nonexistent@example.com",
            Token: "any_token"
        );

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyVerifiedUser_ThrowsBadRequestException()
    {
        var command = new DeleteUnverifiedUserCommand(
            Email: "verified@example.com",
            Token: "some_token"
        );

        var user = new User
        {
            Id = 2,
            Email = command.Email,
            IsEmailVerified = true,
            EmailVerificationToken = command.Token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1)
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidToken_ThrowsBadRequestException()
    {
        var command = new DeleteUnverifiedUserCommand(
            Email: "user@example.com",
            Token: "wrong_token"
        );

        var user = new User
        {
            Id = 3,
            Email = command.Email,
            IsEmailVerified = false,
            EmailVerificationToken = "correct_token",
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1)
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ThrowsBadRequestException()
    {
        var command = new DeleteUnverifiedUserCommand(
            Email: "user@example.com",
            Token: "expired_token"
        );

        var user = new User
        {
            Id = 4,
            Email = command.Email,
            IsEmailVerified = false,
            EmailVerificationToken = command.Token,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1) // Expired
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullVerificationToken_ThrowsBadRequestException()
    {
        var command = new DeleteUnverifiedUserCommand(
            Email: "user@example.com",
            Token: "some_token"
        );

        var user = new User
        {
            Id = 5,
            Email = command.Email,
            IsEmailVerified = false,
            EmailVerificationToken = null,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(1)
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
