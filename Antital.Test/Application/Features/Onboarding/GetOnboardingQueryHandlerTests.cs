using Antital.Application.Features.Onboarding;
using Antital.Application.Features.Onboarding.GetOnboarding;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Onboarding;

public class GetOnboardingQueryHandlerTests
{
    private readonly Mock<IOnboardingUserAccess> _userAccessMock = new();
    private readonly Mock<IUserOnboardingRepository> _onboardingRepoMock = new();
    private readonly Mock<IUserInvestmentProfileRepository> _profileRepoMock = new();
    private readonly Mock<IUserKycRepository> _kycRepoMock = new();
    private readonly GetOnboardingQueryHandler _handler;

    private static readonly User VerifiedUser = new()
    {
        Id = 1,
        Email = "u@test.com",
        FirstName = "John",
        LastName = "Doe",
        PreferredName = "Johnny",
        PhoneNumber = "+234",
        DateOfBirth = new DateTime(1990, 1, 1),
        Nationality = "Nigerian",
        CountryOfResidence = "Nigeria",
        StateOfResidence = "Lagos",
        ResidentialAddress = "123 Main St",
        IsEmailVerified = true,
        PasswordHash = "x",
        UserType = UserTypeEnum.IndividualInvestor
    };

    public GetOnboardingQueryHandlerTests()
    {
        _handler = new GetOnboardingQueryHandler(
            _userAccessMock.Object,
            _onboardingRepoMock.Object,
            _profileRepoMock.Object,
            _kycRepoMock.Object
        );
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync((1, VerifiedUser));
    }

    [Fact]
    public async Task Handle_NoUserId_ThrowsUnauthorized()
    {
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("User is not authenticated."));

        await _handler.Invoking(h => h.Handle(new GetOnboardingQuery(), CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_UserNotEmailVerified_ThrowsForbidden()
    {
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Email must be verified to access onboarding."));

        await _handler.Invoking(h => h.Handle(new GetOnboardingQuery(), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_NoOnboarding_ReturnsDefaultsAndPersonalInfo()
    {
        _onboardingRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserOnboarding?)null);
        _profileRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserInvestmentProfile?)null);
        _kycRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserKyc?)null);

        var result = await _handler.Handle(new GetOnboardingQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CurrentStep.Should().Be(OnboardingStep.InvestorCategory);
        result.Value.Status.Should().Be(OnboardingStatus.Draft);
        result.Value.SubmittedAt.Should().BeNull();
        result.Value.PersonalInfo.Should().NotBeNull();
        result.Value.PersonalInfo!.FullName.Should().Be("John Doe");
        result.Value.PersonalInfo.Email.Should().Be("u@test.com");
        result.Value.LocationInfo!.Nationality.Should().Be("Nigerian");
        result.Value.InvestorProfile.Should().BeNull();
        result.Value.Kyc.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithOnboardingAndProfile_ReturnsAggregatedData()
    {
        var onboarding = new UserOnboarding { UserId = 1, CurrentStep = OnboardingStep.Kyc, Status = OnboardingStatus.Draft };
        var profile = new UserInvestmentProfile { UserId = 1, InvestorCategory = InvestorCategory.Retail, AnnualIncomeRange = "N5m-N10m" };
        _onboardingRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(onboarding);
        _profileRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        _kycRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserKyc?)null);

        var result = await _handler.Handle(new GetOnboardingQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CurrentStep.Should().Be(OnboardingStep.Kyc);
        result.Value.InvestorProfile.Should().NotBeNull();
        result.Value.InvestorProfile!.InvestorCategory.Should().Be(InvestorCategory.Retail);
        result.Value.InvestorProfile.AnnualIncomeRange.Should().Be("N5m-N10m");
    }
}
