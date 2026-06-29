using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Antital.Application.DTOs.Investors;
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
public class InvestorAccountControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly AntitalDBContext _context;
    private readonly IConfiguration _config;
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public InvestorAccountControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task GetAccount_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/investors/me/account");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAccount_NewInvestor_ReturnsPendingDefaults()
    {
        var user = SeedUser("account-new@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/account");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<InvestorAccountResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.AccountType.Should().Be("Ordinary Investor");
        result.Value.AccountStatus.Should().Be("Pending");
        result.Value.KycStatus.Should().Be("Pending");
        result.Value.KycCompletedDate.Should().BeNull();
        result.Value.InvestorClassification.Should().Be("Ordinary");
        result.Value.VerificationStatus.Should().Be("Verified");
        result.Value.RiskRating.Should().Be("Low");
        result.Value.InvestmentLimits.Should().BeNull();
        result.Value.ComplianceChecks.Should().HaveCount(3);
        result.Value.MemberSince.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetAccount_WithRetailProfileAndCompletedKyc_ReturnsActiveAccount()
    {
        var user = SeedUser("account-complete@example.com");
        await _context.SaveChangesAsync();

        var completedAt = new DateTime(2024, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        _context.UserOnboardings.Add(new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.Review,
            Status = OnboardingStatus.Activated,
            SubmittedAt = completedAt.AddDays(-5),
        });

        _context.UserInvestmentProfiles.Add(new UserInvestmentProfile
        {
            UserId = user.Id,
            InvestorCategory = InvestorCategory.Retail,
        });

        _context.UserKycs.Add(new UserKyc
        {
            UserId = user.Id,
            IdType = KycIdType.NationalIdCard,
            GovernmentIdDocumentPathOrKey = "gov-id.png",
            ProofOfAddressDocumentPathOrKey = "proof.png",
            SelfieVerificationPathOrKey = "selfie.png",
            GovernmentIdVerifiedAt = completedAt.AddDays(-2),
            ProofOfAddressVerifiedAt = completedAt.AddDays(-1),
            SelfieVerifiedAt = completedAt,
        });

        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/account");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<InvestorAccountResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.AccountType.Should().Be("Ordinary Investor");
        result.Value.AccountStatus.Should().Be("Active");
        result.Value.KycStatus.Should().Be("Completed");
        result.Value.KycCompletedDate.Should().BeCloseTo(completedAt, TimeSpan.FromSeconds(1));
        result.Value.InvestorClassification.Should().Be("Ordinary");
        result.Value.VerificationStatus.Should().Be("Verified");
        result.Value.ComplianceChecks.Should().Contain(c => c.Id == "aml" && c.Status == "Passed");
    }

    [Fact]
    public async Task GetAccount_WithActivatedOnboardingAndDocumentsOnly_ReturnsPendingKyc()
    {
        var user = SeedUser("account-docs-only@example.com");
        await _context.SaveChangesAsync();

        _context.UserOnboardings.Add(new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.Review,
            Status = OnboardingStatus.Activated,
        });

        _context.UserKycs.Add(new UserKyc
        {
            UserId = user.Id,
            IdType = KycIdType.NationalIdCard,
            GovernmentIdDocumentPathOrKey = "gov-id.png",
            ProofOfAddressDocumentPathOrKey = "proof.png",
            SelfieVerificationPathOrKey = "selfie.png",
        });

        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/account");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<InvestorAccountResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.AccountStatus.Should().Be("Active");
        result.Value.KycStatus.Should().Be("Pending");
        result.Value.KycCompletedDate.Should().BeNull();
    }

    [Fact]
    public async Task GetAccount_WithSophisticatedProfile_ReturnsClassification()
    {
        var user = SeedUser("account-sophisticated@example.com");
        await _context.SaveChangesAsync();

        _context.UserOnboardings.Add(new UserOnboarding
        {
            UserId = user.Id,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.Review,
            Status = OnboardingStatus.Submitted,
        });

        _context.UserInvestmentProfiles.Add(new UserInvestmentProfile
        {
            UserId = user.Id,
            InvestorCategory = InvestorCategory.Sophisticated,
        });

        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/account");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<InvestorAccountResponse>>(JsonOptions);
        result!.Value!.AccountType.Should().Be("Sophisticated Investor");
        result.Value.InvestorClassification.Should().Be("Sophisticated");
        result.Value.AccountStatus.Should().Be("Active");
        result.Value.KycStatus.Should().Be("Pending");
    }

    private User SeedUser(string email)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            FirstName = "Jane",
            LastName = "Okonkwo",
            PhoneNumber = "+2348012345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street",
            HasAgreedToTerms = true,
        };
        user.Created("TestUser");
        _context.Users.Add(user);
        return user;
    }

    private HttpClient CreateAuthorizedClient(int userId, string email)
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
                new Claim(ClaimTypes.Email, email),
            }),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
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
