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
public class WatchlistControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
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

    public WatchlistControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task GetWatchlist_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/investors/me/watchlist");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWatchlist_NewInvestor_ReturnsEmptyList()
    {
        var user = SeedUser("watchlist-empty@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/watchlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<WatchlistResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.Counts.All.Should().Be(0);
        result.Value.Counts.EndingSoon.Should().Be(0);
        result.Value.Counts.NearTarget.Should().Be(0);
    }

    [Fact]
    public async Task GetWatchlist_WithSeededItem_ReturnsRichItem()
    {
        var user = SeedUser("watchlist-list@example.com");
        var offering = await SeedOfferingAsync("greentech-solutions", deadlineDays: 15, raised: 990_000m, goal: 1_100_000m);
        await SeedWatchlistItemAsync(user.Id, offering, 4.22m);
        await SeedOfferingUpdateAsync(offering.Id, "Production facility expansion fully cleared");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/watchlist");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<WatchlistResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(i =>
            i.Slug == "greentech-solutions"
            && i.Sector == "Energy"
            && i.Risk == "Low"
            && i.DaysLeft == 15
            && i.FundingProgressPercent == 90
            && i.ChangePercent == 4.22m
            && i.RecentUpdate == "Production facility expansion fully cleared"
            && i.RemindersCount == 0);
        result.Value.Counts.All.Should().Be(1);
        result.Value.Counts.NearTarget.Should().Be(1);
    }

    [Fact]
    public async Task AddToWatchlist_AddsItem()
    {
        var user = SeedUser("watchlist-add@example.com");
        var offering = await SeedOfferingAsync("fintech-innovators");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PostAsJsonAsync(
            "/api/investors/me/watchlist",
            new AddToWatchlistRequest(offering.Id));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<WatchlistItemDto>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Slug.Should().Be("fintech-innovators");

        var list = await authClient.GetAsync("/api/investors/me/watchlist");
        var listResult = await list.Content.ReadFromJsonAsync<Result<WatchlistResponse>>(JsonOptions);
        listResult!.Value!.Items.Should().ContainSingle(i => i.OfferingId == offering.Id);
    }

    [Fact]
    public async Task AddToWatchlist_Duplicate_Returns409()
    {
        var user = SeedUser("watchlist-dup@example.com");
        var offering = await SeedOfferingAsync("aquapure-innovations");
        await SeedWatchlistItemAsync(user.Id, offering, 0m);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PostAsJsonAsync(
            "/api/investors/me/watchlist",
            new AddToWatchlistRequest(offering.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddToWatchlist_UnknownOffering_Returns404()
    {
        var user = SeedUser("watchlist-missing@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PostAsJsonAsync(
            "/api/investors/me/watchlist",
            new AddToWatchlistRequest(99999));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveFromWatchlist_RemovesItem()
    {
        var user = SeedUser("watchlist-remove@example.com");
        var offering = await SeedOfferingAsync("ecobuild-materials");
        await SeedWatchlistItemAsync(user.Id, offering, 0m);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.DeleteAsync($"/api/investors/me/watchlist/{offering.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await authClient.GetAsync("/api/investors/me/watchlist");
        var listResult = await list.Content.ReadFromJsonAsync<Result<WatchlistResponse>>(JsonOptions);
        listResult!.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveFromWatchlist_NotFound_Returns404()
    {
        var user = SeedUser("watchlist-remove-missing@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.DeleteAsync("/api/investors/me/watchlist/12345");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWatchlistStatus_ReturnsExpectedFlag()
    {
        var user = SeedUser("watchlist-status@example.com");
        var offering = await SeedOfferingAsync("solars-innovations");
        await SeedWatchlistItemAsync(user.Id, offering, 0m);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);

        var watchlisted = await authClient.GetAsync($"/api/investors/me/watchlist/status?offeringId={offering.Id}");
        watchlisted.StatusCode.Should().Be(HttpStatusCode.OK);
        var watchlistedResult = await watchlisted.Content.ReadFromJsonAsync<Result<WatchlistStatusResponse>>(JsonOptions);
        watchlistedResult!.Value!.IsWatchlisted.Should().BeTrue();

        var notWatchlisted = await authClient.GetAsync("/api/investors/me/watchlist/status?offeringId=99999");
        notWatchlisted.StatusCode.Should().Be(HttpStatusCode.OK);
        var notWatchlistedResult = await notWatchlisted.Content.ReadFromJsonAsync<Result<WatchlistStatusResponse>>(JsonOptions);
        notWatchlistedResult!.Value!.IsWatchlisted.Should().BeFalse();
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
        _context.Users.Add(user);
        return user;
    }

    private async Task<InvestmentOffering> SeedOfferingAsync(
        string slug,
        int deadlineDays = 30,
        decimal raised = 450_000m,
        decimal goal = 1_100_000m)
    {
        var offering = new InvestmentOffering
        {
            Slug = slug,
            Name = slug,
            Category = "Energy",
            Tagline = "Solar innovation",
            CoverImageUrl = "/investments/ayka_solar.jpg",
            RiskLevel = OfferingRiskLevel.Low,
            Status = OfferingStatus.Published,
            PublishedAt = DateTime.UtcNow,
            Funding = new OfferingFunding
            {
                RaisedAmount = raised,
                FundingGoal = goal,
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
                FundingGoal = goal,
                Deadline = DateTime.UtcNow.AddDays(deadlineDays),
            },
        };
        offering.Created("TestUser");
        offering.Funding.Created("TestUser");
        offering.DealTerms.Created("TestUser");
        _context.InvestmentOfferings.Add(offering);
        await _context.SaveChangesAsync();
        return offering;
    }

    private async Task SeedWatchlistItemAsync(int userId, InvestmentOffering offering, decimal changePercent)
    {
        var watchlist = new InvestorWatchlistItem
        {
            UserId = userId,
            OfferingId = offering.Id,
            ChangePercent = changePercent,
            AddedAt = DateTime.UtcNow.AddDays(-1),
        };
        watchlist.Created("TestUser");
        _context.InvestorWatchlistItems.Add(watchlist);
        await _context.SaveChangesAsync();
    }

    private async Task SeedOfferingUpdateAsync(int offeringId, string title)
    {
        var update = new OfferingUpdate
        {
            OfferingId = offeringId,
            PublishedAt = DateTime.UtcNow.AddHours(-4),
            Title = title,
            Body = "Details",
            LikeCount = 0,
        };
        update.Created("TestUser");
        _context.OfferingUpdates.Add(update);
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
        _context.OfferingUpdates.RemoveRange(_context.OfferingUpdates);
        _context.InvestorWatchlistItems.RemoveRange(_context.InvestorWatchlistItems);
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
