using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Antital.Domain.Enums;
using Antital.Domain.Models;
using Antital.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Antital.Test.Integration.API.Controllers;

[Collection("IntegrationTests")]
public sealed class AdminFlagsAndAlertsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly AntitalDBContext _context;
    private readonly IConfiguration _configuration;

    public AdminFlagsAndAlertsControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _configuration = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        Cleanup();
    }

    [Fact]
    public async Task List_WithoutAuthentication_Returns401()
    {
        (await _client.GetAsync("/api/admin/flags-and-alerts")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_NonAdmin_Returns403()
    {
        var user = AddUser("alerts-investor@example.com", UserRoleEnum.User);
        await _context.SaveChangesAsync();
        using var client = AuthorizedClient(user.Id, user.Email, UserRoleEnum.User);
        (await client.GetAsync("/api/admin/flags-and-alerts")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_AdminReturnsData_AndInvalidPaginationReturns400()
    {
        var user = AddUser("alerts-admin@example.com", UserRoleEnum.Admin);
        _context.PlatformAlerts.Add(new PlatformAlert { PublicId = "FLG-TEST-1", Type = "AML/Fraud", Severity = AlertSeverity.Critical, Status = AlertStatus.Open, EntityAffected = "INV-1", Description = "Test alert", OccurredAtUtc = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        using var client = AuthorizedClient(user.Id, user.Email, UserRoleEnum.Admin);

        var response = await client.GetAsync("/api/admin/flags-and-alerts?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("FLG-TEST-1");
        (await client.GetAsync("/api/admin/flags-and-alerts?page=0&pageSize=10")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DetailAndUpdate_UseExpectedNotFoundAndValidationResponses()
    {
        var user = AddUser("alerts-admin-detail@example.com", UserRoleEnum.Admin);
        _context.PlatformAlerts.Add(new PlatformAlert { PublicId = "FLG-TEST-2", Type = "KYC", Severity = AlertSeverity.High, Status = AlertStatus.Open, EntityAffected = "INV-2", Description = "Test detail", OccurredAtUtc = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        using var client = AuthorizedClient(user.Id, user.Email, UserRoleEnum.Admin);

        (await client.GetAsync("/api/admin/flags-and-alerts/missing")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PatchAsJsonAsync("/api/admin/flags-and-alerts/missing", new { status = "Resolved" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.PatchAsJsonAsync("/api/admin/flags-and-alerts/FLG-TEST-2", new { status = "NotAStatus" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var update = await client.PatchAsJsonAsync("/api/admin/flags-and-alerts/FLG-TEST-2", new { status = "Acknowledged", resolutionNote = "Reviewed" });
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        (await _context.PlatformAlerts.AsNoTracking().SingleAsync(x => x.PublicId == "FLG-TEST-2")).Status.Should().Be(AlertStatus.Acknowledged);
    }

    private User AddUser(string email, UserRoleEnum role)
    {
        var user = new User { Email = email, PasswordHash = "test", Role = role, UserType = UserTypeEnum.IndividualInvestor, IsEmailVerified = true, FirstName = "Test", LastName = "Admin", PhoneNumber = "+2348000000000", DateOfBirth = new DateTime(1990, 1, 1), Nationality = "Nigerian", CountryOfResidence = "Nigeria", StateOfResidence = "Lagos", ResidentialAddress = "Test address", HasAgreedToTerms = true };
        user.Created("test");
        _context.Users.Add(user);
        return user;
    }

    private HttpClient AuthorizedClient(int userId, string email, UserRoleEnum role)
    {
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
        var token = handler.CreateToken(new SecurityTokenDescriptor { Issuer = _configuration["Jwt:Issuer"], Audience = _configuration["Jwt:Audience"], Expires = DateTime.UtcNow.AddHours(1), Subject = new ClaimsIdentity([new Claim("UserId", userId.ToString()), new Claim(ClaimTypes.Email, email), new Claim(ClaimTypes.Role, role.ToString())]), SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256) });
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", handler.WriteToken(token));
        return client;
    }

    private void Cleanup()
    {
        _context.PlatformAlerts.RemoveRange(_context.PlatformAlerts);
        _context.Users.RemoveRange(_context.Users);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        Cleanup();
        _scope.Dispose();
        _client.Dispose();
    }
}
