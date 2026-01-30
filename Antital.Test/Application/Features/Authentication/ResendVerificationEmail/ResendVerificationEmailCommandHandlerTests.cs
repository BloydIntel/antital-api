using Antital.Application.Features.Authentication.ResendVerificationEmail;
using Antital.Application.Common.Security;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.ResendVerificationEmail;

public class ResendVerificationEmailCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IAntitalUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly IConfiguration _configuration;
    private readonly ResendVerificationEmailCommandHandler _handler;

    public ResendVerificationEmailCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _unitOfWorkMock = new Mock<IAntitalUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Email:VerificationExpiryHours", "24" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _handler = new ResendVerificationEmailCommandHandler(
            _userRepositoryMock.Object,
            _emailServiceMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _configuration
        );
    }

    [Fact]
    public async Task Handle_UnverifiedUser_SendsNewTokenAndEmail()
    {
        // Arrange
        var command = new ResendVerificationEmailCommand("user@example.com");
        var user = new User
        {
            Id = 1,
            Email = command.Email,
            IsEmailVerified = false,
            EmailVerificationToken = "old-token",
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1)
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.EmailVerificationToken.Should().NotBe("old-token");
        user.EmailVerificationTokenExpiry.Should().BeAfter(DateTime.UtcNow.AddHours(23));

        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(command.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlreadyVerified_ThrowsBadRequest()
    {
        // Arrange
        var command = new ResendVerificationEmailCommand("verified@example.com");
        var user = new User
        {
            Id = 2,
            Email = command.Email,
            IsEmailVerified = true
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>();
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFound()
    {
        // Arrange
        var command = new ResendVerificationEmailCommand("missing@example.com");
        _userRepositoryMock.Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
        _emailServiceMock.Verify(x => x.SendVerificationEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
