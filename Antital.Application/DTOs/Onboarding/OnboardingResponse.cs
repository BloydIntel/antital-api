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
    OnboardingKycDto? Kyc,
    OnboardingCorporateProfileDto? CorporateProfile = null
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
    string? RecentStatusReportDocumentPathOrKey,
    string? QiiLicenseEvidenceDocumentPathOrKey,
    string? BoardResolutionDocumentPathOrKey,
    string? IncorporationCertificateDocumentPathOrKey,
    bool GovernmentIdCompleted,
    bool ProofOfAddressCompleted,
    bool SelfieCompleted,
    bool IncomeCompleted
);

public record OnboardingCorporateProfileDto(
    OnboardingCorporateCompanyDto? Company,
    OnboardingCorporateAddressDto? Address,
    OnboardingCorporateRepresentativeDto? Representative,
    OnboardingCorporateQiiProfileDto? QiiProfile,
    OnboardingCorporateOciProfileDto? OciProfile
);

public record OnboardingCorporateCompanyDto(
    string? CompanyLegalName,
    string? TradingBrandName,
    string? RegistrationType,
    string? RegistrationNumber,
    string? CompanyLoginEmail
);

public record OnboardingCorporateAddressDto(
    DateTime? DateOfRegistration,
    string? CompanyWebsite,
    string? BusinessAddress,
    string? RegisteredAddress,
    string? CompanyEmail,
    string? CompanyPhone
);

public record OnboardingCorporateRepresentativeDto(
    string? RepresentativeFullName,
    string? RepresentativeJobTitle,
    string? RepresentativePhoneNumber,
    DateTime? RepresentativeDateOfBirth,
    string? RepresentativeEmail,
    string? RepresentativeNationality,
    string? RepresentativeCountryOfResidence,
    string? RepresentativeAddress
);

public record OnboardingCorporateQiiProfileDto(
    string? InstitutionTypesCommaSeparated,
    string? OtherInstitutionType,
    bool? HasValidQiiRegistrationOrLicense,
    bool? HasApprovedAlternativeInvestmentMandate,
    bool? ConfirmsSecNigeriaQiiCriteria
);

public record OnboardingCorporateOciProfileDto(
    bool? HasBoardResolutionOrInternalMandate,
    OciNetAssetValueRange? NetAssetValueRange,
    bool? HasFinancialCapacityToWithstandLoss,
    bool? UnderstandsCrowdfundingHighRiskLoss,
    bool? HasQualifiedInvestmentProfessionalsAccess
);
