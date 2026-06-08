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
public class InvestorsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
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

    public InvestorsControllerTests(CustomWebApplicationFactory<Program> factory)
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
        var response = await _client.GetAsync("/api/investors/me/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboard_InvalidPeriod_ReturnsValidationError()
    {
        var user = SeedUser("dashboard@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);

        var response = await authClient.GetAsync("/api/investors/me/dashboard?period=active");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<InvestorDashboardResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainKey("period");
    }

    [Fact]
    public async Task GetDashboard_AuthenticatedUser_ReturnsDashboardBundle()
    {
        var user = SeedUser("dashboard@example.com");
        var offering = await SeedOfferingAsync("greentech-solutions");
        await SeedDashboardDataAsync(user.Id, offering);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);

        var response = await authClient.GetAsync("/api/investors/me/dashboard?period=this-month");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<InvestorDashboardResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Summary.AvailableBalance.Should().Be(5_325_400m);
        result.Value.Summary.TotalInvested.Should().Be(25_400_000m);
        result.Value.Summary.Currency.Should().Be("NGN");
        result.Value.Holdings.Should().ContainSingle(h => h.Slug == "greentech-solutions");
        result.Value.ActiveDeals.Should().ContainSingle(d => d.Slug == "greentech-solutions");
        result.Value.PortfolioPerformance.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDashboard_NewInvestor_ReturnsEmptyArraysAndZeroSummary()
    {
        var user = SeedUser("empty@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);

        var response = await authClient.GetAsync("/api/investors/me/dashboard?period=this-month");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<InvestorDashboardResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Summary.AvailableBalance.Should().Be(0m);
        result.Value.Summary.TotalInvested.Should().Be(0m);
        result.Value.Holdings.Should().BeEmpty();
        result.Value.ActiveDeals.Should().BeEmpty();
        result.Value.PortfolioPerformance.Should().BeEmpty();
    }

    private User SeedUser(string email, string? preferredName = null)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            FirstName = "Jane",
            LastName = "Okonkwo",
            PreferredName = preferredName,
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

    private async Task<InvestmentOffering> SeedOfferingAsync(string slug)
    {
        var offering = new InvestmentOffering
        {
            Slug = slug,
            Name = "GreenTech Solutions",
            Category = "Energy",
            Tagline = "Solar innovation",
            CoverImageUrl = "/investments/ayka_solar.jpg",
            RiskLevel = OfferingRiskLevel.Low,
            Status = OfferingStatus.Published,
            PublishedAt = DateTime.UtcNow,
            Funding = new OfferingFunding
            {
                RaisedAmount = 450_000m,
                FundingGoal = 1_100_000m,
                InvestorCount = 25,
                SharePrice = 22_400m,
                MinInvestment = 1000m,
                MaxInvestment = 50_000m,
            },
            DealTerms = new DealTerms
            {
                TotalSharesOffered = 10_000,
                PricePerShare = 100m,
                MinimumInvestment = 1000m,
                MaximumInvestment = 50_000m,
                MinimumThreshold = 250_000m,
                FundingGoal = 1_100_000m,
                Deadline = DateTime.UtcNow.AddDays(30),
            },
        };
        offering.Created("TestUser");
        offering.Funding.Created("TestUser");
        offering.DealTerms.Created("TestUser");
        _context.InvestmentOfferings.Add(offering);
        await _context.SaveChangesAsync();
        return offering;
    }

    private async Task SeedDashboardDataAsync(int userId, InvestmentOffering offering)
    {
        var wallet = new InvestorWallet
        {
            UserId = userId,
            AvailableBalance = 5_325_400m,
            Currency = "NGN",
        };
        wallet.Created("TestUser");
        _context.InvestorWallets.Add(wallet);

        var holding = new InvestorHolding
        {
            UserId = userId,
            OfferingId = offering.Id,
            InvestedAmount = 25_400_000m,
            CurrentValue = 1_250m,
            Returns = 432_650m,
            UnitHolding = 1234,
            InvestedAt = DateTime.UtcNow.AddDays(-3),
        };
        holding.Created("TestUser");
        _context.InvestorHoldings.Add(holding);

        var watchlist = new InvestorWatchlistItem
        {
            UserId = userId,
            OfferingId = offering.Id,
            ChangePercent = 4.22m,
            AddedAt = DateTime.UtcNow.AddDays(-1),
        };
        watchlist.Created("TestUser");
        _context.InvestorWatchlistItems.Add(watchlist);

        var now = DateTime.UtcNow;
        var performance = new InvestorPortfolioPerformancePoint
        {
            UserId = userId,
            Year = now.Year,
            Month = now.Month,
            Value = 48m,
        };
        performance.Created("TestUser");
        _context.InvestorPortfolioPerformancePoints.Add(performance);
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
        _context.InvestorPortfolioPerformancePoints.RemoveRange(_context.InvestorPortfolioPerformancePoints);
        _context.InvestorWatchlistItems.RemoveRange(_context.InvestorWatchlistItems);
        _context.InvestorHoldings.RemoveRange(_context.InvestorHoldings);
        _context.InvestorWallets.RemoveRange(_context.InvestorWallets);
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
