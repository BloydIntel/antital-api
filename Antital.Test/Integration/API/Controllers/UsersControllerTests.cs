using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Antital.Application.DTOs;
using Antital.Application.Features.Users.CreateUser;
using Antital.Application.Features.Users.UpdateUser;
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
public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
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

    public UsersControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task GetAll_ReturnsUsers()
    {
        var user = SeedUser(email: "getall@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient();

        var response = await authClient.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<List<UserDto>>>(JsonOptions);
        result.Should().NotBeNull();
        result!.IsSuccess.Should().BeTrue();
        result.Value!.Any(u => u.Email == user.Email).Should().BeTrue();
    }

    [Fact]
    public async Task GetById_ReturnsUser()
    {
        var user = SeedUser(email: "getbyid@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient();

        var response = await authClient.GetAsync($"/api/users/{user.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<UserDto>>(JsonOptions);
        result!.Value!.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task Create_CreatesUser()
    {
        var command = new CreateUserCommand(
            Email: "created@example.com",
            Password: "Password123!",
            FirstName: "Created",
            LastName: "User",
            PreferredName: null,
            PhoneNumber: "+123",
            UserType: UserTypeEnum.IndividualInvestor);

        using var authClient = CreateAuthorizedClient();

        var response = await authClient.PostAsJsonAsync("/api/users", command);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<UserDto>>(JsonOptions);
        result!.Value!.Email.Should().Be(command.Email);

        var created = await _context.Users.AsNoTracking().FirstAsync(u => u.Email == command.Email);
        created.FirstName.Should().Be("Created");
    }

    [Fact]
    public async Task Update_UpdatesUser()
    {
        var user = SeedUser(email: "update@example.com", firstName: "Old");
        await _context.SaveChangesAsync();

        var command = new UpdateUserCommand(
            Id: user.Id,
            FirstName: "New",
            LastName: "Name",
            PreferredName: "Pref",
            PhoneNumber: "+321",
            UserType: UserTypeEnum.IndividualInvestor,
            IsEmailVerified: true,
            Password: null);

        using var authClient = CreateAuthorizedClient();

        var response = await authClient.PutAsJsonAsync($"/api/users/{user.Id}", command);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await _context.Users.AsNoTracking().FirstAsync(u => u.Id == user.Id);
        updated.FirstName.Should().Be("New");
        updated.IsEmailVerified.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_AdminClaim_DeletesUser()
    {
        var user = SeedUser(email: "delete@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(includeDeletePermission: true);

        var response = await authClient.DeleteAsync($"/api/users/{user.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleted = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        deleted.Should().NotBeNull();
        deleted!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_WithoutAdminClaim_ReturnsForbidden()
    {
        var user = SeedUser(email: "forbid@example.com");
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(); // token without CanDelete claim

        var response = await authClient.DeleteAsync($"/api/users/{user.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private User SeedUser(string email, string firstName = "Test")
    {
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true,
            FirstName = firstName,
            LastName = "User",
            PhoneNumber = "+2348012345678",
            DateOfBirth = new DateTime(1990, 1, 1),
            Nationality = "Nigerian",
            CountryOfResidence = "Nigeria",
            StateOfResidence = "Lagos",
            ResidentialAddress = "123 Main Street",
            HasAgreedToTerms = true
        };
        user.Created("System");
        _context.Users.Add(user);
        return user;
    }

    private HttpClient CreateAuthorizedClient(bool includeDeletePermission = false)
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
                new Claim(ClaimTypes.Email, "user@example.com")
            }),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
        };

        if (includeDeletePermission)
            descriptor.Subject.AddClaim(new Claim("Permissions", "CanDelete"));

        var token = tokenHandler.CreateToken(descriptor);
        var jwt = tokenHandler.WriteToken(token);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
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
