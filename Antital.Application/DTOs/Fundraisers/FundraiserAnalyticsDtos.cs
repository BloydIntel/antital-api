namespace Antital.Application.DTOs.Fundraisers;

public record FundraiserAnalyticsOverviewDto(
    int TotalPageViews,
    int CampaignLikes,
    int SocialShares);

public record FundraiserAnalyticsTrafficPointDto(
    DateTime Date,
    string Label,
    int Value);

public record FundraiserAnalyticsTrafficDto(
    double AveragePerDay,
    string Unit,
    IReadOnlyList<FundraiserAnalyticsTrafficPointDto> Points);

public record FundraiserAnalyticsBucketDto(
    string Label,
    int Percentage);

public record FundraiserAnalyticsDiversityDto(
    string? TopLocation,
    IReadOnlyList<FundraiserAnalyticsBucketDto> Geographic,
    IReadOnlyList<FundraiserAnalyticsBucketDto> Categories);

public record FundraiserAnalyticsConversionDto(
    decimal ViewToInvestmentRate,
    double? AverageTimeToInvestHours,
    decimal ReturnVisitorRate);

public record FundraiserAnalyticsResponse(
    int? OfferingId,
    string? OfferingSlug,
    FundraiserAnalyticsOverviewDto Overview,
    FundraiserAnalyticsTrafficDto Traffic,
    FundraiserAnalyticsDiversityDto Diversity,
    FundraiserAnalyticsConversionDto Conversion);
