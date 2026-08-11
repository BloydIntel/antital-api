using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Antital.Infrastructure.Seed;

public static class SuperAdminUserSeed
{
    private const string SeedActor = "system";

    public static async Task SeedAsync(
        AntitalDBContext context,
        IPasswordHasher passwordHasher,
        string email,
        string password,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var accountExists = await context.Users.AnyAsync(
            user => user.Email.ToLower() == normalizedEmail && !user.IsDeleted,
            cancellationToken);

        if (accountExists)
        {
            logger.LogInformation(
                "Super-admin seed skipped because an active account already exists for {Email}.",
                normalizedEmail);
            return;
        }

        var admin = new User
        {
            Email = normalizedEmail,
            PasswordHash = passwordHasher.HashPassword(password),
            UserType = UserTypeEnum.IndividualInvestor,
            Role = UserRoleEnum.Admin,
            IsEmailVerified = true,
            FirstName = "Super",
            LastName = "Admin",
            PreferredName = "Admin",
            PhoneNumber = string.Empty,
            DateOfBirth = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Nationality = string.Empty,
            CountryOfResidence = string.Empty,
            StateOfResidence = string.Empty,
            ResidentialAddress = string.Empty,
            HasAgreedToTerms = true,
        };
        admin.Created(SeedActor);

        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded verified super-admin account for {Email}.", normalizedEmail);
    }
}
