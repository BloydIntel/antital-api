using Antital.Domain.Enums;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Antital.Infrastructure.Seed;

public static class InvestorDashboardSeed
{
    private const string SeedUser = "system";

    public static async Task SeedAsync(
        AntitalDBContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await context.InvestorWallets.AnyAsync(cancellationToken))
        {
            return;
        }

        var offerings = await context.InvestmentOfferings
            .Include(o => o.Funding)
            .Where(o => o.Status == OfferingStatus.Published && !o.IsDeleted)
            .OrderBy(o => o.Id)
            .Take(4)
            .ToListAsync(cancellationToken);

        if (offerings.Count == 0)
        {
            return;
        }

        var demoUser = await context.Users
            .Where(u => u.IsEmailVerified && u.UserType == UserTypeEnum.IndividualInvestor && !u.IsDeleted)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (demoUser == null)
        {
            logger.LogInformation("Investor dashboard seed skipped: no verified individual investor user found.");
            return;
        }

        var now = DateTime.UtcNow;
        var wallet = new InvestorWallet
        {
            UserId = demoUser.Id,
            AvailableBalance = 5_325_400m,
            Currency = "NGN",
        };
        wallet.Created(SeedUser);
        context.InvestorWallets.Add(wallet);

        var holdings = new List<InvestorHolding>
        {
            CreateHolding(demoUser.Id, offerings[0], 25_400_000m, 1_250m, 432_650m, 1234, now.AddDays(-14)),
            CreateHolding(demoUser.Id, offerings[1], 25_400_000m, 959m, 50_567m, 1245, now.AddDays(-16)),
        };
        context.InvestorHoldings.AddRange(holdings);

        foreach (var (offering, changePercent, addedDaysAgo) in new[]
        {
            (offerings[0], 4.22m, 2),
            (offerings[1], 4.13m, 3),
            (offerings.Count > 2 ? offerings[2] : offerings[0], 6.20m, 4),
            (offerings.Count > 3 ? offerings[3] : offerings[1], -3.00m, 5),
        })
        {
            var watchlistItem = new InvestorWatchlistItem
            {
                UserId = demoUser.Id,
                OfferingId = offering.Id,
                ChangePercent = changePercent,
                AddedAt = now.AddDays(-addedDaysAgo),
            };
            watchlistItem.Created(SeedUser);
            context.InvestorWatchlistItems.Add(watchlistItem);
        }

        var performanceMonths = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var monthDate = now.AddMonths(-6 + offset);
                var point = new InvestorPortfolioPerformancePoint
                {
                    UserId = demoUser.Id,
                    Year = monthDate.Year,
                    Month = monthDate.Month,
                    Value = 10m + offset * 8m,
                };
                point.Created(SeedUser);
                return point;
            })
            .ToList();

        context.InvestorPortfolioPerformancePoints.AddRange(performanceMonths);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded investor dashboard data for user {UserId} ({Email}).",
            demoUser.Id,
            demoUser.Email);
    }

    private static InvestorHolding CreateHolding(
        int userId,
        InvestmentOffering offering,
        decimal invested,
        decimal currentValue,
        decimal returns,
        int units,
        DateTime investedAt)
    {
        var holding = new InvestorHolding
        {
            UserId = userId,
            OfferingId = offering.Id,
            InvestedAmount = invested,
            CurrentValue = currentValue,
            Returns = returns,
            UnitHolding = units,
            InvestedAt = investedAt,
        };
        holding.Created(SeedUser);
        return holding;
    }
}
