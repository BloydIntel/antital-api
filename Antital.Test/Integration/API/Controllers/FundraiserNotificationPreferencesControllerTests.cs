using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Antital.Application.DTOs.Fundraisers;
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
public class FundraiserNotificationPreferencesControllerTests
    : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
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

    public FundraiserNotificationPreferencesControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task GetNotifications_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/fundraisers/me/settings/notifications");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNotifications_NoRow_ReturnsDefaults()
    {
        var user = SeedUser("fundraiser-notif-defaults@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/fundraisers/me/settings/notifications");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<FundraiserNotificationPreferencesResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Email.CampaignUpdates.Should().BeTrue();
        result.Value.Email.Muted.Should().BeFalse();
        result.Value.InApp.RealTimeActivity.Should().BeTrue();
        result.Value.Marketing.Partner.Should().BeFalse();
        result.Value.Marketing.Muted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateNotifications_CreatesAndPersists()
    {
        var user = SeedUser("fundraiser-notif-update@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        var payload = new UpdateFundraiserNotificationPreferencesRequest(
            Email: new FundraiserEmailNotificationPrefsDto(false, true, true, true),
            InApp: new FundraiserInAppNotificationPrefsDto(true, false, true, false),
            Marketing: new FundraiserMarketingNotificationPrefsDto(false, false, true, false));

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PutAsJsonAsync("/api/fundraisers/me/settings/notifications", payload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<FundraiserNotificationPreferencesResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Email.CampaignUpdates.Should().BeFalse();
        result.Value.Email.Muted.Should().BeTrue();
        result.Value.InApp.ChatMessages.Should().BeFalse();
        result.Value.Marketing.Partner.Should().BeTrue();

        var getResponse = await authClient.GetAsync("/api/fundraisers/me/settings/notifications");
        var getResult = await getResponse.Content.ReadFromJsonAsync<Result<FundraiserNotificationPreferencesResponse>>(JsonOptions);
        getResult!.Value!.Email.CampaignUpdates.Should().BeFalse();
        getResult.Value.Marketing.Partner.Should().BeTrue();

        var stored = await _context.FundraiserNotificationPreferences
            .AsNoTracking()
            .SingleAsync(p => p.UserId == user.Id);
        stored.EmailCampaignUpdates.Should().BeFalse();
        stored.EmailMuted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateNotifications_Investor_Returns403()
    {
        var user = SeedUser("investor-notif@example.com", UserTypeEnum.IndividualInvestor);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PutAsJsonAsync(
            "/api/fundraisers/me/settings/notifications",
            new UpdateFundraiserNotificationPreferencesRequest(
                Email: new FundraiserEmailNotificationPrefsDto(true, true, true, false),
                InApp: new FundraiserInAppNotificationPrefsDto(true, true, true, false),
                Marketing: new FundraiserMarketingNotificationPrefsDto(true, true, false, false)));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
        _context.FundraiserNotificationPreferences.RemoveRange(_context.FundraiserNotificationPreferences);
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
