using Antital.Application.DTOs.Onboarding;
using Antital.Application.Features.Onboarding.SaveOnboarding;
using Antital.Domain.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace Antital.Test.Application.Features.Onboarding;

public class SaveOnboardingCommandValidatorTests
{
    private readonly SaveOnboardingCommandValidator _validator = new();

    [Fact]
    public void InvestorCategoryStep_WithoutPayload_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestorCategory,
            null,
            null,
            null
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.InvestorCategoryPayload);
    }

    [Fact]
    public void InvestorCategoryStep_WithPayload_Passes()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestorCategory,
            new InvestorCategoryPayload(InvestorCategory.Retail),
            null,
            null
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvestmentProfileStep_WithoutPayload_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestmentProfile,
            null,
            null,
            null
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.InvestmentProfilePayload);
    }

    [Fact]
    public void InvestmentProfileStep_WithInvalidPercent_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestmentProfile,
            null,
            new InvestmentProfilePayload(
                InvestorCategory.Retail,
                150m, // > 100
                15m,
                null,
                null,
                null, null, null, null, null,
                null, null, null, null, null, null, null, null,
                null, null, null, null, null
            ),
            null
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.InvestmentProfilePayload!.HighRiskAllocationPast12MonthsPercent);
    }

    [Fact]
    public void InvestmentProfileStep_WithValidPayload_Passes()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestmentProfile,
            null,
            new InvestmentProfilePayload(
                InvestorCategory.Retail,
                10m,
                20m,
                "N5m-N10m",
                1_000_000m,
                true, true, true, true, true,
                null, null, null, null, null, null, null, null,
                null, null, null, null, null
            ),
            null
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void KycStep_WithoutPayload_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            null
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.KycPayload);
    }

    [Fact]
    public void KycStep_WithValidPayload_Passes()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            new KycPayload(KycIdType.NationalIdCard, "12345678901", "21234567890", null, null, null, null, null)
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void KycStep_WithValidPayload_NullNinAndBvn_Passes()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            new KycPayload(KycIdType.NationalIdCard, null, null, "path", null, null, null, null)
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void KycStep_WithInvalidNin_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            new KycPayload(KycIdType.NationalIdCard, "123", "21234567890", null, null, null, null, null)
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.KycPayload!.Nin)
            .WithErrorMessage("NIN must be exactly 11 digits.");
    }

    [Fact]
    public void KycStep_WithInvalidBvn_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            new KycPayload(KycIdType.NationalIdCard, "12345678901", "456", null, null, null, null, null)
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.KycPayload!.Bvn)
            .WithErrorMessage("BVN must be exactly 11 digits.");
    }

    [Fact]
    public void KycStep_WithNonDigitNin_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            new KycPayload(KycIdType.NationalIdCard, "1234567890a", "21234567890", null, null, null, null, null)
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.KycPayload!.Nin);
    }
}
