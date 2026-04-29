using Antital.Application.DTOs.Onboarding;
using Antital.Application.Features.Onboarding;
using Antital.Application.Features.Onboarding.SaveOnboarding;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Onboarding;

public class SaveOnboardingCommandHandlerTests
{
    private readonly Mock<IOnboardingUserAccess> _userAccessMock = new();
    private readonly Mock<IAntitalUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IUserOnboardingRepository> _onboardingRepoMock = new();
    private readonly Mock<IUserInvestmentProfileRepository> _profileRepoMock = new();
    private readonly Mock<IUserKycRepository> _kycRepoMock = new();
    private readonly Mock<IKycVerificationService> _kycVerificationServiceMock = new();
    private readonly SaveOnboardingCommandHandler _handler;

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

    public SaveOnboardingCommandHandlerTests()
    {
        _handler = new SaveOnboardingCommandHandler(
            _userAccessMock.Object,
            _unitOfWorkMock.Object,
            _onboardingRepoMock.Object,
            _profileRepoMock.Object,
            _kycRepoMock.Object,
            _kycVerificationServiceMock.Object
        );
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync((1, VerifiedUser));
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _kycVerificationServiceMock
            .Setup(x => x.ProcessAsync(It.IsAny<KycVerificationInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KycVerificationInput input, CancellationToken _) => new KycVerificationResult(
                input.GovernmentIdDocumentPathOrKey,
                input.ProofOfAddressDocumentPathOrKey,
                input.SelfieVerificationPathOrKey,
                input.IncomeVerificationPathOrKey,
                null, null, null, null));
    }

    [Fact]
    public async Task Handle_NoUserId_ThrowsUnauthorized()
    {
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("User is not authenticated."));

        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestorCategory,
            new InvestorCategoryPayload(InvestorCategory.Retail),
            null,
            null
        );

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_UserNotEmailVerified_ThrowsForbidden()
    {
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("Email must be verified to access onboarding."));

        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestorCategory,
            new InvestorCategoryPayload(InvestorCategory.Retail),
            null,
            null
        );

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Handle_InvestorCategoryStep_CreatesOnboardingAndProfile_Success()
    {
        var onboarding = new UserOnboarding { UserId = 1, CurrentStep = OnboardingStep.InvestorCategory, Status = OnboardingStatus.Draft };
        _onboardingRepoMock.Setup(x => x.GetOrCreateForUserAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(onboarding);
        _profileRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserInvestmentProfile?)null);

        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestorCategory,
            new InvestorCategoryPayload(InvestorCategory.Retail),
            null,
            null
        );

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _onboardingRepoMock.Verify(x => x.GetOrCreateForUserAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _onboardingRepoMock.Verify(x => x.UpdateAsync(It.Is<UserOnboarding>(e => e.CurrentStep == OnboardingStep.InvestmentProfile), It.IsAny<CancellationToken>()), Times.Once);
        _profileRepoMock.Verify(x => x.AddAsync(It.Is<UserInvestmentProfile>(p => p.InvestorCategory == InvestorCategory.Retail), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvestmentProfileStep_UpdatesProfile_Success()
    {
        var existingOnboarding = new UserOnboarding { UserId = 1, CurrentStep = OnboardingStep.InvestorCategory, Status = OnboardingStatus.Draft };
        var existingProfile = new UserInvestmentProfile { UserId = 1, InvestorCategory = InvestorCategory.Retail };
        _onboardingRepoMock.Setup(x => x.GetOrCreateForUserAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingOnboarding);
        _profileRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingProfile);

        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestmentProfile,
            null,
            new InvestmentProfilePayload(
                InvestorCategory.Retail,
                10m, 15m,
                "N5m-N10m",
                5_000_000m,
                true, true, true, true, true,
                null, null, null, null, null, null, null, null,
                null, null, null, null, null
            ),
            null
        );

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _profileRepoMock.Verify(x => x.UpdateAsync(It.IsAny<UserInvestmentProfile>(), It.IsAny<CancellationToken>()), Times.Once);
        _onboardingRepoMock.Verify(x => x.UpdateAsync(It.Is<UserOnboarding>(e => e.CurrentStep == OnboardingStep.Kyc), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_KycStep_AddsOrUpdatesKyc_Success()
    {
        var existingOnboarding = new UserOnboarding { UserId = 1, CurrentStep = OnboardingStep.Kyc, Status = OnboardingStatus.Draft };
        _onboardingRepoMock.Setup(x => x.GetOrCreateForUserAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingOnboarding);
        _kycRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserKyc?)null);

        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            new KycPayload(KycIdType.NationalIdCard, "12345678901", "21234567890", "path1", "path2", null, null, null)
        );

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _kycRepoMock.Verify(x => x.AddAsync(It.Is<UserKyc>(k => k.Nin == "12345678901" && k.Bvn == "21234567890"), It.IsAny<CancellationToken>()), Times.Once);
        _onboardingRepoMock.Verify(x => x.UpdateAsync(It.Is<UserOnboarding>(e => e.CurrentStep == OnboardingStep.Review), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvestorCategoryStep_WithCorporateCompanyInfo_UpdatesProfile_WithoutAdvancingStep()
    {
        var corporateUser = new User
        {
            Id = 1,
            Email = "corp@test.com",
            IsEmailVerified = true,
            FirstName = "Corp",
            LastName = "User",
            PhoneNumber = "+1",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Nationality = "NG",
            CountryOfResidence = "NG",
            StateOfResidence = "Lagos",
            ResidentialAddress = "Addr",
            PasswordHash = "x",
            UserType = UserTypeEnum.CorporateInvestor
        };
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync((1, corporateUser));

        var existingOnboarding = new UserOnboarding
        {
            UserId = 1,
            CurrentStep = OnboardingStep.InvestorCategory,
            Status = OnboardingStatus.Draft
        };
        _onboardingRepoMock.Setup(x => x.GetOrCreateForUserAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingOnboarding);
        _profileRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserInvestmentProfile?)null);

        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestorCategory,
            null,
            null,
            null,
            CorporateCompanyPayload: new CorporateCompanyPayload(
                CompanyLegalName: "Acme Ventures Limited",
                TradingBrandName: "Acme Ventures",
                RegistrationType: "LTD",
                RegistrationNumber: "RC123456",
                CompanyLoginEmail: "ops@acmeventures.com"
            ),
            CorporateAddressPayload: new CorporateAddressPayload(
                DateOfRegistration: new DateTime(2020, 1, 15),
                CompanyWebsite: "https://acmeventures.com",
                BusinessAddress: "23A Unity Crescent Lekki",
                RegisteredAddress: "23A Unity Crescent Lekki",
                CompanyEmail: "info@acmeventures.com",
                CompanyPhone: "+2348012345678"
            ),
            CorporateRepresentativePayload: new CorporateRepresentativePayload(
                RepresentativeFullName: "Jane Doe",
                RepresentativeJobTitle: "Director",
                RepresentativePhoneNumber: "+2348098765432",
                RepresentativeDateOfBirth: new DateTime(1990, 5, 10),
                RepresentativeEmail: "jane@acmeventures.com",
                RepresentativeNationality: "Nigerian",
                RepresentativeCountryOfResidence: "Nigeria",
                RepresentativeAddress: "Lekki, Lagos"
            )
        );

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _profileRepoMock.Verify(x => x.AddAsync(It.Is<UserInvestmentProfile>(p =>
            p.CompanyLegalName == "Acme Ventures Limited" &&
            p.TradingBrandName == "Acme Ventures" &&
            p.RegistrationType == "LTD" &&
            p.RegistrationNumber == "RC123456" &&
            p.CompanyLoginEmail == "ops@acmeventures.com" &&
            p.CompanyWebsite == "https://acmeventures.com" &&
            p.BusinessAddress == "23A Unity Crescent Lekki" &&
            p.RegisteredAddress == "23A Unity Crescent Lekki" &&
            p.CompanyEmail == "info@acmeventures.com" &&
            p.CompanyPhone == "+2348012345678" &&
            p.RepresentativeFullName == "Jane Doe" &&
            p.RepresentativeJobTitle == "Director" &&
            p.RepresentativePhoneNumber == "+2348098765432" &&
            p.RepresentativeEmail == "jane@acmeventures.com"), It.IsAny<CancellationToken>()), Times.Once);
        _onboardingRepoMock.Verify(x => x.UpdateAsync(It.Is<UserOnboarding>(e => e.CurrentStep == OnboardingStep.InvestorCategory), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvestmentProfileStep_WithCorporateQiiPayload_UpdatesCorporateProfile()
    {
        var corporateUser = new User
        {
            Id = 1,
            Email = "corp@test.com",
            IsEmailVerified = true,
            FirstName = "Corp",
            LastName = "User",
            PhoneNumber = "+1",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Nationality = "NG",
            CountryOfResidence = "NG",
            StateOfResidence = "Lagos",
            ResidentialAddress = "Addr",
            PasswordHash = "x",
            UserType = UserTypeEnum.CorporateInvestor
        };
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync((1, corporateUser));

        var existingOnboarding = new UserOnboarding { UserId = 1, CurrentStep = OnboardingStep.InvestorCategory, Status = OnboardingStatus.Draft };
        var existingProfile = new UserInvestmentProfile { UserId = 1 };
        _onboardingRepoMock.Setup(x => x.GetOrCreateForUserAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingOnboarding);
        _profileRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingProfile);

        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestmentProfile,
            null,
            null,
            null,
            CorporateQiiProfilePayload: new CorporateQiiProfilePayload(
                [QiiInstitutionType.AssetManagementCompany],
                null,
                true,
                true,
                true
            )
        );

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _profileRepoMock.Verify(x => x.UpdateAsync(It.Is<UserInvestmentProfile>(p =>
            p.QiiInstitutionTypes == "AssetManagementCompany" &&
            p.HasValidQiiRegistrationOrLicense == true &&
            p.ConfirmsSecNigeriaQiiCriteria == true), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_KycStep_WithCorporateOciDocs_UpdatesCorporateKycDocs()
    {
        var corporateUser = new User
        {
            Id = 1,
            Email = "corp@test.com",
            IsEmailVerified = true,
            FirstName = "Corp",
            LastName = "User",
            PhoneNumber = "+1",
            DateOfBirth = DateTime.UtcNow.AddYears(-30),
            Nationality = "NG",
            CountryOfResidence = "NG",
            StateOfResidence = "Lagos",
            ResidentialAddress = "Addr",
            PasswordHash = "x",
            UserType = UserTypeEnum.CorporateInvestor
        };
        _userAccessMock.Setup(x => x.RequireVerifiedUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync((1, corporateUser));

        var existingOnboarding = new UserOnboarding { UserId = 1, CurrentStep = OnboardingStep.Kyc, Status = OnboardingStatus.Draft };
        var existingKyc = new UserKyc { UserId = 1, IdType = KycIdType.NationalIdCard };
        _onboardingRepoMock.Setup(x => x.GetOrCreateForUserAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingOnboarding);
        _kycRepoMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingKyc);

        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            null,
            CorporateOciDocumentsPayload: new CorporateOciDocumentsPayload(
                "inc-path",
                "status-path",
                "board-path"
            )
        );

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _kycRepoMock.Verify(x => x.UpdateAsync(It.Is<UserKyc>(k =>
            k.IncorporationCertificateDocumentPathOrKey == "inc-path" &&
            k.RecentStatusReportDocumentPathOrKey == "status-path" &&
            k.BoardResolutionDocumentPathOrKey == "board-path"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
