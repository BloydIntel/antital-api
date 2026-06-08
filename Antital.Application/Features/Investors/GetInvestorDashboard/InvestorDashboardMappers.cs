using Antital.Application.DTOs.Investors;
using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Application.Features.Investors.GetInvestorDashboard;

internal static class InvestorDashboardMappers
{
    public static InvestorDashboardActiveDealDto ToActiveDeal(InvestorWatchlistItem item)
    {
        var offering = item.Offering;
        var price = offering.Funding?.SharePrice ?? 0m;

        return new InvestorDashboardActiveDealDto(
            offering.Id,
            offering.Slug,
            offering.Name,
            offering.CoverImageUrl,
            price,
            item.ChangePercent);
    }

    public static InvestorDashboardHoldingDto ToHolding(InvestorHolding holding)
    {
        var offering = holding.Offering;

        return new InvestorDashboardHoldingDto(
            offering.Id,
            offering.Slug,
            offering.Name,
            offering.Category,
            ToRiskString(offering.RiskLevel),
            holding.InvestedAmount,
            holding.CurrentValue,
            holding.Returns,
            holding.UnitHolding,
            holding.InvestedAt);
    }

    private static string ToRiskString(OfferingRiskLevel risk) =>
        risk switch
        {
            OfferingRiskLevel.Low => "low",
            OfferingRiskLevel.Moderate => "moderate",
            OfferingRiskLevel.High => "high",
            _ => risk.ToString().ToLowerInvariant(),
        };
}
