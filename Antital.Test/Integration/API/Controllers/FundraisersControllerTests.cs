using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Antital.Application.DTOs.Fundraisers;
using Antital.Application.DTOs.Investments;
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
public class FundraisersControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
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

    public FundraisersControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task GetDashboard_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/fundraisers/me/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboard_InvestorUser_Returns403()
    {
        var user = SeedUser("investor-fundraiser-dash@example.com", UserTypeEnum.IndividualInvestor);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/fundraisers/me/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetDashboard_InvalidPeriod_ReturnsValidationError()
    {
        var user = SeedUser("fundraiser-period@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/fundraisers/me/dashboard?period=active");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<FundraiserDashboardResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainKey("period");
    }

    [Fact]
    public async Task GetDashboard_NoOwnedOffering_ReturnsEmptyZeros()
    {
        var user = SeedUser("fundraiser-empty@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/fundraisers/me/dashboard?period=this-month");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<FundraiserDashboardResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.OfferingId.Should().BeNull();
        result.Value.Summary.TotalAmountRaised.Should().Be(0m);
        result.Value.Summary.TotalInvestors.Should().Be(0);
        result.Value.FundingProgress.RaisedAmount.Should().Be(0m);
        result.Value.InvestorBreakdown.Buckets.Should().HaveCount(4);
        result.Value.Milestones.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboard_OwnedOfferingWithHoldings_ReturnsAggregates()
    {
        var fundraiser = SeedUser("fundraiser-owned@example.com", UserTypeEnum.FundRaiser);
        var investor = SeedUser("holding-investor@example.com", UserTypeEnum.IndividualInvestor);
        await _context.SaveChangesAsync();

        var offering = await SeedOwnedOfferingAsync(fundraiser.Id, "fundraiser-campaign");
        await SeedHoldingsAsync(offering.Id, investor.Id);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(fundraiser.Id, fundraiser.Email);
        var response = await authClient.GetAsync("/api/fundraisers/me/dashboard?period=this-month");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<FundraiserDashboardResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.OfferingId.Should().Be(offering.Id);
        result.Value.OfferingSlug.Should().Be("fundraiser-campaign");
        result.Value.Summary.TotalAmountRaised.Should().Be(142_500_000m);
        result.Value.Summary.TotalInvestors.Should().Be(2);
        result.Value.Summary.AverageInvestmentSize.Should().Be(71_250_000m);
        result.Value.Summary.DaysRemaining.Should().BeGreaterThan(0);
        result.Value.FundingProgress.TargetAmount.Should().Be(200_000_000m);
        result.Value.FundingProgress.MinimumThreshold.Should().Be(100_000_000m);
        result.Value.FundingProgress.CurrentVelocity.Should().Be(12_500_000m);
        result.Value.FundingProgress.VelocityPeriod.Should().Be("week");
        result.Value.InvestorBreakdown.Dimension.Should().Be("size");
        result.Value.InvestorBreakdown.Buckets.Sum(b => b.Percentage).Should().Be(100);
        result.Value.Milestones.Should().HaveCount(5);
        result.Value.Milestones.First(m => m.Key == "launch").Status.Should().Be("completed");
        result.Value.Milestones.First(m => m.Key == "funded_50").Status.Should().Be("completed");
    }

    [Fact]
    public async Task ListCampaignUpdates_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/fundraisers/me/campaign/updates");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateAndListCampaignUpdates_DraftThenPublish_Works()
    {
        var fundraiser = SeedUser("fundraiser-updates@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();
        await SeedOwnedOfferingAsync(fundraiser.Id, "updates-campaign");

        using var authClient = CreateAuthorizedClient(fundraiser.Id, fundraiser.Email);

        var createResponse = await authClient.PostAsJsonAsync(
            "/api/fundraisers/me/campaign/updates",
            new CreateFundraiserCampaignUpdateRequest("Draft title", "Draft body", Publish: false));
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var created = await createResponse.Content.ReadFromJsonAsync<Result<FundraiserCampaignUpdateDto>>(JsonOptions);
        created!.IsSuccess.Should().BeTrue();
        created.Value!.Status.Should().Be("draft");
        created.Value.PublishedAt.Should().BeNull();

        var listDrafts = await authClient.GetAsync("/api/fundraisers/me/campaign/updates?status=draft");
        var drafts = await listDrafts.Content.ReadFromJsonAsync<Result<FundraiserCampaignUpdatesResponse>>(JsonOptions);
        drafts!.Value!.Items.Should().ContainSingle(i => i.Id == created.Value.Id);

        var publishResponse = await authClient.PatchAsJsonAsync(
            $"/api/fundraisers/me/campaign/updates/{created.Value.Id}",
            new UpdateFundraiserCampaignUpdateRequest(null, null, Publish: true));
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var published = await publishResponse.Content.ReadFromJsonAsync<Result<FundraiserCampaignUpdateDto>>(JsonOptions);
        published!.Value!.Status.Should().Be("published");
        published.Value.PublishedAt.Should().NotBeNull();

        var publicUpdates = await _client.GetAsync("/api/investments/updates-campaign/updates");
        publicUpdates.StatusCode.Should().Be(HttpStatusCode.OK);
        var publicResult = await publicUpdates.Content.ReadFromJsonAsync<Result<OfferingUpdatesResponse>>(JsonOptions);
        publicResult!.Value!.Items.Should().ContainSingle(i => i.Title == "Draft title");
    }

    [Fact]
    public async Task CreateCampaignUpdate_NoOwnedOffering_Returns404()
    {
        var fundraiser = SeedUser("fundraiser-no-offer@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(fundraiser.Id, fundraiser.Email);
        var response = await authClient.PostAsJsonAsync(
            "/api/fundraisers/me/campaign/updates",
            new CreateFundraiserCampaignUpdateRequest("Title", "Body", Publish: true));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCampaign_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/fundraisers/me/campaign");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCampaign_NoOwnedOffering_ReturnsNullFields()
    {
        var fundraiser = SeedUser("fundraiser-campaign-empty@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(fundraiser.Id, fundraiser.Email);
        var response = await authClient.GetAsync("/api/fundraisers/me/campaign");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<FundraiserCampaignResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.OfferingId.Should().BeNull();
        result.Value.OfferingSlug.Should().BeNull();
        result.Value.PublicPath.Should().BeNull();
    }

    [Fact]
    public async Task GetCampaign_OwnedOffering_ReturnsContext()
    {
        var fundraiser = SeedUser("fundraiser-campaign-ctx@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();
        var offering = await SeedOwnedOfferingAsync(fundraiser.Id, "campaign-context");

        using var authClient = CreateAuthorizedClient(fundraiser.Id, fundraiser.Email);
        var response = await authClient.GetAsync("/api/fundraisers/me/campaign");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<FundraiserCampaignResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.OfferingId.Should().Be(offering.Id);
        result.Value.OfferingSlug.Should().Be("campaign-context");
        result.Value.OfferingName.Should().Be("Fundraiser Campaign");
        result.Value.Status.Should().Be("published");
        result.Value.PublicPath.Should().Be("/explore/campaign-context");
    }

    private User SeedUser(string email, UserTypeEnum userType)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            UserType = userType,
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
        _context.Users.Add(user);
        return user;
    }

    private async Task<InvestmentOffering> SeedOwnedOfferingAsync(int ownerUserId, string slug)
    {
        var offering = new InvestmentOffering
        {
            OwnerUserId = ownerUserId,
            Slug = slug,
            Name = "Fundraiser Campaign",
            Category = "Energy",
            Tagline = "Solar innovation",
            CoverImageUrl = "/investments/ayka_solar.jpg",
            RiskLevel = OfferingRiskLevel.Low,
            Status = OfferingStatus.Published,
            PublishedAt = DateTime.UtcNow.AddDays(-45),
            Funding = new OfferingFunding
            {
                RaisedAmount = 142_500_000m,
                FundingGoal = 200_000_000m,
                InvestorCount = 2,
                SharePrice = 100m,
                MinInvestment = 1_000_000m,
                MaxInvestment = 100_000_000m,
            },
            DealTerms = new DealTerms
            {
                TotalSharesOffered = 10_000,
                PricePerShare = 100m,
                MinimumInvestment = 1_000_000m,
                MaximumInvestment = 100_000_000m,
                MinimumThreshold = 100_000_000m,
                FundingGoal = 200_000_000m,
                Deadline = DateTime.UtcNow.AddDays(18),
            },
        };
        offering.Created("TestUser");
        offering.Funding.Created("TestUser");
        offering.DealTerms.Created("TestUser");
        _context.InvestmentOfferings.Add(offering);
        await _context.SaveChangesAsync();
        return offering;
    }

    private async Task SeedHoldingsAsync(int offeringId, int investorUserId)
    {
        var recent = new InvestorHolding
        {
            UserId = investorUserId,
            OfferingId = offeringId,
            InvestedAmount = 12_500_000m,
            CurrentValue = 12_500_000m,
            Returns = 0m,
            UnitHolding = 100,
            InvestedAt = DateTime.UtcNow.AddDays(-2),
        };
        recent.Created("TestUser");

        var older = new InvestorHolding
        {
            UserId = investorUserId,
            OfferingId = offeringId,
            InvestedAmount = 130_000_000m,
            CurrentValue = 130_000_000m,
            Returns = 0m,
            UnitHolding = 1000,
            InvestedAt = DateTime.UtcNow.AddDays(-20),
        };
        older.Created("TestUser");

        _context.InvestorHoldings.AddRange(recent, older);
        await _context.SaveChangesAsync();
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
            Subject = new ClaimsIdentity(
            [
                new Claim("UserId", userId.ToString()),
                new Claim(ClaimTypes.Email, email),
            ]),
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
        _context.OfferingUpdates.RemoveRange(_context.OfferingUpdates);
        _context.InvestorHoldings.RemoveRange(_context.InvestorHoldings);
        _context.InvestmentOfferings.RemoveRange(_context.InvestmentOfferings.IgnoreQueryFilters());
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
