namespace Antital.Application.DTOs.Fundraisers;

public record FundraiserDashboardSummaryDto(
    decimal TotalAmountRaised,
    int TotalInvestors,
    int DaysRemaining,
    decimal AverageInvestmentSize);

public record FundraiserFundingProgressDto(
    decimal RaisedAmount,
    decimal TargetAmount,
    decimal MinimumThreshold,
    decimal CurrentVelocity,
    string VelocityPeriod,
    int ConfidenceRate);

public record FundraiserBreakdownBucketDto(string Label, int Percentage);

public record FundraiserInvestorBreakdownDto(
    string Dimension,
    IReadOnlyList<FundraiserBreakdownBucketDto> Buckets);

public record FundraiserMilestoneDto(
    string Key,
    string Title,
    string Description,
    string DateLabel,
    string Status);

public record FundraiserDashboardResponse(
    int? OfferingId,
    string? OfferingSlug,
    string? OfferingName,
    string Currency,
    FundraiserDashboardSummaryDto Summary,
    FundraiserFundingProgressDto FundingProgress,
    FundraiserInvestorBreakdownDto InvestorBreakdown,
    IReadOnlyList<FundraiserMilestoneDto> Milestones);
