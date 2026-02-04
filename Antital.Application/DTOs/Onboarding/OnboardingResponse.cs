using Antital.Domain.Enums;

namespace Antital.Application.DTOs.Onboarding;

/// <summary>
/// Response for GET /onboarding. Progress and aggregated data for resume and Review screen.
/// Personal and location data come from User; no duplication.
/// </summary>
public record OnboardingResponse(
    OnboardingStep CurrentStep,
    OnboardingStatus Status,
    DateTime? SubmittedAt,
    OnboardingPersonalInfoDto? PersonalInfo,
    OnboardingLocationInfoDto? LocationInfo,
    OnboardingInvestorProfileDto? InvestorProfile,
    OnboardingKycDto? Kyc
);

public record OnboardingPersonalInfoDto(
    string FullName,
    string Email,
    string? PreferredName,
    string PhoneNumber,
    DateTime DateOfBirth
);

public record OnboardingLocationInfoDto(
    string Nationality,
    string CountryOfResidence,
    string StateOfResidence,
    string ResidentialAddress
);

public record OnboardingInvestorProfileDto(
    InvestorCategory? InvestorCategory,
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

public record OnboardingKycDto(
    KycIdType? IdType,
    string? Nin,
    string? Bvn,
    string? GovernmentIdDocumentPathOrKey,
    string? ProofOfAddressDocumentPathOrKey,
    string? SelfieVerificationPathOrKey,
    string? IncomeVerificationPathOrKey,
    string? IncomeVerificationDocumentTypesCommaSeparated,
    bool GovernmentIdCompleted,
    bool ProofOfAddressCompleted,
    bool SelfieCompleted,
    bool IncomeCompleted
);
