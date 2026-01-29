using Antital.Application.Features.Users.UpdateUser;
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

public class UpdateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IAntitalUnitOfWork> _uow = new();
    private readonly Mock<ICurrentUser> _current = new();
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _current.Setup(x => x.UserName).Returns("tester");
        _hasher.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("hashed");
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _handler = new UpdateUserCommandHandler(
            _userRepo.Object,
            _hasher.Object,
            _uow.Object,
            _current.Object
        );
    }

    [Fact]
    public async Task Handle_UserExists_UpdatesFields()
    {
        var user = new User { Id = 1, Email = "a@b.com" };
        _userRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var cmd = new UpdateUserCommand(
            Id: 1,
            FirstName: "New",
            LastName: "Name",
            PreferredName: "Pref",
            PhoneNumber: "123",
            UserType: UserTypeEnum.IndividualInvestor,
            IsEmailVerified: true,
            Password: "Password123!");

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.FirstName.Should().Be("New");
        user.LastName.Should().Be("Name");
        user.PreferredName.Should().Be("Pref");
        user.IsEmailVerified.Should().BeTrue();
        user.PasswordHash.Should().Be("hashed");
        _userRepo.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFound()
    {
        _userRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var cmd = new UpdateUserCommand(1, "A", "B", null, null, UserTypeEnum.IndividualInvestor, null, null);

        Func<Task> act = async () => await _handler.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
