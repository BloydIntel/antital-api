using Antital.Application.Features.Users.DeleteUser;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Users;

public class DeleteUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<IAntitalUnitOfWork> _uow = new();
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _uow.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _handler = new DeleteUserCommandHandler(_userRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_UserExists_Deletes()
    {
        var user = new User { Id = 1 };
        _userRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.Handle(new DeleteUserCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userRepo.Verify(x => x.DeleteAsync(user, It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFound()
    {
        _userRepo.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        Func<Task> act = async () => await _handler.Handle(new DeleteUserCommand(1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
