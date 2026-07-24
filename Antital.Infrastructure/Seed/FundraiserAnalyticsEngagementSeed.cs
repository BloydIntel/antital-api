using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Antital.Infrastructure.Seed;

/// <summary>
/// Seeds ~7 days of engagement counters on owned offerings for local analytics demos.
/// Idempotent: skips an offering that already has engagement rows.
/// </summary>
public static class FundraiserAnalyticsEngagementSeed
{
    private const string SeedActor = "Seed";

    public static async Task SeedAsync(
        AntitalDBContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var ownedOfferings = await context.InvestmentOfferings
            .Where(o => !o.IsDeleted && o.OwnerUserId != null)
            .OrderBy(o => o.Id)
            .ToListAsync(cancellationToken);

        if (ownedOfferings.Count == 0)
        {
            return;
        }

        var seededDays = 0;
        var today = DateTime.UtcNow.Date;
        var pattern = new[] { 2300, 4400, 3500, 1000, 9600, 3500, 5100 };

        foreach (var offering in ownedOfferings)
        {
            var hasRows = await context.OfferingEngagementDailies
                .AnyAsync(e => e.OfferingId == offering.Id && !e.IsDeleted, cancellationToken);
            if (hasRows)
            {
                continue;
            }

            for (var i = 0; i < 7; i++)
            {
                var date = today.AddDays(-6 + i);
                var views = pattern[i % pattern.Length];
                var unique = (int)Math.Round(views * 0.72, MidpointRounding.AwayFromZero);
                var shares = Math.Max(1, views / 25);
                var row = new OfferingEngagementDaily
                {
                    OfferingId = offering.Id,
                    Date = date,
                    PageViews = views,
                    UniqueVisitors = unique,
                    Shares = shares,
                };
                row.Created(SeedActor);
                context.OfferingEngagementDailies.Add(row);
                seededDays++;
            }
        }

        if (seededDays == 0)
        {
            return;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} offering engagement daily rows.", seededDays);
    }
}
