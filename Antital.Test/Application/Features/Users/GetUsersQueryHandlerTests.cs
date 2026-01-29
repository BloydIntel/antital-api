using Antital.Application.DTOs;
using Antital.Application.Features.Users.GetUsers;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Users;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly GetUsersQueryHandler _handler;

    public GetUsersQueryHandlerTests()
    {
        _handler = new GetUsersQueryHandler(_userRepo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsMappedUsers()
    {
        var users = new List<User>
        {
            new() { Id = 1, Email = "a@b.com", FirstName = "A", LastName = "B", IsEmailVerified = true },
            new() { Id = 2, Email = "c@d.com", FirstName = "C", LastName = "D", IsEmailVerified = false }
        };

        _userRepo.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var result = await _handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value!.Select(x => x.Email).Should().Contain(new[] { "a@b.com", "c@d.com" });
    }
}
