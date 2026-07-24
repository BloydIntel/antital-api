using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Antital.Application.DTOs.Files;
using Antital.Domain.Enums;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Test.Integration;
using BuildingBlocks.Application.Features;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Antital.Test.Integration.API.Controllers;

[Collection("IntegrationTests")]
public class FilesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly IServiceScope _scope;
    private readonly AntitalDBContext _context;
    private readonly IConfiguration _config;
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public FilesControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<AntitalDBContext>();
        _config = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
        CleanupDatabase();
    }

    [Fact]
    public async Task Upload_WithoutAuth_Returns401()
    {
        using var content = BuildMultipart("doc.pdf", "application/pdf", "hello");
        var response = await _client.PostAsync("/api/files/upload", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upload_WithAuth_ReturnsCloudinaryMetadata()
    {
        var user = SeedUser("uploader@example.com", UserTypeEnum.FundRaiser);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        using var content = BuildMultipart("Offering.pdf", "application/pdf", "pdf-bytes");
        content.Add(new StringContent("antital/docs"), "folder");

        var response = await authClient.PostAsync("/api/files/upload", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<Result<FileUploadDto>>(JsonOptions);
        result!.IsSuccess.Should().BeTrue();
        result.Value!.OriginalFileName.Should().Be("Offering.pdf");
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.Url.Should().Contain("cloudinary.com");
        result.Value.PublicId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Upload_UnsupportedType_Returns400()
    {
        var user = SeedUser("bad-type@example.com", UserTypeEnum.IndividualInvestor);
        await _context.SaveChangesAsync();

        using var authClient = CreateAuthorizedClient(user.Id, user.Email);
        using var content = BuildMultipart("note.exe", "application/octet-stream", "malware");

        var response = await authClient.PostAsync("/api/files/upload", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static MultipartFormDataContent BuildMultipart(string fileName, string contentType, string body)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
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
