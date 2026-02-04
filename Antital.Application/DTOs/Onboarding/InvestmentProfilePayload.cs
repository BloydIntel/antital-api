using Antital.Domain.Enums;

namespace Antital.Application.DTOs.Onboarding;

/// <summary>
/// Payload for the Investment Profile step. No User fields (name, address, etc.).
/// Retail, Sophisticated, and HNI each use a subset of fields per category.
/// </summary>
public record InvestmentProfilePayload(
    InvestorCategory InvestorCategory,
    // Retail
    decimal? HighRiskAllocationPast12MonthsPercent,
    decimal? HighRiskAllocationNext12MonthsPercent,
    string? AnnualIncomeRange,
    decimal? NetInvestmentAssetsValue,
    bool? CanAffordToLoseWithoutAffectingStability,
    bool? UnderstandsCrowdfundingIsHighRisk,
    bool? ReadRiskDisclosureAndSecRules,
    bool? UnderstandsPastPerformanceNoGuarantee,
    bool? AwareOfLimitedLiquidity,
    // Sophisticated
    int? YearsActivelyInvesting,
    string? InvestmentTypesCommaSeparated,
    bool? InvestedInPrivateMarketsBefore,
    bool? AwareOfLimitedLiquiditySophisticated,
    bool? ConfirmCrowdfundingAssessment,
    string? SourceOfWealthCommaSeparated,
    string? SourceOfWealthOther,
    bool? ConfirmSecSophisticatedCriteria,
    // HNI
    bool? NetAssetsExceed100m,
    NetInvestmentAssetsRange? NetInvestmentAssetsRange,
    bool? AdequateLiquidityForLosses,
    bool? AwareOfLimitedLiquidityHni,
    bool? ConfirmSecHniCriteria
);
