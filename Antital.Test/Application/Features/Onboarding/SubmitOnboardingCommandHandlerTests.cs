using Antital.Application.Features.Onboarding;
using Antital.Application.Features.Onboarding.SubmitOnboarding;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Onboarding;

public class SubmitOnboardingCommandHandlerTests
{
    private readonly Mock<IOnboardingUserAccess> _userAccessMock = new();
    private readonly Mock<IAntitalCurrentUser> _currentUserMock = new();
    private readonly Mock<IAntitalUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserOnboardingRepository> _onboardingRepoMock = new();
    private readonly Mock<IUserInvestmentProfileRepository> _profileRepoMock = new();
    private readonly SubmitOnboardingCommandHandler _handler;

    private static readonly User VerifiedUser = new()
    {
        Id = 1,
        Email = "u@test.com",
        IsEmailVerified = true,
        FirstName = "A",
        LastName = "B",
        PhoneNumber = "+1",
        DateOfBirth = DateTime.UtcNow,
        Nationality = "NG",
        CountryOfResidence = "NG",
        StateOfResidence = "Lagos",
        ResidentialAddress = "Addr",
        PasswordHash = "x",
        UserType = UserTypeEnum.IndividualInvestor
    };

    public SubmitOnboardingCommandHandlerTests()
    {
        _handler = new SubmitOnboardingCommandHandler(
            _userAccessMock.Object,
            _currentUserMock.Object,
            _unitOfWorkMock.Object,
            _onboardingRepoMock.Object,
            _profileRepoMock.Object
        );
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync((1, VerifiedUser));
        _currentUserMock.Setup(x => x.UserName).Returns("u@test.com");
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Fact]
    public async Task Handle_NoUserId_ThrowsUnauthorized()
    {
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("User is not authenticated."));

        await _handler.Invoking(h => h.Handle(new SubmitOnboardingCommand(), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_UserNotEmailVerified_ThrowsForbidden()
    {
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Email must be verified to access onboarding."));

        await _handler.Invoking(h => h.Handle(new SubmitOnboardingCommand(), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_NoOnboarding_ThrowsBadRequest()
    {
        _onboardingRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserOnboarding?)null);

        await _handler.Invoking(h => h.Handle(new SubmitOnboardingCommand(), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_AlreadySubmitted_ThrowsBadRequest()
    {
        var onboarding = new UserOnboarding { UserId = 1, Status = OnboardingStatus.Submitted };
        _onboardingRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(onboarding);
        _profileRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new UserInvestmentProfile { UserId = 1 });

        await _handler.Invoking(h => h.Handle(new SubmitOnboardingCommand(), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_NoProfile_ThrowsBadRequest()
    {
        var onboarding = new UserOnboarding { UserId = 1, Status = OnboardingStatus.Draft };
        _onboardingRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(onboarding);
        _profileRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserInvestmentProfile?)null);

        await _handler.Invoking(h => h.Handle(new SubmitOnboardingCommand(), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Handle_Complete_Success()
    {
        var onboarding = new UserOnboarding { UserId = 1, Status = OnboardingStatus.Draft, CurrentStep = OnboardingStep.Review };
        var profile = new UserInvestmentProfile { UserId = 1, InvestorCategory = InvestorCategory.Retail };
        _onboardingRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(onboarding);
        _profileRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(profile);

        var result = await _handler.Handle(new SubmitOnboardingCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _onboardingRepoMock.Verify(x => x.UpdateAsync(It.Is<UserOnboarding>(e =>
            e.Status == OnboardingStatus.Submitted &&
            e.CurrentStep == OnboardingStep.Submitted &&
            e.SubmittedAt != null
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
