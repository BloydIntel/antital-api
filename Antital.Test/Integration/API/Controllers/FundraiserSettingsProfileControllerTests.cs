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
public class FundraiserSettingsProfileControllerTests
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

    public FundraiserSettingsProfileControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task GetSettingsProfile_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/fundraisers/me/settings/profile");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSettingsProfile_InvestorUser_Returns403()
    {
        var user = SeedUser("investor-settings-profile@example.com", UserTypeEnum.IndividualInvestor);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/fundraisers/me/settings/profile");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSettingsProfile_NoProfile_ReturnsEmptyDefaults()
    {
        var user = SeedUser("fundraiser-settings-empty@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/fundraisers/me/settings/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<FundraiserSettingsProfileResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.CompanyName.Should().BeNull();
        result.Value.CompletionPercentage.Should().Be(0);
        result.Value.Contact.IsWhatsAppConnected.Should().BeFalse();
        result.Value.Contact.HasPublicHelpDesk.Should().BeFalse();
    }

    [Fact]
    public async Task GetSettingsProfile_WithOnboardingData_MapsFields()
    {
        var user = SeedUser("fundraiser-settings-get@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();
        SeedFundraiserProfile(user.Id);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.GetAsync("/api/fundraisers/me/settings/profile");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<FundraiserSettingsProfileResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.CompanyName.Should().Be("Acme Fundraise Limited");
        result.Value.RegistrationNumber.Should().Be("RC-998877");
        result.Value.Bio.Should().Be("We raise capital for growth.");
        result.Value.Website.Should().Be("https://acme.example.com");
        result.Value.PublicEmail.Should().Be("hello@acme.example.com");
        result.Value.Headquarters.Should().Be("12 Admiralty Way, Victoria Island, Lagos, Nigeria");
        result.Value.LocationLabel.Should().Be("Lagos, Nigeria");
        result.Value.CompanyAvatarFallback.Should().Be("AF");
        result.Value.CompletionPercentage.Should().Be(100);
        result.Value.Contact.FullName.Should().Be("Ada Okonkwo");
        result.Value.Contact.EmailAddress.Should().Be("ada@acme.example.com");
        result.Value.Contact.PhoneNumber.Should().Be("+2348091112233");
    }

    [Fact]
    public async Task UpdateSettingsProfile_WithoutAuth_Returns401()
    {
        var response = await _client.PutAsJsonAsync(
            "/api/fundraisers/me/settings/profile",
            ValidUpdateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSettingsProfile_CreatesAndPersistsProfile()
    {
        var user = SeedUser("fundraiser-settings-create@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PutAsJsonAsync(
            "/api/fundraisers/me/settings/profile",
            ValidUpdateRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<FundraiserSettingsProfileResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.CompanyName.Should().Be("Skyhigh Technologies");
        result.Value.RegistrationNumber.Should().Be("RC-12345678");
        result.Value.Contact.FullName.Should().Be("John Doe");
        result.Value.Contact.EmailAddress.Should().Be("doe@skyhightech.com");

        var getResponse = await authClient.GetAsync("/api/fundraisers/me/settings/profile");
        var getResult = await getResponse.Content.ReadFromJsonAsync<Result<FundraiserSettingsProfileResponse>>(JsonOptions);
        getResult!.Value!.CompanyName.Should().Be("Skyhigh Technologies");
        getResult.Value.Bio.Should().Be("Advanced prosthetic solutions.");
        getResult.Value.Headquarters.Should().Be("123 Business Way, Victoria Island, Lagos, Nigeria");
    }

    [Fact]
    public async Task UpdateSettingsProfile_UpdatesExistingOnboardingFields()
    {
        var user = SeedUser("fundraiser-settings-update@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();
        SeedFundraiserProfile(user.Id);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PutAsJsonAsync(
            "/api/fundraisers/me/settings/profile",
            new UpdateFundraiserSettingsProfileRequest(
                CompanyName: "Updated Fundraise Co",
                RegistrationNumber: "RC-000111",
                Bio: "Updated bio for settings.",
                Website: "https://updated.example.com",
                PublicEmail: "ops@updated.example.com",
                Headquarters: "1 Broad Street, Lagos, Nigeria",
                Contact: new UpdateFundraiserSettingsContactRequest(
                    FullName: "Bola Ade",
                    EmailAddress: "bola@updated.example.com",
                    PhoneNumber: "+2348010001111")));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Result<FundraiserSettingsProfileResponse>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.CompanyName.Should().Be("Updated Fundraise Co");
        result.Value.LocationLabel.Should().Be("Lagos, Nigeria");
        result.Value.Contact.FullName.Should().Be("Bola Ade");

        var stored = await _context.UserInvestmentProfiles
            .AsNoTracking()
            .SingleAsync(p => p.UserId == user.Id);
        stored.CompanyLegalName.Should().Be("Updated Fundraise Co");
        stored.BusinessDescription.Should().Be("Updated bio for settings.");
        stored.RepresentativeEmail.Should().Be("bola@updated.example.com");
    }

    [Fact]
    public async Task UpdateSettingsProfile_WithInvalidPayload_Returns400()
    {
        var user = SeedUser("fundraiser-settings-invalid@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        var response = await authClient.PutAsJsonAsync(
            "/api/fundraisers/me/settings/profile",
            new UpdateFundraiserSettingsProfileRequest(
                CompanyName: "",
                RegistrationNumber: null,
                Bio: null,
                Website: null,
                PublicEmail: "not-an-email",
                Headquarters: null,
                Contact: null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static UpdateFundraiserSettingsProfileRequest ValidUpdateRequest() =>
        new(
            CompanyName: "Skyhigh Technologies",
            RegistrationNumber: "RC-12345678",
            Bio: "Advanced prosthetic solutions.",
            Website: "https://skyhightechnologies.com",
            PublicEmail: "contact@skyhightech.com",
            Headquarters: "123 Business Way, Victoria Island, Lagos, Nigeria",
            Contact: new UpdateFundraiserSettingsContactRequest(
                FullName: "John Doe",
                EmailAddress: "doe@skyhightech.com",
                PhoneNumber: "+2348012345678"));

    private void SeedFundraiserProfile(int userId)
    {
        var profile = new UserInvestmentProfile
        {
            UserId = userId,
            InvestorCategory = InvestorCategory.Retail,
            CompanyLegalName = "Acme Fundraise Limited",
            RegistrationNumber = "RC-998877",
            BusinessDescription = "We raise capital for growth.",
            CompanyWebsite = "https://acme.example.com",
            CompanyEmail = "hello@acme.example.com",
            BusinessAddress = "12 Admiralty Way, Victoria Island, Lagos, Nigeria",
            RepresentativeFullName = "Ada Okonkwo",
            RepresentativeEmail = "ada@acme.example.com",
            RepresentativePhoneNumber = "+2348091112233",
        };
        profile.Created("TestUser");
        _context.UserInvestmentProfiles.Add(profile);
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
        _context.UserInvestmentProfiles.RemoveRange(_context.UserInvestmentProfiles);
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
