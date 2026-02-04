using Antital.Application.DTOs.Onboarding;
using Antital.Domain.Models;

namespace Antital.Application.Features.Onboarding;

/// <summary>
/// Maps onboarding entities to response DTOs. Single place for GET /api/onboarding shape.
/// </summary>
public static class OnboardingMappers
{
    public static OnboardingInvestorProfileDto? ToDto(this UserInvestmentProfile? profile)
    {
        if (profile == null) return null;
        return new OnboardingInvestorProfileDto(
            profile.InvestorCategory,
            profile.HighRiskAllocationPast12MonthsPercent,
            profile.HighRiskAllocationNext12MonthsPercent,
            profile.AnnualIncomeRange,
            profile.NetInvestmentAssetsValue,
            profile.CanAffordToLoseWithoutAffectingStability,
            profile.UnderstandsCrowdfundingIsHighRisk,
            profile.ReadRiskDisclosureAndSecRules,
            profile.UnderstandsPastPerformanceNoGuarantee,
            profile.AwareOfLimitedLiquidity,
            profile.YearsActivelyInvesting,
            profile.InvestmentTypes,
            profile.InvestedInPrivateMarketsBefore,
            profile.AwareOfLimitedLiquiditySophisticated,
            profile.ConfirmCrowdfundingAssessment,
            profile.SourceOfWealth,
            profile.SourceOfWealthOther,
            profile.ConfirmSecSophisticatedCriteria,
            profile.NetAssetsExceed100m,
            profile.NetInvestmentAssetsRange,
            profile.AdequateLiquidityForLosses,
            profile.AwareOfLimitedLiquidityHni,
            profile.ConfirmSecHniCriteria
        );
    }

    public static OnboardingKycDto? ToDto(this UserKyc? kyc)
    {
        if (kyc == null) return null;
        return new OnboardingKycDto(
            kyc.IdType,
            kyc.Nin,
            kyc.Bvn,
            kyc.GovernmentIdDocumentPathOrKey,
            kyc.ProofOfAddressDocumentPathOrKey,
            kyc.SelfieVerificationPathOrKey,
            kyc.IncomeVerificationPathOrKey,
            kyc.IncomeVerificationDocumentTypes,
            kyc.GovernmentIdVerifiedAt.HasValue,
            kyc.ProofOfAddressVerifiedAt.HasValue,
            kyc.SelfieVerifiedAt.HasValue,
            kyc.IncomeVerifiedAt.HasValue
        );
    }
}
