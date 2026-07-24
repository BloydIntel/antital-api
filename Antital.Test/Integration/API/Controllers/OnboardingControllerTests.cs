using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Antital.Application.DTOs.Onboarding;
using Antital.Domain.Enums;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Test.Integration;
using BuildingBlocks.Application.Features;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Antital.Test.Integration.API.Controllers;

[Collection("IntegrationTests")]
public class OnboardingControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly AntitalDBContext _context;
    private readonly IConfiguration _config;
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public OnboardingControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task Get_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/onboarding");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_EmailNotVerified_Returns403()
    {
        var user = SeedUser(email: "unverified@example.com", isEmailVerified: false);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);

        var response = await authClient.GetAsync("/api/onboarding");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_VerifiedUser_ReturnsProgressAndPersonalInfoFromUser()
    {
        var user = SeedUser(
            email: "onboard@example.com",
            firstName: "Onboard",
            lastName: "Tester",
            preferredName: "OB",
            phoneNumber: "+2349012345678",
            dateOfBirth: new DateTime(1985, 5, 15),
            nationality: "Nigerian",
            countryOfResidence: "Nigeria",
            stateOfResidence: "Lagos",
            residentialAddress: "45 Test Avenue, Lagos"
        );
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);

        var response = await authClient.GetAsync("/api/onboarding");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<OnboardingResponse>>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CurrentStep.Should().Be(OnboardingStep.InvestorCategory);
        result.Value.Status.Should().Be(OnboardingStatus.Draft);
        result.Value.SubmittedAt.Should().BeNull();

        result.Value.PersonalInfo.Should().NotBeNull();
        result.Value.PersonalInfo!.FullName.Should().Be("Onboard Tester");
        result.Value.PersonalInfo.Email.Should().Be(user.Email);
        result.Value.PersonalInfo.PreferredName.Should().Be("OB");
        result.Value.PersonalInfo.PhoneNumber.Should().Be(user.PhoneNumber);
        result.Value.PersonalInfo.DateOfBirth.Should().Be(user.DateOfBirth);

        result.Value.LocationInfo.Should().NotBeNull();
        result.Value.LocationInfo!.Nationality.Should().Be(user.Nationality);
        result.Value.LocationInfo.CountryOfResidence.Should().Be(user.CountryOfResidence);
        result.Value.LocationInfo.StateOfResidence.Should().Be(user.StateOfResidence);
        result.Value.LocationInfo.ResidentialAddress.Should().Be(user.ResidentialAddress);

        result.Value.InvestorProfile.Should().BeNull();
        result.Value.Kyc.Should().BeNull();
    }

    [Fact]
    public async Task Put_InvestorCategoryStep_SavesAndAdvances()
    {
        var user = SeedUser(email: "putcat@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);

        var request = new SaveOnboardingRequest(
            Step: OnboardingStep.InvestorCategory,
            InvestorCategoryPayload: new InvestorCategoryPayload(InvestorCategory.Retail),
            InvestmentProfilePayload: null,
            KycPayload: null
        );

        var putResponse = await authClient.PutAsJsonAsync("/api/onboarding", request, JsonOptions);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await authClient.GetAsync("/api/onboarding");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResult = await getResponse.Content.ReadFromJsonAsync<Result<OnboardingResponse>>(JsonOptions);
        getResult!.Value!.CurrentStep.Should().Be(OnboardingStep.InvestmentProfile);
        getResult.Value.InvestorProfile.Should().NotBeNull();
        getResult.Value.InvestorProfile!.InvestorCategory.Should().Be(InvestorCategory.Retail);
    }

    [Fact]
    public async Task Put_InvestmentProfileStep_Saves()
    {
        var user = SeedUser(email: "putprofile@example.com");
        await _context.SaveChangesAsync(); // Ensure user.Id is set

        var onboarding = new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.InvestmentProfile,
            Status = OnboardingStatus.Draft
        };
        _context.UserOnboardings.Add(onboarding);
        var profile = new UserInvestmentProfile { UserId = user.Id, InvestorCategory = InvestorCategory.Retail };
        _context.UserInvestmentProfiles.Add(profile);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);

        var request = new SaveOnboardingRequest(
            Step: OnboardingStep.InvestmentProfile,
            InvestorCategoryPayload: null,
            InvestmentProfilePayload: new InvestmentProfilePayload(
                InvestorCategory.Retail,
                10m, 20m,
                "N5m-N10m",
                5_000_000m,
                true, true, true, true, true,
                null, null, null, null, null, null, null, null,
                null, null, null, null, null
            ),
            KycPayload: null
        );

        var response = await authClient.PutAsJsonAsync("/api/onboarding", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await _context.UserInvestmentProfiles.AsNoTracking().FirstAsync(p => p.UserId == user.Id);
        updated.AnnualIncomeRange.Should().Be("N5m-N10m");
        updated.NetInvestmentAssetsValue.Should().Be(5_000_000m);
    }

    [Fact]
    public async Task Put_CorporateQiiProfileStep_SavesAndHydratesOnGet()
    {
        var user = SeedUser(email: "corp-qii@example.com", userType: UserTypeEnum.CorporateInvestor);
        await _context.SaveChangesAsync();

        var onboarding = new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.CorporateInvestor,
            CurrentStep = OnboardingStep.InvestmentProfile,
            Status = OnboardingStatus.Draft
        };
        _context.UserOnboardings.Add(onboarding);
        _context.UserInvestmentProfiles.Add(new UserInvestmentProfile { UserId = user.Id, InvestorCategory = InvestorCategory.Retail });
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);

        var request = new SaveOnboardingRequest(
            Step: OnboardingStep.InvestmentProfile,
            InvestorCategoryPayload: null,
            InvestmentProfilePayload: null,
            KycPayload: null,
            CorporateQiiProfilePayload: new CorporateQiiProfilePayload(
                InstitutionTypes: [QiiInstitutionType.AssetManagementCompany, QiiInstitutionType.CorporateFinanceInstitution],
                OtherInstitutionType: null,
                HasValidQiiRegistrationOrLicense: true,
                HasApprovedAlternativeInvestmentMandate: true,
                ConfirmsSecNigeriaQiiCriteria: true
            )
        );

        var response = await authClient.PutAsJsonAsync("/api/onboarding", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await authClient.GetAsync("/api/onboarding");
        var getResult = await getResponse.Content.ReadFromJsonAsync<Result<OnboardingResponse>>(JsonOptions);
        getResult!.Value!.CorporateProfile.Should().NotBeNull();
        getResult.Value.CorporateProfile!.QiiProfile!.InstitutionTypesCommaSeparated.Should().Contain("AssetManagementCompany");
        getResult.Value.CorporateProfile.QiiProfile.HasValidQiiRegistrationOrLicense.Should().BeTrue();
    }

    [Fact]
    public async Task Put_FundRaiserCompanyStep_SavesAndHydratesOnGet()
    {
        var user = SeedUser(email: "fundraiser-company@example.com", userType: UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        var onboarding = new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.Startup,
            CurrentStep = OnboardingStep.InvestorCategory,
            Status = OnboardingStatus.Draft
        };
        _context.UserOnboardings.Add(onboarding);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);

        var request = new SaveOnboardingRequest(
            Step: OnboardingStep.InvestorCategory,
            InvestorCategoryPayload: null,
            InvestmentProfilePayload: null,
            KycPayload: null,
            FundRaiserCompanyPayload: new FundRaiserCompanyPayload(
                CompanyLegalName: "Acme Fundraise Limited",
                TradingBrandName: "Acme Raise",
                RegistrationType: "LTD",
                RegistrationNumber: "RC123456",
                CompanyLoginEmail: "ops@acmefundraise.com",
                DateOfRegistration: new DateTime(2020, 1, 15),
                CompanyWebsite: "https://acmefundraise.com",
                BusinessAddress: "23A Unity Crescent Lekki",
                RegisteredAddress: "23A Unity Crescent Lekki",
                CompanyEmail: "info@acmefundraise.com",
                CompanyPhone: "+2348012345678"
            )
        );

        var putResponse = await authClient.PutAsJsonAsync("/api/onboarding", request, JsonOptions);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await authClient.GetAsync("/api/onboarding");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResult = await getResponse.Content.ReadFromJsonAsync<Result<OnboardingResponse>>(JsonOptions);

        getResult!.Value!.FundRaiserProfile.Should().NotBeNull();
        getResult.Value.FundRaiserProfile!.Company.Should().NotBeNull();
        getResult.Value.FundRaiserProfile.Company!.CompanyLegalName.Should().Be("Acme Fundraise Limited");
        getResult.Value.FundRaiserProfile.Company.TradingBrandName.Should().Be("Acme Raise");
        getResult.Value.FundRaiserProfile.Company.CompanyPhone.Should().Be("+2348012345678");
    }

    [Fact]
    public async Task Put_FundRaiserKycStep_WithRepresentativeAndBusinessDocuments_SavesAndHydratesOnGet()
    {
        var user = SeedUser(email: "fundraiser-kyc@example.com", userType: UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        var onboarding = new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.Startup,
            CurrentStep = OnboardingStep.Kyc,
            Status = OnboardingStatus.Draft
        };
        _context.UserOnboardings.Add(onboarding);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);

        var request = new SaveOnboardingRequest(
            Step: OnboardingStep.Kyc,
            InvestorCategoryPayload: null,
            InvestmentProfilePayload: null,
            KycPayload: new KycPayload(
                KycIdType.NationalIdCard,
                "12345678901",
                "21234567890",
                "gov-id.png",
                "proof-of-address.png",
                "selfie.png",
                null,
                null
            ),
            FundRaiserBusinessDocumentsPayload: new FundRaiserBusinessDocumentsPayload(
                FounderAndTeamIntroductionDocumentPathOrKey: "founders.png",
                FundraisingDeckDocumentPathOrKey: "deck.png",
                InvestmentMemoDocumentPathOrKey: "memo.png",
                TermsOfOfferingDocumentPathOrKey: "terms.png",
                ProductDemoDocumentPathOrKey: "demo.png",
                BusinessDescription: "Business overview",
                BusinessSector: "Technology",
                InstrumentType: "Equity Investment Contracts",
                BusinessSize: "Micro",
                FundingTarget: 10_000_000m,
                InvestmentRound: "Pre-Seed Round"
            ),
            FundRaiserRepresentativePayload: new FundRaiserRepresentativePayload(
                RepresentativeFullName: "John Doe",
                RepresentativeJobTitle: "Director",
                RepresentativePhoneNumber: "+2348011111111",
                RepresentativeDateOfBirth: new DateTime(1990, 1, 1),
                RepresentativeEmail: "john@example.com",
                RepresentativeNationality: "Nigerian",
                RepresentativeCountryOfResidence: "Nigeria",
                RepresentativeAddress: "Lekki, Lagos"
            )
        );

        var putResponse = await authClient.PutAsJsonAsync("/api/onboarding", request, JsonOptions);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await authClient.GetAsync("/api/onboarding");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResult = await getResponse.Content.ReadFromJsonAsync<Result<OnboardingResponse>>(JsonOptions);

        getResult!.Value!.FundRaiserProfile.Should().NotBeNull();
        getResult.Value.FundRaiserProfile!.Representative!.RepresentativeFullName.Should().Be("John Doe");
        getResult.Value.FundRaiserProfile.BusinessDocuments!.FundraisingDeckDocumentPathOrKey.Should().Be("deck.png");
        getResult.Value.FundRaiserProfile.BusinessDocuments.FundingTarget.Should().Be(10_000_000m);
        getResult.Value.Kyc!.GovernmentIdDocumentPathOrKey.Should().Be("gov-id.png");
        getResult.Value.Kyc.ProofOfAddressDocumentPathOrKey.Should().Be("proof-of-address.png");
    }

    [Fact]
    public async Task Put_CorporateOciDocumentsInKycStep_SavesAndHydratesOnGet()
    {
        var user = SeedUser(email: "corp-oci-docs@example.com", userType: UserTypeEnum.CorporateInvestor);
        await _context.SaveChangesAsync();

        var onboarding = new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.CorporateInvestor,
            CurrentStep = OnboardingStep.Kyc,
            Status = OnboardingStatus.Draft
        };
        _context.UserOnboardings.Add(onboarding);
        _context.UserKycs.Add(new UserKyc { UserId = user.Id, IdType = KycIdType.NationalIdCard });
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);

        var request = new SaveOnboardingRequest(
            Step: OnboardingStep.Kyc,
            InvestorCategoryPayload: null,
            InvestmentProfilePayload: null,
            KycPayload: null,
            CorporateOciDocumentsPayload: new CorporateOciDocumentsPayload(
                IncorporationCertificateDocumentPathOrKey: "docs/inc.pdf",
                RecentStatusReportDocumentPathOrKey: "docs/status.pdf",
                BoardResolutionDocumentPathOrKey: "docs/board.pdf"
            )
        );

        var response = await authClient.PutAsJsonAsync("/api/onboarding", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await authClient.GetAsync("/api/onboarding");
        var getResult = await getResponse.Content.ReadFromJsonAsync<Result<OnboardingResponse>>(JsonOptions);
        getResult!.Value!.Kyc!.IncorporationCertificateDocumentPathOrKey.Should().Be("docs/inc.pdf");
        getResult.Value.Kyc.RecentStatusReportDocumentPathOrKey.Should().Be("docs/status.pdf");
        getResult.Value.Kyc.BoardResolutionDocumentPathOrKey.Should().Be("docs/board.pdf");
    }

    [Fact]
    public async Task Post_Submit_WhenComplete_SetsSubmitted()
    {
        var user = SeedUser(email: "submit@example.com");
        await _context.SaveChangesAsync();

        var profile = new UserInvestmentProfile { UserId = user.Id, InvestorCategory = InvestorCategory.Retail };
        _context.UserInvestmentProfiles.Add(profile);
        var onboarding = new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.Review,
            Status = OnboardingStatus.Draft
        };
        _context.UserOnboardings.Add(onboarding);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);

        var response = await authClient.PostAsync("/api/onboarding/submit", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await _context.UserOnboardings.AsNoTracking().FirstAsync(o => o.UserId == user.Id);
        updated.Status.Should().Be(OnboardingStatus.Submitted);
        updated.CurrentStep.Should().Be(OnboardingStep.Submitted);
        updated.SubmittedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_Submit_WithoutProfile_Returns400()
    {
        var user = SeedUser(email: "noprofile@example.com");
        await _context.SaveChangesAsync();

        var onboarding = new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.InvestorCategory,
            Status = OnboardingStatus.Draft
        };
        _context.UserOnboardings.Add(onboarding);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);

        var response = await authClient.PostAsync("/api/onboarding/submit", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_FundRaiserReviewStep_WithPaymentPayload_SavesAndHydratesOnGet()
    {
        var user = SeedUser(email: "fundraiser-payment@example.com", userType: UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        _context.UserOnboardings.Add(new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.Startup,
            CurrentStep = OnboardingStep.Review,
            Status = OnboardingStatus.Draft
        });
        _context.UserInvestmentProfiles.Add(new UserInvestmentProfile
        {
            UserId = user.Id,
            FundRaiserApplicationFeePaid = true,
            FundRaiserPaymentMethod = "card",
            FundRaiserPaymentReference = "ANT-FEE-seed",
            FundRaiserPaymentStatus = "Paid",
        });
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);
        var request = new SaveOnboardingRequest(
            Step: OnboardingStep.Review,
            InvestorCategoryPayload: null,
            InvestmentProfilePayload: null,
            KycPayload: null,
            FundRaiserPaymentPayload: new FundRaiserPaymentPayload(
                PaymentMethod: "Bank Transfer",
                PaymentReference: "PAY-001",
                PaymentStatus: "Paid",
                ApplicationFeePaid: true
            )
        );

        var response = await authClient.PutAsJsonAsync("/api/onboarding", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await authClient.GetAsync("/api/onboarding");
        var getResult = await getResponse.Content.ReadFromJsonAsync<Result<OnboardingResponse>>(JsonOptions);
        getResult!.Value!.FundRaiserProfile!.Payment!.PaymentMethod.Should().Be("Bank Transfer");
        getResult.Value.FundRaiserProfile.Payment.PaymentReference.Should().Be("PAY-001");
        getResult.Value.FundRaiserProfile.Payment.ApplicationFeePaid.Should().BeTrue();
    }

    [Fact]
    public async Task Post_Submit_FundRaiserWithoutPayment_Returns400()
    {
        var user = SeedUser(email: "fundraiser-submit-missing-payment@example.com", userType: UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        _context.UserInvestmentProfiles.Add(new UserInvestmentProfile
        {
            UserId = user.Id,
            CompanyLegalName = "Acme",
            TradingBrandName = "Acme",
            RegistrationType = "LTD",
            RegistrationNumber = "RC123",
            CompanyLoginEmail = "ops@acme.com",
            DateOfRegistration = new DateTime(2020, 1, 1),
            BusinessAddress = "Address",
            RegisteredAddress = "Address",
            CompanyEmail = "info@acme.com",
            CompanyPhone = "+234",
            RepresentativeFullName = "Rep",
            RepresentativeJobTitle = "Director",
            RepresentativePhoneNumber = "+234",
            RepresentativeDateOfBirth = new DateTime(1990, 1, 1),
            RepresentativeEmail = "rep@acme.com",
            RepresentativeNationality = "Nigerian",
            RepresentativeCountryOfResidence = "Nigeria",
            RepresentativeAddress = "Address",
            FounderAndTeamIntroductionDocumentPathOrKey = "founders.png",
            FundraisingDeckDocumentPathOrKey = "deck.png",
            InvestmentMemoDocumentPathOrKey = "memo.png",
            TermsOfOfferingDocumentPathOrKey = "terms.png",
            BusinessDescription = "Business",
            BusinessSector = "Technology",
            InstrumentType = "Equity",
            BusinessSize = "Micro",
            FundingTarget = 100000m,
            InvestmentRound = "Pre-Seed"
        });
        _context.UserKycs.Add(new UserKyc
        {
            UserId = user.Id,
            IdType = KycIdType.NationalIdCard,
            GovernmentIdDocumentPathOrKey = "gov.png",
            ProofOfAddressDocumentPathOrKey = "proof.png"
        });
        _context.UserOnboardings.Add(new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.Startup,
            CurrentStep = OnboardingStep.Review,
            Status = OnboardingStatus.Draft
        });
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);
        var response = await authClient.PostAsync("/api/onboarding/submit", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Submit_FundRaiserComplete_Returns200()
    {
        var user = SeedUser(email: "fundraiser-submit-complete@example.com", userType: UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        _context.UserInvestmentProfiles.Add(new UserInvestmentProfile
        {
            UserId = user.Id,
            CompanyLegalName = "Acme",
            TradingBrandName = "Acme",
            RegistrationType = "LTD",
            RegistrationNumber = "RC123",
            CompanyLoginEmail = "ops@acme.com",
            DateOfRegistration = new DateTime(2020, 1, 1),
            BusinessAddress = "Address",
            RegisteredAddress = "Address",
            CompanyEmail = "info@acme.com",
            CompanyPhone = "+234",
            RepresentativeFullName = "Rep",
            RepresentativeJobTitle = "Director",
            RepresentativePhoneNumber = "+234",
            RepresentativeDateOfBirth = new DateTime(1990, 1, 1),
            RepresentativeEmail = "rep@acme.com",
            RepresentativeNationality = "Nigerian",
            RepresentativeCountryOfResidence = "Nigeria",
            RepresentativeAddress = "Address",
            FounderAndTeamIntroductionDocumentPathOrKey = "founders.png",
            FundraisingDeckDocumentPathOrKey = "deck.png",
            InvestmentMemoDocumentPathOrKey = "memo.png",
            TermsOfOfferingDocumentPathOrKey = "terms.png",
            BusinessDescription = "Business",
            BusinessSector = "Technology",
            InstrumentType = "Equity",
            BusinessSize = "Micro",
            FundingTarget = 100000m,
            InvestmentRound = "Pre-Seed",
            FundRaiserPaymentMethod = "Bank Transfer",
            FundRaiserPaymentReference = "PAY-100",
            FundRaiserPaymentStatus = "Paid",
            FundRaiserApplicationFeePaid = true
        });
        _context.UserKycs.Add(new UserKyc
        {
            UserId = user.Id,
            IdType = KycIdType.NationalIdCard,
            GovernmentIdDocumentPathOrKey = "gov.png",
            ProofOfAddressDocumentPathOrKey = "proof.png"
        });
        _context.UserOnboardings.Add(new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.Startup,
            CurrentStep = OnboardingStep.Review,
            Status = OnboardingStatus.Draft
        });
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(userId: user.Id);
        var response = await authClient.PostAsync("/api/onboarding/submit", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Put_WithoutAuth_Returns401()
    {
        var request = new SaveOnboardingRequest(
            OnboardingStep.InvestorCategory,
            new InvestorCategoryPayload(InvestorCategory.Retail),
            null,
            null
        );
        var response = await _client.PutAsJsonAsync("/api/onboarding", request, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private User SeedUser(
        string email,
        bool isEmailVerified = true,
        UserTypeEnum userType = UserTypeEnum.IndividualInvestor,
        string firstName = "Test",
        string lastName = "User",
        string? preferredName = null,
        string phoneNumber = "+2348012345678",
        DateTime? dateOfBirth = null,
        string nationality = "Nigerian",
        string countryOfResidence = "Nigeria",
        string stateOfResidence = "Lagos",
        string residentialAddress = "123 Main Street")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            UserType = userType,
            IsEmailVerified = isEmailVerified,
            FirstName = firstName,
            LastName = lastName,
            PreferredName = preferredName,
            PhoneNumber = phoneNumber,
            DateOfBirth = dateOfBirth ?? new DateTime(1990, 1, 1),
            Nationality = nationality,
            CountryOfResidence = countryOfResidence,
            StateOfResidence = stateOfResidence,
            ResidentialAddress = residentialAddress,
            HasAgreedToTerms = true
        };
        _context.Users.Add(user);
        return user;
    }

    private HttpClient CreateAuthorizedClient(int userId)
    {
        var client = _factory.CreateClient();
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            Expires = DateTime.UtcNow.AddHours(1),
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("UserId", userId.ToString()),
                new Claim(ClaimTypes.Email, "onboarding@test.com")
            }),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        var token = tokenHandler.CreateToken(descriptor);
        var jwt = tokenHandler.WriteToken(token);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
        return client;
    }

    private void CleanupDatabase()
    {
        _context.UserKycs.RemoveRange(_context.UserKycs);
        _context.UserInvestmentProfiles.RemoveRange(_context.UserInvestmentProfiles);
        _context.UserOnboardings.RemoveRange(_context.UserOnboardings);
        _context.SaveChanges(); // Delete children first (FK to Users)
        _context.Users.RemoveRange(_context.Users);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        CleanupDatabase();
        _scope.Dispose();
        _client.Dispose();
    }
}
