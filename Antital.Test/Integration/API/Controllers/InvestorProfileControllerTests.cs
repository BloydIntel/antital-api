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
public class InvestorProfileControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
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

    public InvestorProfileControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task GetProfile_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/investors/me/profile");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_ReturnsAuthenticatedUserProfile()
    {
        var user = SeedUser("profile-get@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/investors/me/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<InvestorProfileResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be("profile-get@example.com");
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().Be("Okonkwo");
        result.Value.PhoneNumber.Should().Be("+2348012345678");
        result.Value.ResidentialAddress.Should().Be("123 Main Street");
        result.Value.StateOfResidence.Should().Be("Lagos");
        result.Value.CountryOfResidence.Should().Be("Nigeria");
        result.Value.Nationality.Should().Be("Nigerian");
        result.Value.IsEmailVerified.Should().BeTrue();
        result.Value.UserType.Should().Be(UserTypeEnum.IndividualInvestor);
    }

    [Fact]
    public async Task UpdateProfile_WithoutAuth_Returns401()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/investors/me/profile",
            ValidUpdateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_UpdatesEditableFields()
    {
        var user = SeedUser("profile-update@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PutAsJsonAsync(
            "/api/investors/me/profile",
            new UpdateInvestorProfileRequest(
                FirstName: "Ada",
                LastName: "Okafor",
                PreferredName: "Ada",
                PhoneNumber: "+2348098765432",
                ResidentialAddress: "42 Admiralty Way, Lekki Phase 1",
                StateOfResidence: "Lagos State",
                CountryOfResidence: "Nigeria"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<InvestorProfileResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.FirstName.Should().Be("Ada");
        result.Value.LastName.Should().Be("Okafor");
        result.Value.PreferredName.Should().Be("Ada");
        result.Value.PhoneNumber.Should().Be("+2348098765432");
        result.Value.ResidentialAddress.Should().Be("42 Admiralty Way, Lekki Phase 1");
        result.Value.StateOfResidence.Should().Be("Lagos State");
        result.Value.Email.Should().Be("profile-update@example.com");
        result.Value.Nationality.Should().Be("Nigerian");

        var getResponse = await authClient.GetAsync("/api/investors/me/profile");
        var getResult = await getResponse.Content.ReadFromJsonAsync<Result<InvestorProfileResponse>>(JsonOptions);
        getResult!.Value!.FirstName.Should().Be("Ada");
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidPayload_Returns400()
    {
        var user = SeedUser("profile-invalid@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PutAsJsonAsync(
            "/api/investors/me/profile",
            new UpdateInvestorProfileRequest(
                FirstName: "",
                LastName: "Okafor",
                PreferredName: null,
                PhoneNumber: "+2348098765432",
                ResidentialAddress: "Short",
                StateOfResidence: "Lagos State",
                CountryOfResidence: "Nigeria"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static UpdateInvestorProfileRequest ValidUpdateRequest() =>
        new(
            FirstName: "Ada",
            LastName: "Okafor",
            PreferredName: null,
            PhoneNumber: "+2348098765432",
            ResidentialAddress: "42 Admiralty Way, Lekki Phase 1",
            StateOfResidence: "Lagos State",
            CountryOfResidence: "Nigeria");

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
