using Antital.Application.DTOs.Onboarding;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Onboarding.SaveOnboarding;

public class SaveOnboardingCommandHandler(
    IOnboardingUserAccess userAccess,
    IAntitalUnitOfWork unitOfWork,
    IUserOnboardingRepository userOnboardingRepository,
    IUserInvestmentProfileRepository userInvestmentProfileRepository,
    IUserKycRepository userKycRepository,
    IKycVerificationService kycVerificationService
) : ICommandQueryHandler<SaveOnboardingCommand>
{
    public async Task<Result> Handle(SaveOnboardingCommand request, CancellationToken cancellationToken)
    {
        var (userId, _) = await userAccess.RequireVerifiedUserAsync(cancellationToken);
        var onboarding = await userOnboardingRepository.GetOrCreateForUserAsync(userId, cancellationToken);

        switch (request.Step)
        {
            case OnboardingStep.InvestorCategory:
                await SaveInvestorCategoryAsync(userId, request.InvestorCategoryPayload!, cancellationToken);
                onboarding.CurrentStep = OnboardingStep.InvestmentProfile;
                break;
            case OnboardingStep.InvestmentProfile:
                await SaveInvestmentProfileAsync(userId, request.InvestmentProfilePayload!, cancellationToken);
                onboarding.CurrentStep = OnboardingStep.Kyc;
                break;
            case OnboardingStep.Kyc:
                await SaveKycAsync(userId, request.KycPayload!, cancellationToken);
                onboarding.CurrentStep = OnboardingStep.Review;
                break;
            case OnboardingStep.Review:
            case OnboardingStep.Submitted:
                // No data to save; step already set
                break;
            default:
                throw new BadRequestException("Invalid onboarding step.", new Dictionary<string, string[]>());
        }

        await userOnboardingRepository.UpdateAsync(onboarding, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result();
        result.OK();
        return result;
    }

    private async Task SaveInvestorCategoryAsync(int userId, InvestorCategoryPayload payload, CancellationToken cancellationToken)
    {
        var profile = await userInvestmentProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            profile = new UserInvestmentProfile { UserId = userId, InvestorCategory = payload.InvestorCategory };
            await userInvestmentProfileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.InvestorCategory = payload.InvestorCategory;
            await userInvestmentProfileRepository.UpdateAsync(profile, cancellationToken);
        }
    }

    private async Task SaveInvestmentProfileAsync(int userId, InvestmentProfilePayload payload, CancellationToken cancellationToken)
    {
        var profile = await userInvestmentProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        var isNew = profile == null;
        profile ??= new UserInvestmentProfile { UserId = userId };
        Apply(payload, profile);
        if (isNew) { await userInvestmentProfileRepository.AddAsync(profile, cancellationToken); }
        else { await userInvestmentProfileRepository.UpdateAsync(profile, cancellationToken); }
    }

    private static void Apply(InvestmentProfilePayload payload, UserInvestmentProfile profile)
    {
        profile.InvestorCategory = payload.InvestorCategory;
        profile.HighRiskAllocationPast12MonthsPercent = payload.HighRiskAllocationPast12MonthsPercent;
        profile.HighRiskAllocationNext12MonthsPercent = payload.HighRiskAllocationNext12MonthsPercent;
        profile.AnnualIncomeRange = payload.AnnualIncomeRange;
        profile.NetInvestmentAssetsValue = payload.NetInvestmentAssetsValue;
        profile.CanAffordToLoseWithoutAffectingStability = payload.CanAffordToLoseWithoutAffectingStability;
        profile.UnderstandsCrowdfundingIsHighRisk = payload.UnderstandsCrowdfundingIsHighRisk;
        profile.ReadRiskDisclosureAndSecRules = payload.ReadRiskDisclosureAndSecRules;
        profile.UnderstandsPastPerformanceNoGuarantee = payload.UnderstandsPastPerformanceNoGuarantee;
        profile.AwareOfLimitedLiquidity = payload.AwareOfLimitedLiquidity;
        profile.YearsActivelyInvesting = payload.YearsActivelyInvesting;
        profile.InvestmentTypes = payload.InvestmentTypesCommaSeparated;
        profile.InvestedInPrivateMarketsBefore = payload.InvestedInPrivateMarketsBefore;
        profile.AwareOfLimitedLiquiditySophisticated = payload.AwareOfLimitedLiquiditySophisticated;
        profile.ConfirmCrowdfundingAssessment = payload.ConfirmCrowdfundingAssessment;
        profile.SourceOfWealth = payload.SourceOfWealthCommaSeparated;
        profile.SourceOfWealthOther = payload.SourceOfWealthOther;
        profile.ConfirmSecSophisticatedCriteria = payload.ConfirmSecSophisticatedCriteria;
        profile.NetAssetsExceed100m = payload.NetAssetsExceed100m;
        profile.NetInvestmentAssetsRange = payload.NetInvestmentAssetsRange;
        profile.AdequateLiquidityForLosses = payload.AdequateLiquidityForLosses;
        profile.AwareOfLimitedLiquidityHni = payload.AwareOfLimitedLiquidityHni;
        profile.ConfirmSecHniCriteria = payload.ConfirmSecHniCriteria;
    }

    private async Task SaveKycAsync(int userId, KycPayload payload, CancellationToken cancellationToken)
    {
        var input = new KycVerificationInput(
            userId,
            (int)payload.IdType,
            payload.Nin,
            payload.Bvn,
            payload.GovernmentIdDocumentPathOrKey,
            payload.ProofOfAddressDocumentPathOrKey,
            payload.SelfieVerificationPathOrKey,
            payload.IncomeVerificationPathOrKey,
            payload.IncomeVerificationDocumentTypesCommaSeparated
        );
        var result = await kycVerificationService.ProcessAsync(input, cancellationToken);

        var kyc = await userKycRepository.GetByUserIdAsync(userId, cancellationToken);
        var isNew = kyc == null;
        kyc ??= new UserKyc { UserId = userId };
        Apply(payload, result, kyc);
        if (isNew) { await userKycRepository.AddAsync(kyc, cancellationToken); }
        else { await userKycRepository.UpdateAsync(kyc, cancellationToken); }
    }

    private static void Apply(KycPayload payload, KycVerificationResult result, UserKyc kyc)
    {
        kyc.IdType = payload.IdType;
        kyc.Nin = payload.Nin;
        kyc.Bvn = payload.Bvn;
        kyc.GovernmentIdDocumentPathOrKey = result.GovernmentIdDocumentPathOrKey;
        kyc.ProofOfAddressDocumentPathOrKey = result.ProofOfAddressDocumentPathOrKey;
        kyc.SelfieVerificationPathOrKey = result.SelfieVerificationPathOrKey;
        kyc.IncomeVerificationPathOrKey = result.IncomeVerificationPathOrKey;
        kyc.IncomeVerificationDocumentTypes = payload.IncomeVerificationDocumentTypesCommaSeparated;
        kyc.GovernmentIdVerifiedAt = result.GovernmentIdVerifiedAt;
        kyc.ProofOfAddressVerifiedAt = result.ProofOfAddressVerifiedAt;
        kyc.SelfieVerifiedAt = result.SelfieVerifiedAt;
        kyc.IncomeVerifiedAt = result.IncomeVerifiedAt;
    }
}
