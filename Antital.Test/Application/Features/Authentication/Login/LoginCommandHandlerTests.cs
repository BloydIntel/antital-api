using Antital.Application.DTOs.Authentication;
using Antital.Application.Features.Authentication.Login;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();

        _handler = new LoginCommandHandler(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object
        );
    }

    [Fact]
    public async Task Handle_SuccessfulLogin_ReturnsAuthResponseDto()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "user@example.com",
            Password: "SecurePass123!"
        );

        var user = new User
        {
            Id = 1,
            Email = command.Email,
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            FirstName = "John",
            LastName = "Doe"
        };

        var jwtToken = "jwt_token_12345";

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(user))
            .Returns(jwtToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Token.Should().Be(jwtToken);
        result.Value.UserId.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
        result.Value.UserType.Should().Be(user.UserType);
        result.Value.IsEmailVerified.Should().BeTrue();

        _userRepositoryMock.Verify(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()), Times.Once);
        _passwordHasherMock.Verify(x => x.VerifyPassword(command.Password, user.PasswordHash), Times.Once);
        _jwtTokenServiceMock.Verify(x => x.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ThrowsNotFoundException()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "nonexistent@example.com",
            Password: "SecurePass123!"
        );

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _passwordHasherMock.Verify(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _jwtTokenServiceMock.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "user@example.com",
            Password: "WrongPassword123!"
        );

        var user = new User
        {
            Id = 1,
            Email = command.Email,
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(false);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();

        _jwtTokenServiceMock.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnverifiedEmail_ThrowsUnauthorizedException()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "user@example.com",
            Password: "SecurePass123!"
        );

        var user = new User
        {
            Id = 1,
            Email = command.Email,
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = false // Not verified
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        // Act
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();

        _jwtTokenServiceMock.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_JwtTokenGeneratedCorrectlyWithVerifiedUserClaims()
    {
        // Arrange
        var command = new LoginCommand(
            Email: "user@example.com",
            Password: "SecurePass123!"
        );

        var user = new User
        {
            Id = 1,
            Email = command.Email,
            PasswordHash = "hashed_password",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            FirstName = "John",
            LastName = "Doe"
        };

        User? capturedUser = null;
        var jwtToken = "jwt_token_12345";

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(command.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(command.Password, user.PasswordHash))
            .Returns(true);

        _jwtTokenServiceMock
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Callback<User>(u => capturedUser = u)
            .Returns(jwtToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Id.Should().Be(user.Id);
        capturedUser.Email.Should().Be(user.Email);
        capturedUser.UserType.Should().Be(user.UserType);
        capturedUser.IsEmailVerified.Should().BeTrue();

        result.Value!.Token.Should().Be(jwtToken);
        result.Value.IsEmailVerified.Should().BeTrue();
    }
}
