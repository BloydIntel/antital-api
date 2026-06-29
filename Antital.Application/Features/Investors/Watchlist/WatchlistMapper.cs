using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investments;
using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Application.Features.Investors.Watchlist;

internal static class WatchlistMapper
{
    public static WatchlistItemDto ToItem(InvestorWatchlistItem item, OfferingUpdate? latestUpdate)
    {
        var offering = item.Offering;
        var funding = offering.Funding;
        var raised = funding?.RaisedAmount ?? 0m;
        var goal = funding?.FundingGoal ?? 0m;
        var daysLeft = InvestmentMappers.ComputeDaysLeft(offering.DealTerms?.Deadline);
        var fundingProgressPercent = InvestmentMappers.ComputeFundingProgressPercent(raised, goal);

        return new WatchlistItemDto(
            offering.Id,
            offering.Slug,
            offering.Name,
            offering.Category,
            ToRiskLabel(offering.RiskLevel),
            daysLeft,
            fundingProgressPercent,
            item.ChangePercent,
            item.AddedAt,
            latestUpdate?.Title,
            latestUpdate?.PublishedAt,
            RemindersCount: 0);
    }

    public static WatchlistCountsDto ToCounts(IReadOnlyList<WatchlistItemDto> items) =>
        new(
            items.Count,
            items.Count(i => i.DaysLeft is < 3),
            items.Count(i => i.FundingProgressPercent > 80));

    private static string ToRiskLabel(OfferingRiskLevel risk) =>
        risk switch
        {
            OfferingRiskLevel.Low => "Low",
            OfferingRiskLevel.Moderate => "Moderate",
            OfferingRiskLevel.High => "High",
            _ => risk.ToString(),
        };
}
