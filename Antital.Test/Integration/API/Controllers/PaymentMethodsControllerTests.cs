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
public class PaymentMethodsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
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

    public PaymentMethodsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task GetPaymentMethods_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/investors/me/payment-methods");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPaymentMethods_NewInvestor_ReturnsEmptyList()
    {
        var user = SeedUser("pm-empty@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/payment-methods");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<PaymentMethodsResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AddPaymentMethod_FirstMethod_BecomesDefault()
    {
        var user = SeedUser("pm-add@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PostAsJsonAsync(
            "/api/investors/me/payment-methods",
            new AddPaymentMethodRequest("Bank", "GTBank Savings Account", "Guaranty Trust Bank", "5678"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<PaymentMethodResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Item.Type.Should().Be("Bank");
        result.Value.Item.Title.Should().Be("GTBank Savings Account");
        result.Value.Item.Subtitle.Should().Be("Guaranty Trust Bank • ********5678");
        result.Value.Item.IsDefault.Should().BeTrue();
        result.Value.Item.IsVerified.Should().BeTrue();
    }

    [Fact]
    public async Task AddPaymentMethod_InvalidLast4_Returns400()
    {
        var user = SeedUser("pm-invalid@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PostAsJsonAsync(
            "/api/investors/me/payment-methods",
            new AddPaymentMethodRequest("Card", "Visa Debit Card", "Visa", "12"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetDefaultPaymentMethod_UpdatesDefault()
    {
        var user = SeedUser("pm-default@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var first = await authClient.PostAsJsonAsync(
            "/api/investors/me/payment-methods",
            new AddPaymentMethodRequest("Bank", "GTBank Savings Account", "Guaranty Trust Bank", "5678"));
        var firstResult = await first.Content.ReadFromJsonAsync<Result<PaymentMethodResponse>>(JsonOptions);

        var second = await authClient.PostAsJsonAsync(
            "/api/investors/me/payment-methods",
            new AddPaymentMethodRequest("Card", "Visa Debit Card", "Visa", "4532"));
        var secondResult = await second.Content.ReadFromJsonAsync<Result<PaymentMethodResponse>>(JsonOptions);

        var response = await authClient.PatchAsync(
            $"/api/investors/me/payment-methods/{secondResult!.Value!.Item.Id}/default",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var setDefaultResult = await response.Content.ReadFromJsonAsync<Result<PaymentMethodResponse>>(JsonOptions);
        setDefaultResult!.Value!.Item.IsDefault.Should().BeTrue();

        var listResponse = await authClient.GetAsync("/api/investors/me/payment-methods");
        var listResult = await listResponse.Content.ReadFromJsonAsync<Result<PaymentMethodsResponse>>(JsonOptions);
        listResult!.Value!.Items.Should().HaveCount(2);
        listResult.Value.Items.Single(i => i.Id == firstResult!.Value!.Item.Id).IsDefault.Should().BeFalse();
        listResult.Value.Items.Single(i => i.Id == secondResult.Value!.Item.Id).IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePaymentMethod_RemovesItem()
    {
        var user = SeedUser("pm-delete@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var created = await authClient.PostAsJsonAsync(
            "/api/investors/me/payment-methods",
            new AddPaymentMethodRequest("Bank", "GTBank Savings Account", "Guaranty Trust Bank", "5678"));
        var createdResult = await created.Content.ReadFromJsonAsync<Result<PaymentMethodResponse>>(JsonOptions);

        var deleteResponse = await authClient.DeleteAsync(
            $"/api/investors/me/payment-methods/{createdResult!.Value!.Item.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listResponse = await authClient.GetAsync("/api/investors/me/payment-methods");
        var listResult = await listResponse.Content.ReadFromJsonAsync<Result<PaymentMethodsResponse>>(JsonOptions);
        listResult!.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task DeletePaymentMethod_OtherUsersMethod_Returns404()
    {
        var owner = SeedUser("pm-owner@example.com");
        var other = SeedUser("pm-other@example.com");
        await _context.SaveChangesAsync();

        using var ownerClient = CreateAuthorizedClient(owner.Id, owner.Email);
        var created = await ownerClient.PostAsJsonAsync(
            "/api/investors/me/payment-methods",
            new AddPaymentMethodRequest("Bank", "GTBank Savings Account", "Guaranty Trust Bank", "5678"));
        var createdResult = await created.Content.ReadFromJsonAsync<Result<PaymentMethodResponse>>(JsonOptions);

        using var otherClient = CreateAuthorizedClient(other.Id, other.Email);
        var deleteResponse = await otherClient.DeleteAsync(
            $"/api/investors/me/payment-methods/{createdResult!.Value!.Item.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
        _context.InvestorPaymentMethods.RemoveRange(_context.InvestorPaymentMethods);
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
