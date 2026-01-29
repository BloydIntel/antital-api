using Antital.Application.Features.Users.GetUserById;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Users;

public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _handler = new GetUserByIdQueryHandler(_userRepo.Object);
    }

    [Fact]
    public async Task Handle_UserFound_ReturnsDto()
    {
        var user = new User { Id = 5, Email = "x@y.com", FirstName = "X", LastName = "Y", IsEmailVerified = true };
        _userRepo.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await _handler.Handle(new GetUserByIdQuery(5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(5);
        result.Value.Email.Should().Be("x@y.com");
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsNotFound()
    {
        _userRepo.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        Func<Task> act = async () => await _handler.Handle(new GetUserByIdQuery(5), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
