using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Antital.Application.DTOs.Investments;
using Antital.Application.DTOs.Investors;
using Antital.Domain.Enums;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Test.Helpers;
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
public class InvestmentPaymentControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
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

    public InvestmentPaymentControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task InitializePayment_ValidOrder_ReturnsPaystackSession()
    {
        var (user, offering) = await SeedEligibleInvestorWithOfferingAsync();
        var orderId = await CreateOrderAsync(user, offering.Id);

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PostAsJsonAsync(
            $"/api/investments/orders/{orderId}/pay",
            new InitializeInvestmentPaymentRequest("card"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<InitializeInvestmentPaymentResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.AuthorizationUrl.Should().StartWith("https://checkout.paystack.com/");
        result.Value.AccessCode.Should().NotBeNullOrWhiteSpace();
        result.Value.Reference.Should().StartWith($"ANT-ORD-{orderId}-");
        result.Value.PublicKey.Should().Be(_config["Paystack:PublicKey"]);

        var order = await _context.InvestmentOrders.SingleAsync(o => o.Id == orderId);
        order.PaystackReference.Should().Be(result.Value.Reference);
        order.PaymentChannel.Should().Be(PaymentChannel.Card);
    }

    [Fact]
    public async Task InitializePayment_InvalidChannel_Returns400()
    {
        var (user, offering) = await SeedEligibleInvestorWithOfferingAsync();
        var orderId = await CreateOrderAsync(user, offering.Id);

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PostAsJsonAsync(
            $"/api/investments/orders/{orderId}/pay",
            new InitializeInvestmentPaymentRequest("bitcoin"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PaystackWebhook_ValidSignature_MarksOrderPaid()
    {
        var (user, offering) = await SeedEligibleInvestorWithOfferingAsync();
        var orderId = await CreateOrderAsync(user, offering.Id);

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var payResponse = await authClient.PostAsJsonAsync(
            $"/api/investments/orders/{orderId}/pay",
            new InitializeInvestmentPaymentRequest("card"));
        payResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var payResult = await payResponse.Content.ReadFromJsonAsync<Result<InitializeInvestmentPaymentResponse>>(JsonOptions);
        var reference = payResult!.Value!.Reference;
        var payload = PaystackTestHelper.BuildChargeSuccessPayload(reference, 102_500);
        var signature = PaystackTestHelper.ComputeSignature(payload, PaystackTestHelper.TestSecretKey);

        using var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments/paystack/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        webhookRequest.Headers.Add("x-paystack-signature", signature);

        var webhookResponse = await _client.SendAsync(webhookRequest);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        _context.ChangeTracker.Clear();

        var order = await _context.InvestmentOrders.AsNoTracking().SingleAsync(o => o.Id == orderId);
        order.Status.Should().Be(InvestmentOrderStatus.Paid);
        order.PaidAt.Should().NotBeNull();
        order.InvestorHoldingId.Should().NotBeNull();

        var transaction = await _context.PaymentTransactions.AsNoTracking().SingleAsync();
        transaction.Reference.Should().Be(reference);
        transaction.Status.Should().Be(PaymentTransactionStatus.Success);

        var holding = await _context.InvestorHoldings.AsNoTracking().SingleAsync();
        holding.UserId.Should().Be(user.Id);
        holding.OfferingId.Should().Be(offering.Id);
        holding.InvestedAmount.Should().Be(1000m);
        holding.UnitHolding.Should().Be(10);

        var funding = await _context.OfferingFundings.AsNoTracking().SingleAsync(f => f.OfferingId == offering.Id);
        funding.RaisedAmount.Should().Be(101_000m);
        funding.InvestorCount.Should().Be(26);
    }

    [Fact]
    public async Task GetOrder_AfterSuccessfulWebhook_ReturnsPaidOrderWithHolding()
    {
        var (user, offering) = await SeedEligibleInvestorWithOfferingAsync();
        var orderId = await CreateOrderAsync(user, offering.Id);

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var payResponse = await authClient.PostAsJsonAsync(
            $"/api/investments/orders/{orderId}/pay",
            new InitializeInvestmentPaymentRequest("card"));
        var payResult = await payResponse.Content.ReadFromJsonAsync<Result<InitializeInvestmentPaymentResponse>>(JsonOptions);
        var reference = payResult!.Value!.Reference;

        var payload = PaystackTestHelper.BuildChargeSuccessPayload(reference, 102_500);
        var signature = PaystackTestHelper.ComputeSignature(payload, PaystackTestHelper.TestSecretKey);
        using var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments/paystack/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        webhookRequest.Headers.Add("x-paystack-signature", signature);
        (await _client.SendAsync(webhookRequest)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await authClient.GetAsync($"/api/investments/orders/{orderId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<GetInvestmentOrderResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.OrderId.Should().Be(orderId);
        result.Value.Status.Should().Be(nameof(InvestmentOrderStatus.Paid));
        result.Value.InvestorHoldingId.Should().NotBeNull();
        result.Value.TotalAmount.Should().Be(1025m);
    }

    [Fact]
    public async Task PaystackWebhook_AfterPayment_AddsHoldingToDashboard()
    {
        var (user, offering) = await SeedEligibleInvestorWithOfferingAsync();
        var orderId = await CreateOrderAsync(user, offering.Id);

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var payResponse = await authClient.PostAsJsonAsync(
            $"/api/investments/orders/{orderId}/pay",
            new InitializeInvestmentPaymentRequest("card"));
        var payResult = await payResponse.Content.ReadFromJsonAsync<Result<InitializeInvestmentPaymentResponse>>(JsonOptions);
        var reference = payResult!.Value!.Reference;

        var payload = PaystackTestHelper.BuildChargeSuccessPayload(reference, 102_500);
        var signature = PaystackTestHelper.ComputeSignature(payload, PaystackTestHelper.TestSecretKey);
        using var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments/paystack/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        webhookRequest.Headers.Add("x-paystack-signature", signature);
        (await _client.SendAsync(webhookRequest)).StatusCode.Should().Be(HttpStatusCode.OK);

        var dashboardResponse = await authClient.GetAsync("/api/investors/me/dashboard?period=this-month");
        dashboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<Result<InvestorDashboardResponse>>(JsonOptions);
        dashboard!.IsSuccess.Should().BeTrue();
        dashboard.Value!.Holdings.Should().ContainSingle(h =>
            h.Slug == offering.Slug
            && h.Invested == 1000m
            && h.UnitHolding == 10
            && h.RaisedAmount == 101_000m);
        dashboard.Value.Summary.TotalInvested.Should().Be(1000m);
    }

    [Fact]
    public async Task PaystackWebhook_DuplicateDelivery_IsIdempotent()
    {
        var (user, offering) = await SeedEligibleInvestorWithOfferingAsync();
        var orderId = await CreateOrderAsync(user, offering.Id);

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var payResponse = await authClient.PostAsJsonAsync(
            $"/api/investments/orders/{orderId}/pay",
            new InitializeInvestmentPaymentRequest("card"));
        var payResult = await payResponse.Content.ReadFromJsonAsync<Result<InitializeInvestmentPaymentResponse>>(JsonOptions);
        var reference = payResult!.Value!.Reference;

        var payload = PaystackTestHelper.BuildChargeSuccessPayload(reference, 102_500);
        var signature = PaystackTestHelper.ComputeSignature(payload, PaystackTestHelper.TestSecretKey);

        async Task<HttpResponseMessage> SendWebhookAsync()
        {
            using var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments/paystack/webhook")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            webhookRequest.Headers.Add("x-paystack-signature", signature);
            return await _client.SendAsync(webhookRequest);
        }

        (await SendWebhookAsync()).StatusCode.Should().Be(HttpStatusCode.OK);
        (await SendWebhookAsync()).StatusCode.Should().Be(HttpStatusCode.OK);

        _context.ChangeTracker.Clear();

        (await _context.PaymentTransactions.AsNoTracking().CountAsync()).Should().Be(1);
        (await _context.InvestorHoldings.AsNoTracking().CountAsync()).Should().Be(1);

        var funding = await _context.OfferingFundings.AsNoTracking().SingleAsync(f => f.OfferingId == offering.Id);
        funding.RaisedAmount.Should().Be(101_000m);
        funding.InvestorCount.Should().Be(26);
    }

    [Fact]
    public async Task GetOrder_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/investments/orders/1");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InitializePayment_WithoutAuth_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/investments/orders/1/pay",
            new InitializeInvestmentPaymentRequest("card"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PaystackWebhook_InvalidSignature_Returns400()
    {
        var payload = PaystackTestHelper.BuildChargeSuccessPayload("ANT-ORD-99-abc", 102_500);

        using var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments/paystack/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        webhookRequest.Headers.Add("x-paystack-signature", "invalid-signature");

        var webhookResponse = await _client.SendAsync(webhookRequest);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PaystackWebhook_MissingEvent_Returns200()
    {
        const string payload = """{"data":{"reference":"ANT-ORD-99-abc","amount":102500}}""";
        var signature = PaystackTestHelper.ComputeSignature(payload, PaystackTestHelper.TestSecretKey);

        using var webhookRequest = new HttpRequestMessage(HttpMethod.Post, "/api/payments/paystack/webhook")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        webhookRequest.Headers.Add("x-paystack-signature", signature);

        var webhookResponse = await _client.SendAsync(webhookRequest);
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<int> CreateOrderAsync(User user, int offeringId)
    {
        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PostAsJsonAsync(
            $"/api/investments/{offeringId}/orders",
            new CreateInvestmentOrderRequest(10));
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<CreateInvestmentOrderResponse>>(JsonOptions);
        return result!.Value!.OrderId;
    }

    private async Task<(User User, InvestmentOffering Offering)> SeedEligibleInvestorWithOfferingAsync()
    {
        var user = SeedUser("payment-investor@example.com");
        await _context.SaveChangesAsync();
        SeedSubmittedOnboarding(user.Id);
        var offering = await SeedOfferingAsync();
        await _context.SaveChangesAsync();
        return (user, offering);
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

    private async Task<InvestmentOffering> SeedOfferingAsync()
    {
        var offering = new InvestmentOffering
        {
            Slug = "payment-co",
            Name = "Payment Co",
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
                SharePrice = 100m,
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
