using System.Text.RegularExpressions;
using Antital.Domain.Enums;
using FluentValidation;

namespace Antital.Application.Features.Onboarding.SaveOnboarding;

public class SaveOnboardingCommandValidator : AbstractValidator<SaveOnboardingCommand>
{
    /// <summary>NIN (National Identification Number): exactly 11 digits.</summary>
    private static readonly Regex NinFormat = new(@"^\d{11}$", RegexOptions.Compiled);

    /// <summary>BVN (Bank Verification Number): exactly 11 digits.</summary>
    private static readonly Regex BvnFormat = new(@"^\d{11}$", RegexOptions.Compiled);

    public SaveOnboardingCommandValidator()
    {
        RuleFor(x => x.Step).IsInEnum();

        RuleFor(x => x)
            .Must(x =>
                x.InvestorCategoryPayload != null
                || x.CorporateCompanyPayload != null
                || x.CorporateAddressPayload != null
                || x.CorporateRepresentativePayload != null
                || x.FundRaiserCompanyPayload != null)
            .When(x => x.Step == OnboardingStep.InvestorCategory)
            .WithMessage("InvestorCategory step requires investorCategoryPayload or a corporate/fund raiser company payload.");

        RuleFor(x => x)
            .Must(x => x.Step != OnboardingStep.InvestorCategory || !HasNonInvestorCategoryPayloads(x))
            .WithMessage("InvestorCategory step only supports investorCategoryPayload and corporate company/address/representative payloads.");

        RuleFor(x => x)
            .Must(x => x.Step != OnboardingStep.InvestmentProfile || CountInvestmentProfilePayloads(x) == 1)
            .WithMessage("InvestmentProfile step requires exactly one payload: investmentProfilePayload, corporateQiiProfilePayload, or corporateOciProfilePayload.");

        RuleFor(x => x)
            .Must(x => x.Step != OnboardingStep.InvestmentProfile || !HasPayloadsOutsideInvestmentProfile(x))
            .WithMessage("InvestmentProfile step does not allow investor category, KYC, or corporate document payloads.");

        RuleFor(x => x)
            .Must(x => x.Step != OnboardingStep.Kyc || IsValidKycPayloadCombination(x))
            .WithMessage("Kyc step requires kycPayload and/or one document payload: corporateQiiDocumentsPayload, corporateOciDocumentsPayload, or fundRaiserBusinessDocumentsPayload.");

        RuleFor(x => x)
            .Must(x => x.Step != OnboardingStep.Kyc || !HasPayloadsOutsideKyc(x))
            .WithMessage("Kyc step does not allow investor category, investment profile, or company payloads.");

        RuleFor(x => x)
            .Must(x => x.Step != OnboardingStep.Review || x.FundRaiserPaymentPayload != null)
            .WithMessage("Review step requires fundRaiserPaymentPayload.");

        RuleFor(x => x)
            .Must(x => x.Step != OnboardingStep.Review || !HasPayloadsOutsideReview(x))
            .WithMessage("Review step only supports fundRaiserPaymentPayload.");

        When(x => x.Step == OnboardingStep.Review && x.FundRaiserPaymentPayload != null, () =>
        {
            RuleFor(x => x.FundRaiserPaymentPayload!.PaymentMethod).NotEmpty();
            RuleFor(x => x.FundRaiserPaymentPayload!.PaymentReference).NotEmpty();
            RuleFor(x => x.FundRaiserPaymentPayload!.PaymentStatus).NotEmpty();
            RuleFor(x => x.FundRaiserPaymentPayload!.ApplicationFeePaid).Equal(true);
        });

        When(x => x.Step == OnboardingStep.Kyc && x.FundRaiserRepresentativePayload != null, () =>
        {
            RuleFor(x => x.FundRaiserRepresentativePayload!.RepresentativeFullName).NotEmpty();
            RuleFor(x => x.FundRaiserRepresentativePayload!.RepresentativeJobTitle).NotEmpty();
            RuleFor(x => x.FundRaiserRepresentativePayload!.RepresentativePhoneNumber).NotEmpty();
            RuleFor(x => x.FundRaiserRepresentativePayload!.RepresentativeEmail).NotEmpty().EmailAddress();
            RuleFor(x => x.FundRaiserRepresentativePayload!.RepresentativeNationality).NotEmpty();
            RuleFor(x => x.FundRaiserRepresentativePayload!.RepresentativeCountryOfResidence).NotEmpty();
            RuleFor(x => x.FundRaiserRepresentativePayload!.RepresentativeAddress).NotEmpty();
        });

        When(x => x.Step == OnboardingStep.Kyc && x.FundRaiserBusinessDocumentsPayload != null, () =>
        {
            RuleFor(x => x.FundRaiserBusinessDocumentsPayload!.FounderAndTeamIntroductionDocumentPathOrKey).NotEmpty();
            RuleFor(x => x.FundRaiserBusinessDocumentsPayload!.FundraisingDeckDocumentPathOrKey).NotEmpty();
            RuleFor(x => x.FundRaiserBusinessDocumentsPayload!.InvestmentMemoDocumentPathOrKey).NotEmpty();
            RuleFor(x => x.FundRaiserBusinessDocumentsPayload!.TermsOfOfferingDocumentPathOrKey).NotEmpty();
            RuleFor(x => x.FundRaiserBusinessDocumentsPayload!.BusinessDescription).NotEmpty();
            RuleFor(x => x.FundRaiserBusinessDocumentsPayload!.BusinessSector).NotEmpty();
            RuleFor(x => x.FundRaiserBusinessDocumentsPayload!.InstrumentType).NotEmpty();
            RuleFor(x => x.FundRaiserBusinessDocumentsPayload!.BusinessSize).NotEmpty();
            RuleFor(x => x.FundRaiserBusinessDocumentsPayload!.InvestmentRound).NotEmpty();
            RuleFor(x => x.FundRaiserBusinessDocumentsPayload!.FundingTarget).NotNull();
            RuleFor(x => x.FundRaiserBusinessDocumentsPayload!.FundingTarget).GreaterThan(0);
        });

        RuleFor(x => x.InvestmentProfilePayload)
            .NotNull()
            .When(x => x.Step == OnboardingStep.InvestmentProfile
                && x.CorporateQiiProfilePayload == null
                && x.CorporateOciProfilePayload == null);

        RuleFor(x => x.KycPayload)
            .NotNull()
            .When(x => x.Step == OnboardingStep.Kyc
                && x.CorporateQiiDocumentsPayload == null
                && x.CorporateOciDocumentsPayload == null
                && x.FundRaiserBusinessDocumentsPayload == null
                && x.FundRaiserRepresentativePayload == null);

        When(x => x.Step == OnboardingStep.Kyc && x.KycPayload != null, () =>
        {
            RuleFor(x => x.KycPayload!.Nin)
                .Must(nin => string.IsNullOrWhiteSpace(nin) || NinFormat.IsMatch(nin))
                .WithMessage("NIN must be exactly 11 digits.");
            RuleFor(x => x.KycPayload!.Bvn)
                .Must(bvn => string.IsNullOrWhiteSpace(bvn) || BvnFormat.IsMatch(bvn))
                .WithMessage("BVN must be exactly 11 digits.");
        });

        When(x => x.Step == OnboardingStep.InvestmentProfile && x.InvestmentProfilePayload != null, () =>
        {
            RuleFor(x => x.InvestmentProfilePayload!.HighRiskAllocationPast12MonthsPercent)
                .InclusiveBetween(0, 100).When(x => x.InvestmentProfilePayload!.HighRiskAllocationPast12MonthsPercent.HasValue);
            RuleFor(x => x.InvestmentProfilePayload!.HighRiskAllocationNext12MonthsPercent)
                .InclusiveBetween(0, 100).When(x => x.InvestmentProfilePayload!.HighRiskAllocationNext12MonthsPercent.HasValue);
            RuleFor(x => x.InvestmentProfilePayload!.NetInvestmentAssetsValue)
                .GreaterThanOrEqualTo(0).When(x => x.InvestmentProfilePayload!.NetInvestmentAssetsValue.HasValue);

            // HNI: when user confirms net assets exceed ₦100m, they must select an asset range
            RuleFor(x => x.InvestmentProfilePayload!.NetInvestmentAssetsRange)
                .NotNull()
                .When(x => x.InvestmentProfilePayload!.InvestorCategory == InvestorCategory.HighNetWorth
                    && x.InvestmentProfilePayload.NetAssetsExceed100m == true);
        });

        When(x => x.CorporateQiiProfilePayload != null, () =>
        {
            RuleFor(x => x.CorporateQiiProfilePayload!.InstitutionTypes)
                .NotEmpty()
                .WithMessage("At least one institution type is required.");

            RuleFor(x => x.CorporateQiiProfilePayload!.OtherInstitutionType)
                .NotEmpty()
                .When(x => x.CorporateQiiProfilePayload!.InstitutionTypes != null
                    && x.CorporateQiiProfilePayload.InstitutionTypes.Contains(QiiInstitutionType.OtherRegulatedInstitution))
                .WithMessage("Other institution type is required when 'Other regulated institution' is selected.");
        });

        When(x => x.CorporateOciProfilePayload != null, () =>
        {
            RuleFor(x => x.CorporateOciProfilePayload!.NetAssetValueRange)
                .NotNull()
                .WithMessage("Net asset value range is required.");
        });
    }

    private static int CountInvestmentProfilePayloads(SaveOnboardingCommand x)
    {
        var count = 0;
        if (x.InvestmentProfilePayload != null) count++;
        if (x.CorporateQiiProfilePayload != null) count++;
        if (x.CorporateOciProfilePayload != null) count++;
        return count;
    }

    private static bool IsValidKycPayloadCombination(SaveOnboardingCommand x)
    {
        var hasKycPayload = x.KycPayload != null;
        var corporateDocumentsPayloadCount = 0;
        if (x.CorporateQiiDocumentsPayload != null) corporateDocumentsPayloadCount++;
        if (x.CorporateOciDocumentsPayload != null) corporateDocumentsPayloadCount++;
        if (x.FundRaiserBusinessDocumentsPayload != null) corporateDocumentsPayloadCount++;

        // At least one KYC-related payload must be present, and at most one document schema can be sent.
        return (hasKycPayload || corporateDocumentsPayloadCount == 1) && corporateDocumentsPayloadCount <= 1;
    }

    private static bool HasNonInvestorCategoryPayloads(SaveOnboardingCommand x) =>
        x.InvestmentProfilePayload != null
        || x.KycPayload != null
        || x.FundRaiserBusinessDocumentsPayload != null
        || x.FundRaiserRepresentativePayload != null
        || x.FundRaiserPaymentPayload != null
        || x.CorporateQiiProfilePayload != null
        || x.CorporateOciProfilePayload != null
        || x.CorporateQiiDocumentsPayload != null
        || x.CorporateOciDocumentsPayload != null;

    private static bool HasPayloadsOutsideInvestmentProfile(SaveOnboardingCommand x) =>
        x.InvestorCategoryPayload != null
        || x.KycPayload != null
        || x.FundRaiserBusinessDocumentsPayload != null
        || x.FundRaiserRepresentativePayload != null
        || x.FundRaiserPaymentPayload != null
        || x.FundRaiserCompanyPayload != null
        || x.CorporateCompanyPayload != null
        || x.CorporateAddressPayload != null
        || x.CorporateRepresentativePayload != null
        || x.CorporateQiiDocumentsPayload != null
        || x.CorporateOciDocumentsPayload != null;

    private static bool HasPayloadsOutsideKyc(SaveOnboardingCommand x) =>
        x.InvestorCategoryPayload != null
        || x.InvestmentProfilePayload != null
        || x.FundRaiserCompanyPayload != null
        || x.CorporateCompanyPayload != null
        || x.CorporateAddressPayload != null
        || x.CorporateRepresentativePayload != null
        || x.CorporateQiiProfilePayload != null
        || x.CorporateOciProfilePayload != null
        || x.FundRaiserPaymentPayload != null;

    private static bool HasPayloadsOutsideReview(SaveOnboardingCommand x) =>
        x.InvestorCategoryPayload != null
        || x.InvestmentProfilePayload != null
        || x.KycPayload != null
        || x.FundRaiserCompanyPayload != null
        || x.FundRaiserBusinessDocumentsPayload != null
        || x.FundRaiserRepresentativePayload != null
        || x.CorporateCompanyPayload != null
        || x.CorporateAddressPayload != null
        || x.CorporateRepresentativePayload != null
        || x.CorporateQiiProfilePayload != null
        || x.CorporateOciProfilePayload != null
        || x.CorporateQiiDocumentsPayload != null
        || x.CorporateOciDocumentsPayload != null;
}
