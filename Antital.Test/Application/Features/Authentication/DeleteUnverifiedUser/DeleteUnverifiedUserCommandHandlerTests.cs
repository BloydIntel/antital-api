using Antital.Application.Common.Security;
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
    public async Task Handle_ValidOtpUnverifiedUser_DeletesUser()
    {
        var otp = "123456";
        var command = new DeleteUnverifiedUserCommand(
            Email: "user@example.com",
            Otp: otp
        );

        var user = new User
        {
            Id = 1,
            Email = command.Email,
            IsEmailVerified = false,
            UnverifiedOtpHash = TokenGenerator.HashToken(otp),
            UnverifiedOtpCreatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            UnverifiedOtpExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userRepositoryMock.Verify(x => x.DeleteAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        user.UnverifiedOtpHash.Should().BeNull();
        user.UnverifiedOtpCreatedAtUtc.Should().BeNull();
        user.UnverifiedOtpExpiresAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteUnverifiedUserCommand(
            Email: "nonexistent@example.com",
            Otp: "123456"
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
            Otp: "123456"
        );

        var user = new User
        {
            Id = 2,
            Email = command.Email,
            IsEmailVerified = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WrongOtp_ThrowsBadRequestException()
    {
        var command = new DeleteUnverifiedUserCommand(
            Email: "user@example.com",
            Otp: "000000"
        );

        var user = new User
        {
            Id = 3,
            Email = command.Email,
            IsEmailVerified = false,
            UnverifiedOtpHash = TokenGenerator.HashToken("123456"),
            UnverifiedOtpCreatedAtUtc = DateTime.UtcNow.AddMinutes(-1),
            UnverifiedOtpExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExpiredOtp_ThrowsBadRequestException()
    {
        var otp = "123456";
        var command = new DeleteUnverifiedUserCommand(
            Email: "user@example.com",
            Otp: otp
        );

        var user = new User
        {
            Id = 4,
            Email = command.Email,
            IsEmailVerified = false,
            UnverifiedOtpHash = TokenGenerator.HashToken(otp),
            UnverifiedOtpCreatedAtUtc = DateTime.UtcNow.AddMinutes(-15),
            UnverifiedOtpExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1)
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConsumedOtp_ThrowsBadRequestException()
    {
        var command = new DeleteUnverifiedUserCommand(
            Email: "user@example.com",
            Otp: "123456"
        );

        var user = new User
        {
            Id = 5,
            Email = command.Email,
            IsEmailVerified = false,
            UnverifiedOtpHash = null,
            UnverifiedOtpCreatedAtUtc = null,
            UnverifiedOtpExpiresAtUtc = null
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        _userRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
