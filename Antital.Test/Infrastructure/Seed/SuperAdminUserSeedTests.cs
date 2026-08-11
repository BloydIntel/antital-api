using Antital.Domain.Enums;
using Antital.Domain.Models;
using Antital.Infrastructure;
using Antital.Infrastructure.Seed;
using Antital.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Antital.Test.Infrastructure.Seed;

public class SuperAdminUserSeedTests : IDisposable
{
    private const string Password = "Password@1";
    private readonly AntitalDBContext _dbContext;
    private readonly PasswordHasher _passwordHasher = new();

    public SuperAdminUserSeedTests()
    {
        var options = new DbContextOptionsBuilder<AntitalDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AntitalDBContext(options);
    }

    [Fact]
    public async Task SeedAsync_MissingAccount_CreatesVerifiedAdminWithHashedPassword()
    {
        await SeedAsync("  ADMIN@Antital.com  ");

        var admin = await _dbContext.Users.SingleAsync();
        admin.Email.Should().Be("admin@antital.com");
        admin.Role.Should().Be(UserRoleEnum.Admin);
        admin.UserType.Should().Be(UserTypeEnum.IndividualInvestor);
        admin.IsEmailVerified.Should().BeTrue();
        admin.PasswordHash.Should().NotBe(Password);
        _passwordHasher.VerifyPassword(Password, admin.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_RepeatedCall_CreatesOnlyOneAccount()
    {
        await SeedAsync("admin@antital.com");
        var originalPasswordHash = (await _dbContext.Users.SingleAsync()).PasswordHash;

        await SeedAsync("ADMIN@ANTITAL.COM");

        var admins = await _dbContext.Users.ToListAsync();
        admins.Should().ContainSingle();
        admins[0].PasswordHash.Should().Be(originalPasswordHash);
    }

    [Fact]
    public async Task SeedAsync_ExistingAccount_PreservesAccountCredentialsAndRole()
    {
        var existingUser = new User
        {
            Email = "admin@antital.com",
            PasswordHash = "existing-password-hash",
            UserType = UserTypeEnum.CorporateInvestor,
            Role = UserRoleEnum.User,
            IsEmailVerified = false,
            FirstName = "Existing",
            LastName = "User",
            PhoneNumber = string.Empty,
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Nationality = string.Empty,
            CountryOfResidence = string.Empty,
            StateOfResidence = string.Empty,
            ResidentialAddress = string.Empty,
            HasAgreedToTerms = true,
        };
        existingUser.Created("test");
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        await SeedAsync(" ADMIN@ANTITAL.COM ");

        var preservedUser = await _dbContext.Users.SingleAsync();
        preservedUser.PasswordHash.Should().Be("existing-password-hash");
        preservedUser.Role.Should().Be(UserRoleEnum.User);
        preservedUser.UserType.Should().Be(UserTypeEnum.CorporateInvestor);
        preservedUser.IsEmailVerified.Should().BeFalse();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private Task SeedAsync(string email)
    {
        return SuperAdminUserSeed.SeedAsync(
            _dbContext,
            _passwordHasher,
            email,
            Password,
            NullLogger.Instance,
            CancellationToken.None);
    }
}
