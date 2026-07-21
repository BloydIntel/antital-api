using Antital.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Antital.Infrastructure.Seed;

/// <summary>
/// Assigns seeded / unowned published offerings to fundraiser users for local dashboard testing.
/// Idempotent: only updates offerings where <c>OwnerUserId</c> is null.
/// </summary>
public static class FundraiserOfferingOwnershipSeed
{
    private const string PreferredFundraiserEmail = "johneseyin@gmail.com";
    private const string PreferredOfferingSlug = "greentech-solutions";

    public static async Task SeedAsync(
        AntitalDBContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var unownedOfferings = await context.InvestmentOfferings
            .Where(o => !o.IsDeleted && o.OwnerUserId == null)
            .OrderBy(o => o.Id)
            .ToListAsync(cancellationToken);

        if (unownedOfferings.Count == 0)
        {
            return;
        }

        var fundraisers = await context.Users
            .Where(u => u.UserType == UserTypeEnum.FundRaiser && !u.IsDeleted)
            .OrderBy(u => u.Id)
            .ToListAsync(cancellationToken);

        if (fundraisers.Count == 0)
        {
            logger.LogInformation(
                "Fundraiser offering ownership seed skipped: no fundraiser users found.");
            return;
        }

        var preferredOwner = fundraisers.FirstOrDefault(u =>
            string.Equals(u.Email, PreferredFundraiserEmail, StringComparison.OrdinalIgnoreCase))
            ?? fundraisers[0];

        var preferredOffering = unownedOfferings.FirstOrDefault(o => o.Slug == PreferredOfferingSlug)
            ?? unownedOfferings[0];

        preferredOffering.OwnerUserId = preferredOwner.Id;

        // Round-robin remaining unowned offerings across fundraisers (dev convenience).
        var otherOfferings = unownedOfferings.Where(o => o.Id != preferredOffering.Id).ToList();
        for (var i = 0; i < otherOfferings.Count; i++)
        {
            otherOfferings[i].OwnerUserId = fundraisers[i % fundraisers.Count].Id;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Assigned OwnerUserId on {Count} investment offerings (primary: {Slug} → {Email}).",
            unownedOfferings.Count,
            preferredOffering.Slug,
            preferredOwner.Email);
    }
}
