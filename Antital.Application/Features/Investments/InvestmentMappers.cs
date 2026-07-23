using Antital.Application.DTOs.Investments;
using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Application.Features.Investments;

internal static class InvestmentMappers
{
    public static InvestmentListItemDto ToListItem(InvestmentOffering offering)
    {
        var funding = offering.Funding!;
        return new InvestmentListItemDto(
            offering.Id,
            offering.Slug,
            offering.Name,
            offering.Category,
            offering.Tagline,
            offering.CoverImageUrl,
            ToRiskString(offering.RiskLevel),
            funding.InvestorCount,
            ComputeDaysLeft(offering.DealTerms?.Deadline),
            funding.MinInvestment,
            funding.RaisedAmount,
            funding.FundingGoal,
            ComputeFundingProgressPercent(funding.RaisedAmount, funding.FundingGoal));
    }

    public static OfferingShellResponse ToShellResponse(InvestmentOffering offering)
    {
        var funding = offering.Funding!;
        var dealTerms = offering.DealTerms!;

        return new OfferingShellResponse(
            new OfferingSummaryDto(
                offering.Id,
                offering.Slug,
                offering.Name,
                offering.Category,
                offering.Tagline,
                offering.CoverImageUrl,
                ToRiskString(offering.RiskLevel),
                ComputeDaysLeft(dealTerms.Deadline),
                offering.Status.ToString().ToLowerInvariant()),
            new OfferingFundingDto(
                funding.RaisedAmount,
                funding.FundingGoal,
                funding.InvestorCount,
                funding.SharePrice,
                funding.TargetRating,
                funding.MinInvestment,
                funding.MaxInvestment,
                ComputeFundingProgressPercent(funding.RaisedAmount, funding.FundingGoal)),
            new DealTermsDto(
                dealTerms.TotalSharesOffered,
                dealTerms.PricePerShare,
                dealTerms.MinimumInvestment,
                dealTerms.MaximumInvestment,
                dealTerms.MinimumThreshold,
                dealTerms.FundingGoal,
                dealTerms.Deadline),
            offering.CorporateProfile == null
                ? null
                : new CorporateProfileDto(
                    offering.CorporateProfile.EntityType,
                    offering.CorporateProfile.Jurisdiction,
                    offering.CorporateProfile.IncorporationYear,
                    offering.CorporateProfile.RegistrationId,
                    offering.CorporateProfile.AdditionalNotes));
    }

    public static HighlightDto ToHighlightDto(Highlight highlight) =>
        new(
            highlight.Id,
            highlight.Kind.ToString().ToLowerInvariant(),
            highlight.Headline,
            highlight.Body,
            highlight.SortOrder);

    public static ContentBlockDto ToContentBlockDto(OfferingContentBlock block) =>
        new(
            block.Id,
            block.BlockType.ToString(),
            block.Key,
            block.Title,
            block.Summary,
            block.SortOrder,
            block.Items
                .OrderBy(i => i.SortOrder)
                .Select(i => new ContentBlockItemDto(i.Id, i.Label, i.Body, i.SortOrder))
                .ToList());

    public static TeamMemberDto ToTeamMemberDto(TeamMember member) =>
        new(member.Id, member.Name, member.Title, member.Bio, member.ImageUrl, member.SortOrder);

    public static FinancialMetricDto ToFinancialMetricDto(FinancialMetric metric) =>
        new(
            metric.Id,
            metric.MetricName,
            metric.PeriodLabel,
            metric.PeriodSortOrder,
            metric.Value,
            metric.Unit.ToString().ToLowerInvariant(),
            metric.CurrencyCode,
            metric.ValueType.ToString().ToLowerInvariant());

    public static UseOfProceedsItemDto ToUseOfProceedsDto(UseOfProceedsItem item) =>
        new(item.Id, item.AllocationPercent, item.Category, item.Description, item.SortOrder);

    public static OfferingRiskDto ToRiskDto(OfferingRisk risk) =>
        new(risk.Id, risk.Category, risk.Description, risk.Mitigation, risk.SortOrder);

    public static OfferingDocumentDto ToDocumentDto(OfferingDocument document) =>
        new(
            document.Id,
            document.Title,
            document.FileUrl,
            document.DocumentType.ToString(),
            document.PageCount);

    public static MediaAssetDto ToMediaAssetDto(MediaAsset asset) =>
        new(asset.Id, asset.AssetType.ToString(), asset.Url, asset.SortOrder);

    public static OfferingUpdateDto ToUpdateDto(OfferingUpdate update) =>
        new(update.Id, update.PublishedAt ?? DateTime.UtcNow, update.Title, update.Body, update.LikeCount);

    public static TestimonialDto ToTestimonialDto(Testimonial testimonial) =>
        new(
            testimonial.Id,
            testimonial.Quote,
            testimonial.AuthorName,
            testimonial.AuthorTitle,
            testimonial.ImageUrl,
            testimonial.SortOrder);

    public static bool TryParseRiskFilter(string? risk, out OfferingRiskLevel? parsed)
    {
        parsed = null;

        if (string.IsNullOrWhiteSpace(risk))
        {
            return true;
        }

        parsed = risk.Trim().ToLowerInvariant() switch
        {
            "low" => OfferingRiskLevel.Low,
            "moderate" => OfferingRiskLevel.Moderate,
            "high" => OfferingRiskLevel.High,
            _ => null,
        };

        return parsed.HasValue;
    }

    private static string ToRiskString(OfferingRiskLevel risk) =>
        risk switch
        {
            OfferingRiskLevel.Low => "low",
            OfferingRiskLevel.Moderate => "moderate",
            OfferingRiskLevel.High => "high",
            _ => risk.ToString().ToLowerInvariant(),
        };

    internal static int ComputeFundingProgressPercent(decimal raised, decimal goal) =>
        goal <= 0 ? 0 : (int)Math.Round(raised / goal * 100m, MidpointRounding.AwayFromZero);

    internal static int? ComputeDaysLeft(DateTime? deadline)
    {
        if (!deadline.HasValue)
        {
            return null;
        }

        var days = (int)Math.Ceiling((deadline.Value - DateTime.UtcNow).TotalDays);
        return Math.Max(0, days);
    }
}
