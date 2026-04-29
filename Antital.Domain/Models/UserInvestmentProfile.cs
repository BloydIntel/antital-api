using BuildingBlocks.Domain.Models;
using Antital.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Antital.Domain.Models;

/// <summary>
/// Investment profile and risk assessment for individual investors (SEC categorization).
/// Does not duplicate User fields (name, DOB, address, etc.).
/// </summary>
public class UserInvestmentProfile : TrackableEntity
{
    public int UserId { get; set; }
    public InvestorCategory InvestorCategory { get; set; }

    /// <summary>Percentage of net investment assets in high-risk/speculative investments (past 12 months).</summary>
    public decimal? HighRiskAllocationPast12MonthsPercent { get; set; }

    /// <summary>Percentage of net investment assets intended for high-risk/speculative (next 12 months).</summary>
    public decimal? HighRiskAllocationNext12MonthsPercent { get; set; }

    [MaxLength(100)]
    public string? AnnualIncomeRange { get; set; }

    /// <summary>Estimated total value of net investment assets (excluding primary residence, car, personal items).</summary>
    public decimal? NetInvestmentAssetsValue { get; set; }

    public bool? CanAffordToLoseWithoutAffectingStability { get; set; }
    public bool? UnderstandsCrowdfundingIsHighRisk { get; set; }
    public bool? ReadRiskDisclosureAndSecRules { get; set; }
    public bool? UnderstandsPastPerformanceNoGuarantee { get; set; }
    public bool? AwareOfLimitedLiquidity { get; set; }

    // --- Sophisticated Investor profile ---
    public int? YearsActivelyInvesting { get; set; }
    /// <summary>Comma-separated InvestmentType enum values (e.g. "0,1,2").</summary>
    [MaxLength(100)]
    public string? InvestmentTypes { get; set; }
    public bool? InvestedInPrivateMarketsBefore { get; set; }
    public bool? AwareOfLimitedLiquiditySophisticated { get; set; }
    public bool? ConfirmCrowdfundingAssessment { get; set; }
    /// <summary>Comma-separated SourceOfWealth enum values.</summary>
    [MaxLength(200)]
    public string? SourceOfWealth { get; set; }
    [MaxLength(200)]
    public string? SourceOfWealthOther { get; set; }
    public bool? ConfirmSecSophisticatedCriteria { get; set; }

    // --- High Net-Worth Investor (HNI) profile ---
    public bool? NetAssetsExceed100m { get; set; }
    public NetInvestmentAssetsRange? NetInvestmentAssetsRange { get; set; }
    public bool? AdequateLiquidityForLosses { get; set; }
    public bool? AwareOfLimitedLiquidityHni { get; set; }
    public bool? ConfirmSecHniCriteria { get; set; }

    // --- Corporate company details ---
    [MaxLength(200)]
    public string? CompanyLegalName { get; set; }
    [MaxLength(200)]
    public string? TradingBrandName { get; set; }
    [MaxLength(100)]
    public string? RegistrationType { get; set; }
    [MaxLength(100)]
    public string? RegistrationNumber { get; set; }
    [MaxLength(255)]
    public string? CompanyLoginEmail { get; set; }

    // --- Corporate address ---
    public DateTime? DateOfRegistration { get; set; }
    [MaxLength(300)]
    public string? CompanyWebsite { get; set; }
    [MaxLength(500)]
    public string? BusinessAddress { get; set; }
    [MaxLength(500)]
    public string? RegisteredAddress { get; set; }
    [MaxLength(255)]
    public string? CompanyEmail { get; set; }
    [MaxLength(50)]
    public string? CompanyPhone { get; set; }

    // --- Corporate representative ---
    [MaxLength(200)]
    public string? RepresentativeFullName { get; set; }
    [MaxLength(100)]
    public string? RepresentativeJobTitle { get; set; }
    [MaxLength(50)]
    public string? RepresentativePhoneNumber { get; set; }
    public DateTime? RepresentativeDateOfBirth { get; set; }
    [MaxLength(255)]
    public string? RepresentativeEmail { get; set; }
    [MaxLength(100)]
    public string? RepresentativeNationality { get; set; }
    [MaxLength(100)]
    public string? RepresentativeCountryOfResidence { get; set; }
    [MaxLength(500)]
    public string? RepresentativeAddress { get; set; }

    // --- Corporate QII profile ---
    [MaxLength(200)]
    public string? QiiInstitutionTypes { get; set; }
    [MaxLength(200)]
    public string? QiiOtherInstitutionType { get; set; }
    public bool? HasValidQiiRegistrationOrLicense { get; set; }
    public bool? HasApprovedAlternativeInvestmentMandate { get; set; }
    public bool? ConfirmsSecNigeriaQiiCriteria { get; set; }

    // --- Corporate OCI profile ---
    public bool? HasBoardResolutionOrInternalMandate { get; set; }
    public OciNetAssetValueRange? OciNetAssetValueRange { get; set; }
    public bool? HasFinancialCapacityToWithstandLoss { get; set; }
    public bool? UnderstandsCrowdfundingHighRiskLoss { get; set; }
    public bool? HasQualifiedInvestmentProfessionalsAccess { get; set; }

    public virtual User User { get; set; } = null!;
}
