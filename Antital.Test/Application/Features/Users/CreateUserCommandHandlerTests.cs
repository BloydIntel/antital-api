using Antital.Application.Features.Users.CreateUser;
using Antital.Application.DTOs;
using Antital.Application.Common.Security;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Users;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IAntitalUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _current = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _current.Setup(x => x.UserName).Returns("tester");
        _hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed");
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _handler = new CreateUserCommandHandler(
            _userRepo.Object,
            _hasher.Object,
            _uow.Object,
            _current.Object
        );
    }

    [Fact]
    public async Task Handle_EmailAvailable_CreatesUser()
    {
        // Arrange
        var cmd = new CreateUserCommand(
            Email: "new@example.com",
            Password: "Password123!",
            FirstName: "New",
            LastName: "User",
            PreferredName: null,
            PhoneNumber: "+123",
            UserType: UserTypeEnum.IndividualInvestor);

        _userRepo.Setup(x => x.EmailExistsAsync(cmd.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be(cmd.Email);
        result.Value.FirstName.Should().Be(cmd.FirstName);
        _userRepo.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmailExists_ThrowsConflict()
    {
        var cmd = new CreateUserCommand("exists@example.com", "Password123!", "A", "B", null, null, UserTypeEnum.IndividualInvestor);
        _userRepo.Setup(x => x.EmailExistsAsync(cmd.Email, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Func<Task> act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _userRepo.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
