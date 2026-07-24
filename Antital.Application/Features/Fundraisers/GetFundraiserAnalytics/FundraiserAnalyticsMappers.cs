using System.ComponentModel;
using System.Reflection;
using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Application.Features.Fundraisers.GetFundraiserAnalytics;

internal static class FundraiserAnalyticsMappers
{
    private static readonly HashSet<string> WestAfrica = new(StringComparer.OrdinalIgnoreCase)
    {
        "Nigeria", "Ghana", "Senegal", "Ivory Coast", "Côte d'Ivoire", "Cote d'Ivoire",
        "Benin", "Togo", "Mali", "Niger", "Burkina Faso", "Liberia", "Sierra Leone",
        "Guinea", "Gambia", "Cape Verde", "NG", "GH",
    };

    private static readonly HashSet<string> Europe = new(StringComparer.OrdinalIgnoreCase)
    {
        "United Kingdom", "UK", "England", "France", "Germany", "Netherlands", "Spain",
        "Italy", "Ireland", "Belgium", "Portugal", "Sweden", "Switzerland", "GB", "DE", "FR",
    };

    private static readonly HashSet<string> Americas = new(StringComparer.OrdinalIgnoreCase)
    {
        "United States", "USA", "US", "Canada", "Brazil", "Mexico", "Argentina", "CA",
    };

    public static bool TryParsePeriod(string? period, out DateTime fromUtc, out DateTime toUtcExclusive, out string? error)
    {
        error = null;
        var today = DateTime.UtcNow.Date;
        toUtcExclusive = today.AddDays(1);

        if (string.IsNullOrWhiteSpace(period) || period.Equals("last-7-days", StringComparison.OrdinalIgnoreCase))
        {
            fromUtc = today.AddDays(-6);
            return true;
        }

        if (period.Equals("last-14-days", StringComparison.OrdinalIgnoreCase))
        {
            fromUtc = today.AddDays(-13);
            return true;
        }

        if (period.Equals("last-30-days", StringComparison.OrdinalIgnoreCase))
        {
            fromUtc = today.AddDays(-29);
            return true;
        }

        fromUtc = today;
        error = "Period must be last-7-days, last-14-days, or last-30-days.";
        return false;
    }

    public static FundraiserAnalyticsResponse Empty() =>
        new(
            null,
            null,
            new FundraiserAnalyticsOverviewDto(0, 0, 0),
            new FundraiserAnalyticsTrafficDto(0, "views", []),
            new FundraiserAnalyticsDiversityDto(null, [], []),
            new FundraiserAnalyticsConversionDto(0m, null, 0m));

    public static FundraiserAnalyticsResponse ToResponse(
        InvestmentOffering offering,
        IReadOnlyList<OfferingEngagementDaily> engagement,
        DateTime fromUtc,
        DateTime toUtcExclusive,
        int campaignLikes,
        IReadOnlyList<InvestorHolding> holdings,
        IReadOnlyDictionary<int, UserInvestmentProfile> profiles,
        IReadOnlyList<InvestmentOrder> paidOrders)
    {
        var byDate = engagement
            .GroupBy(e => e.Date.Date)
            .ToDictionary(g => g.Key, g => g.First());

        var points = new List<FundraiserAnalyticsTrafficPointDto>();
        for (var day = fromUtc.Date; day < toUtcExclusive; day = day.AddDays(1))
        {
            byDate.TryGetValue(day, out var row);
            var views = row?.PageViews ?? 0;
            points.Add(new FundraiserAnalyticsTrafficPointDto(
                day,
                day.ToString("ddd", System.Globalization.CultureInfo.InvariantCulture),
                views));
        }

        var totalViews = points.Sum(p => p.Value);
        var totalShares = engagement.Sum(e => e.Shares);
        var totalUnique = engagement.Sum(e => e.UniqueVisitors);
        var dayCount = points.Count == 0 ? 1 : points.Count;
        var averagePerDay = Math.Round((double)totalViews / dayCount, 1, MidpointRounding.AwayFromZero);

        var investors = holdings
            .GroupBy(h => h.UserId)
            .Select(g => g.First())
            .ToList();
        var investorCount = investors.Count;

        var viewToInvest = totalViews <= 0
            ? 0m
            : decimal.Round((decimal)investorCount / totalViews, 4, MidpointRounding.AwayFromZero);

        var returnVisitorRate = totalViews <= 0
            ? 0m
            : decimal.Round(
                Math.Clamp((decimal)(totalViews - totalUnique) / totalViews, 0m, 1m),
                4,
                MidpointRounding.AwayFromZero);

        double? avgHours = null;
        var durations = paidOrders
            .Where(o => o.PaidAt.HasValue)
            .Select(o => (o.PaidAt!.Value - o.CreatedAt).TotalHours)
            .Where(h => h >= 0)
            .ToList();
        if (durations.Count > 0)
        {
            avgHours = Math.Round(durations.Average(), 2, MidpointRounding.AwayFromZero);
        }

        return new FundraiserAnalyticsResponse(
            offering.Id,
            offering.Slug,
            new FundraiserAnalyticsOverviewDto(totalViews, campaignLikes, totalShares),
            new FundraiserAnalyticsTrafficDto(averagePerDay, "views", points),
            BuildDiversity(investors, profiles),
            new FundraiserAnalyticsConversionDto(viewToInvest, avgHours, returnVisitorRate));
    }

    private static FundraiserAnalyticsDiversityDto BuildDiversity(
        IReadOnlyList<InvestorHolding> uniqueHolders,
        IReadOnlyDictionary<int, UserInvestmentProfile> profiles)
    {
        if (uniqueHolders.Count == 0)
        {
            return new FundraiserAnalyticsDiversityDto(null, [], []);
        }

        var geoCounts = uniqueHolders
            .GroupBy(h => ResolveRegion(h.User?.CountryOfResidence))
            .ToDictionary(g => g.Key, g => g.Count());

        var geoOrder = new[] { "West Africa", "Europe", "Americas", "Other" };
        var geographic = ToPercentageBuckets(geoOrder.Select(label => (label, geoCounts.GetValueOrDefault(label))));

        var categoryCounts = uniqueHolders
            .GroupBy(h =>
            {
                if (!profiles.TryGetValue(h.UserId, out var profile))
                {
                    return "Uncategorized";
                }

                return GetEnumDescription(profile.InvestorCategory);
            })
            .ToDictionary(g => g.Key, g => g.Count());

        var categories = ToPercentageBuckets(
            categoryCounts.OrderByDescending(kv => kv.Value).Select(kv => (kv.Key, kv.Value)));

        var topLocation = uniqueHolders
            .Select(h => FormatLocation(h.User?.StateOfResidence, h.User?.CountryOfResidence))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .GroupBy(s => s!)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .FirstOrDefault();

        return new FundraiserAnalyticsDiversityDto(topLocation, geographic, categories);
    }

    private static IReadOnlyList<FundraiserAnalyticsBucketDto> ToPercentageBuckets(
        IEnumerable<(string Label, int Count)> items)
    {
        var list = items.Where(i => i.Count > 0).ToList();
        var total = list.Sum(i => i.Count);
        if (total == 0)
        {
            return [];
        }

        var buckets = list
            .Select(i => new FundraiserAnalyticsBucketDto(
                i.Label,
                (int)Math.Round(100.0 * i.Count / total, MidpointRounding.AwayFromZero)))
            .ToList();

        // Fix rounding so percentages sum to 100 when possible.
        var drift = 100 - buckets.Sum(b => b.Percentage);
        if (drift != 0 && buckets.Count > 0)
        {
            var first = buckets[0];
            buckets[0] = first with { Percentage = first.Percentage + drift };
        }

        return buckets;
    }

    private static string ResolveRegion(string? country)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            return "Other";
        }

        var trimmed = country.Trim();
        if (WestAfrica.Contains(trimmed) || trimmed.Contains("Nigeria", StringComparison.OrdinalIgnoreCase))
        {
            return "West Africa";
        }

        if (Europe.Contains(trimmed))
        {
            return "Europe";
        }

        if (Americas.Contains(trimmed))
        {
            return "Americas";
        }

        return "Other";
    }

    private static string? FormatLocation(string? state, string? country)
    {
        var statePart = string.IsNullOrWhiteSpace(state) ? null : state.Trim();
        var countryPart = string.IsNullOrWhiteSpace(country) ? null : AbbreviateCountry(country.Trim());
        if (statePart == null && countryPart == null)
        {
            return null;
        }

        if (statePart != null && countryPart != null)
        {
            return $"{statePart}, {countryPart}";
        }

        return statePart ?? countryPart;
    }

    private static string AbbreviateCountry(string country) =>
        country.Equals("Nigeria", StringComparison.OrdinalIgnoreCase) ? "NG"
        : country.Equals("United Kingdom", StringComparison.OrdinalIgnoreCase) || country.Equals("UK", StringComparison.OrdinalIgnoreCase) ? "GB"
        : country.Equals("United States", StringComparison.OrdinalIgnoreCase) || country.Equals("USA", StringComparison.OrdinalIgnoreCase) ? "US"
        : country.Length <= 3 ? country.ToUpperInvariant()
        : country;

    private static string GetEnumDescription(InvestorCategory category)
    {
        var field = category.GetType().GetField(category.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? category.ToString();
    }
}
