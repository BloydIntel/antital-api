using Antital.Application.Common.Security;
using Antital.Application.Features.Authentication.RequestUnverifiedUserOtp;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.RequestUnverifiedUserOtp;

public class RequestUnverifiedUserOtpCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IAntitalUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly RequestUnverifiedUserOtpCommandHandler _handler;

    public RequestUnverifiedUserOtpCommandHandlerTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Email:UnverifiedOtpExpiryMinutes", "10" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new RequestUnverifiedUserOtpCommandHandler(
            _userRepositoryMock.Object,
            _emailServiceMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            configuration
        );
    }

    [Fact]
    public async Task Handle_ExistingUnverifiedUser_GeneratesOtpAndSendsEmail()
    {
        var command = new RequestUnverifiedUserOtpCommand("user@example.com");
        var user = new User
        {
            Id = 1,
            Email = command.Email,
            IsEmailVerified = false
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.UnverifiedOtpHash.Should().NotBeNullOrWhiteSpace();
        user.UnverifiedOtpCreatedAtUtc.Should().NotBeNull();
        user.UnverifiedOtpExpiresAtUtc.Should().NotBeNull();
        user.UnverifiedOtpExpiresAtUtc.Should().BeAfter(user.UnverifiedOtpCreatedAtUtc!.Value);

        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(
            x => x.SendUnverifiedOtpEmailAsync(command.Email, It.Is<string>(otp => otp.Length == 6 && otp.All(char.IsDigit)), 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserDoesNotExist_ThrowsNotFoundException()
    {
        var command = new RequestUnverifiedUserOtpCommand("missing@example.com");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailServiceMock.Verify(
            x => x.SendUnverifiedOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UserAlreadyVerified_ThrowsBadRequestException()
    {
        var command = new RequestUnverifiedUserOtpCommand("verified@example.com");
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
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _emailServiceMock.Verify(
            x => x.SendUnverifiedOtpEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_StoresHashedOtp_NotPlaintext()
    {
        var command = new RequestUnverifiedUserOtpCommand("user@example.com");
        var user = new User
        {
            Id = 3,
            Email = command.Email,
            IsEmailVerified = false
        };
        string? sentOtp = null;

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _emailServiceMock
            .Setup(x => x.SendUnverifiedOtpEmailAsync(command.Email, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, int, CancellationToken>((_, otp, _, _) => sentOtp = otp)
            .Returns(Task.CompletedTask);

        await _handler.Handle(command, CancellationToken.None);

        sentOtp.Should().NotBeNullOrWhiteSpace();
        user.UnverifiedOtpHash.Should().NotBe(sentOtp);
        TokenGenerator.VerifyTokenHash(sentOtp!, user.UnverifiedOtpHash!).Should().BeTrue();
    }
}
