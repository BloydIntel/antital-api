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
    private readonly Mock<IUserInvestmentProfileRepository> _profileRepo = new();
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _handler = new GetUserByIdQueryHandler(_userRepo.Object, _profileRepo.Object);
    }

    [Fact]
    public async Task Handle_UserFound_ReturnsDto()
    {
        var user = new User
        {
            Id = 5,
            Email = "x@y.com",
            FirstName = "X",
            LastName = "Y",
            IsEmailVerified = true,
            PhoneNumber = "08012345678",
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "12 Test St",
            DateOfBirth = new DateTime(1990, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            HasAgreedToTerms = true
        };
        _userRepo.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _profileRepo.Setup(x => x.GetByUserIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserInvestmentProfile?)null);

        var result = await _handler.Handle(new GetUserByIdQuery(5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(5);
        result.Value.Email.Should().Be("x@y.com");
        result.Value.Nationality.Should().Be("Nigerian");
        result.Value.DateOfBirth.Should().Be(user.DateOfBirth);
        result.Value.Company.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsNotFound()
    {
        _userRepo.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        Func<Task> act = async () => await _handler.Handle(new GetUserByIdQuery(5), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
