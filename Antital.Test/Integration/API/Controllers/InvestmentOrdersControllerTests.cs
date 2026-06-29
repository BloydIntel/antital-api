using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
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
public class InvestmentOrdersControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
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

    public InvestmentOrdersControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task CreateOrder_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/investments/1/orders", new CreateInvestmentOrderRequest(10));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_OnboardingIncomplete_Returns403()
    {
        var user = SeedUser("no-onboarding@example.com");
        await _context.SaveChangesAsync();
        var offering = await SeedOfferingAsync();
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);

        var response = await authClient.PostAsJsonAsync(
            $"/api/investments/{offering.Id}/orders",
            new CreateInvestmentOrderRequest(10));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateOrder_ValidRequest_ReturnsOrderBreakdown()
    {
        var user = SeedUser("investor@example.com");
        await _context.SaveChangesAsync();
        SeedSubmittedOnboarding(user.Id);
        var offering = await SeedOfferingAsync(sharePrice: 100m, minInvestment: 1000m, maxInvestment: 50_000m);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);

        var response = await authClient.PostAsJsonAsync(
            $"/api/investments/{offering.Id}/orders",
            new CreateInvestmentOrderRequest(10));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<CreateInvestmentOrderResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.OfferingId.Should().Be(offering.Id);
        result.Value.Units.Should().Be(10);
        result.Value.SharePrice.Should().Be(100m);
        result.Value.Subtotal.Should().Be(1000m);
        result.Value.PlatformFeePercent.Should().Be(2.5m);
        result.Value.PlatformFee.Should().Be(25m);
        result.Value.TotalAmount.Should().Be(1025m);
        result.Value.Status.Should().Be(nameof(InvestmentOrderStatus.PendingPayment));
        result.Value.MinInvestment.Should().Be(1000m);
        result.Value.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task CreateOrder_BelowMinimumInvestment_Returns400()
    {
        var user = SeedUser("min-check@example.com");
        await _context.SaveChangesAsync();
        SeedSubmittedOnboarding(user.Id);
        var offering = await SeedOfferingAsync(sharePrice: 100m, minInvestment: 1000m, maxInvestment: 50_000m);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);

        var response = await authClient.PostAsJsonAsync(
            $"/api/investments/{offering.Id}/orders",
            new CreateInvestmentOrderRequest(5));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_UnknownOffering_Returns404()
    {
        var user = SeedUser("unknown-offering@example.com");
        await _context.SaveChangesAsync();
        SeedSubmittedOnboarding(user.Id);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);

        var response = await authClient.PostAsJsonAsync(
            "/api/investments/99999/orders",
            new CreateInvestmentOrderRequest(10));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
            LastName = "Investor",
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

    private void SeedSubmittedOnboarding(int userId)
    {
        var onboarding = new UserOnboarding
        {
            UserId = userId,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.Kyc,
            Status = OnboardingStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
        };
        onboarding.Created("TestUser");
        _context.UserOnboardings.Add(onboarding);
    }

    private async Task<InvestmentOffering> SeedOfferingAsync(
        decimal sharePrice = 100m,
        decimal minInvestment = 1000m,
        decimal maxInvestment = 50_000m)
    {
        var offering = new InvestmentOffering
        {
            Slug = "checkout-co",
            Name = "Checkout Co",
            Category = "Tech",
            Tagline = "Tagline",
            CoverImageUrl = "/img.jpg",
            RiskLevel = OfferingRiskLevel.Low,
            Status = OfferingStatus.Published,
            PublishedAt = DateTime.UtcNow,
            Funding = new OfferingFunding
            {
                RaisedAmount = 100_000m,
                FundingGoal = 500_000m,
                InvestorCount = 25,
                SharePrice = sharePrice,
                MinInvestment = minInvestment,
                MaxInvestment = maxInvestment,
            },
            DealTerms = new DealTerms
            {
                TotalSharesOffered = 10_000,
                PricePerShare = sharePrice,
                MinimumInvestment = minInvestment,
                MaximumInvestment = maxInvestment,
                MinimumThreshold = 250_000m,
                FundingGoal = 500_000m,
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
        _context.PaymentTransactions.RemoveRange(_context.PaymentTransactions);
        _context.InvestmentOrders.RemoveRange(_context.InvestmentOrders);
        _context.InvestorHoldings.RemoveRange(_context.InvestorHoldings);
        _context.UserOnboardings.RemoveRange(_context.UserOnboardings);
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
