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
public class WalletTransactionsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
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

    public WalletTransactionsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task GetWalletTransactions_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/investors/me/wallet/transactions");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWalletTransactions_NewInvestor_ReturnsEmptyList()
    {
        var user = SeedUser("wallet-empty@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/wallet/transactions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<WalletTransactionsResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetWalletTransactions_WithPaidOrder_ReturnsInvestmentRow()
    {
        var user = SeedUser("wallet-paid@example.com");
        var offering = await SeedOfferingAsync("aquapure-innovations");
        await SeedPaidOrderAsync(user.Id, offering);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/wallet/transactions?pageSize=3");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<WalletTransactionsResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle();
        var item = result.Value.Items[0];
        item.Type.Should().Be("Investment");
        item.Status.Should().Be("Completed");
        item.Amount.Should().Be(10_000m);
        item.Fees.Should().Be(250m);
        item.OfferingSlug.Should().Be("aquapure-innovations");
        item.SubDescription.Should().Contain("20 units");
        result.Value.PageSize.Should().Be(3);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetWalletTransactions_SecondaryMarketType_Returns400()
    {
        var user = SeedUser("wallet-type@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/wallet/transactions?type=Buy");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<WalletTransactionsResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().ContainKey("type");
    }

    [Fact]
    public async Task GetWalletTransactions_DepositType_ReturnsEmptyList()
    {
        var user = SeedUser("wallet-deposit@example.com");
        var offering = await SeedOfferingAsync("greentech-solutions");
        await SeedPaidOrderAsync(user.Id, offering);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/wallet/transactions?type=Deposit");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<WalletTransactionsResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetWalletTransaction_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/investors/me/wallet/transactions/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWalletTransaction_WithPaidOrder_ReturnsInvoice()
    {
        var user = SeedUser("wallet-detail@example.com");
        var offering = await SeedOfferingAsync("aquapure-innovations");
        var order = await SeedPaidOrderAsync(user.Id, offering);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync($"/api/investors/me/wallet/transactions/{order.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<WalletTransactionInvoiceResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.InvoiceId.Should().Be(order.Id);
        result.Value.PaymentMethod.Should().Be("Card");
        result.Value.PaymentReference.Should().Be(order.PaystackReference);
        result.Value.BillTo.Name.Should().Be("Jane Okonkwo");
        result.Value.BillTo.Email.Should().Be(user.Email);
        result.Value.TransactionDetails.Type.Should().Be("Investment");
        result.Value.TransactionDetails.Status.Should().Be("Completed");
        result.Value.Breakdown.Company.Should().Be("AquaPure Innovations");
        result.Value.Breakdown.Units.Should().Be(20);
        result.Value.Breakdown.TotalAmount.Should().Be(10_250m);
    }

    [Fact]
    public async Task GetWalletTransaction_OtherUsersOrder_Returns404()
    {
        var owner = SeedUser("wallet-owner@example.com");
        var other = SeedUser("wallet-other@example.com");
        var offering = await SeedOfferingAsync("greentech-solutions");
        var order = await SeedPaidOrderAsync(owner.Id, offering);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(other.Id, other.Email);
        var response = await authClient.GetAsync($"/api/investors/me/wallet/transactions/{order.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWalletTransaction_PendingOrder_Returns404()
    {
        var user = SeedUser("wallet-pending@example.com");
        var offering = await SeedOfferingAsync("greentech-solutions");
        var order = new InvestmentOrder
        {
            UserId = user.Id,
            OfferingId = offering.Id,
            Units = 10,
            SharePrice = 500m,
            Subtotal = 5_000m,
            PlatformFeePercent = 2.5m,
            PlatformFee = 125m,
            TotalAmount = 5_125m,
            Currency = "NGN",
            Status = InvestmentOrderStatus.PendingPayment,
        };
        order.Created("TestUser");
        _context.InvestmentOrders.Add(order);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync($"/api/investors/me/wallet/transactions/{order.Id}");

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

    private async Task<InvestmentOffering> SeedOfferingAsync(string slug)
    {
        var offering = new InvestmentOffering
        {
            Slug = slug,
            Name = "AquaPure Innovations",
            Category = "Health",
            Tagline = "Clean water",
            CoverImageUrl = "/investments/aquapure.jpg",
            RiskLevel = OfferingRiskLevel.Moderate,
            Status = OfferingStatus.Published,
            PublishedAt = DateTime.UtcNow,
            Funding = new OfferingFunding
            {
                RaisedAmount = 100_000m,
                FundingGoal = 500_000m,
                InvestorCount = 5,
                SharePrice = 500m,
                MinInvestment = 1000m,
                MaxInvestment = 50_000m,
            },
            DealTerms = new DealTerms
            {
                TotalSharesOffered = 1_000,
                PricePerShare = 500m,
                MinimumInvestment = 1000m,
                MaximumInvestment = 50_000m,
                MinimumThreshold = 50_000m,
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

    private async Task<InvestmentOrder> SeedPaidOrderAsync(int userId, InvestmentOffering offering)
    {
        var order = new InvestmentOrder
        {
            UserId = userId,
            OfferingId = offering.Id,
            Units = 20,
            SharePrice = 500m,
            Subtotal = 10_000m,
            PlatformFeePercent = 2.5m,
            PlatformFee = 250m,
            TotalAmount = 10_250m,
            Currency = "NGN",
            Status = InvestmentOrderStatus.Paid,
            PaymentChannel = PaymentChannel.Card,
            PaystackReference = $"ANT-ORD-test-{Guid.NewGuid():N}",
            PaidAt = DateTime.UtcNow.AddDays(-1),
        };
        order.Created("TestUser");
        _context.InvestmentOrders.Add(order);
        await _context.SaveChangesAsync();
        return order;
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
