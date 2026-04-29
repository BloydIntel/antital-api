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

        RuleFor(x => x.InvestorCategoryPayload)
            .NotNull()
            .When(x => x.Step == OnboardingStep.InvestorCategory);

        RuleFor(x => x.InvestmentProfilePayload)
            .NotNull()
            .When(x => x.Step == OnboardingStep.InvestmentProfile
                && x.CorporateQiiProfilePayload == null
                && x.CorporateOciProfilePayload == null);

        RuleFor(x => x.KycPayload)
            .NotNull()
            .When(x => x.Step == OnboardingStep.Kyc
                && x.CorporateQiiDocumentsPayload == null
                && x.CorporateOciDocumentsPayload == null);

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
                .NotNull()
                .Must(types => types.Count > 0)
                .WithMessage("At least one institution type is required.");

            RuleFor(x => x.CorporateQiiProfilePayload!.OtherInstitutionType)
                .NotEmpty()
                .When(x => x.CorporateQiiProfilePayload!.InstitutionTypes.Contains(QiiInstitutionType.OtherRegulatedInstitution))
                .WithMessage("Other institution type is required when 'Other regulated institution' is selected.");
        });

        When(x => x.CorporateOciProfilePayload != null, () =>
        {
            RuleFor(x => x.CorporateOciProfilePayload!.NetAssetValueRange)
                .NotNull()
                .WithMessage("Net asset value range is required.");
        });
    }
}
