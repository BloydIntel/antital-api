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
        result.ShouldHaveValidationErrorFor(c => c);
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
    public void InvestorCategoryStep_WithCorporateCompanyPayload_Passes()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestorCategory,
            null,
            null,
            null,
            CorporateCompanyPayload: new CorporateCompanyPayload(
                CompanyLegalName: "Acme Ventures Limited",
                TradingBrandName: "Acme Ventures",
                RegistrationType: "LTD",
                RegistrationNumber: "RC123456",
                CompanyLoginEmail: "ops@acmeventures.com"
            )
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void InvestorCategoryStep_WithFundRaiserCompanyPayload_Passes()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestorCategory,
            null,
            null,
            null,
            FundRaiserCompanyPayload: new FundRaiserCompanyPayload(
                CompanyLegalName: "Acme Fundraise Ltd",
                TradingBrandName: "Acme Raise",
                RegistrationType: "LTD",
                RegistrationNumber: "RC789456",
                CompanyLoginEmail: "ops@acmeraise.com",
                DateOfRegistration: new DateTime(2020, 1, 15),
                CompanyWebsite: "https://acmeraise.com",
                BusinessAddress: "123 Business Street",
                RegisteredAddress: "123 Business Street",
                CompanyEmail: "contact@acmeraise.com",
                CompanyPhone: "+2348011111111"
            )
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
        result.ShouldHaveValidationErrorFor(c => c);
    }

    [Fact]
    public void InvestmentProfileStep_WithQiiPayloadAndKycPayload_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestmentProfile,
            null,
            null,
            new KycPayload(KycIdType.NationalIdCard, null, null, null, null, null, null, null),
            CorporateQiiProfilePayload: new CorporateQiiProfilePayload(
                [QiiInstitutionType.Bank],
                null,
                true,
                true,
                true
            )
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c);
    }

    [Fact]
    public void InvestmentProfileStep_WithBothQiiAndOciPayload_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestmentProfile,
            null,
            null,
            null,
            CorporateQiiProfilePayload: new CorporateQiiProfilePayload(
                [QiiInstitutionType.Bank],
                null,
                true,
                true,
                true
            ),
            CorporateOciProfilePayload: new CorporateOciProfilePayload(
                true,
                OciNetAssetValueRange.Below10Million,
                true,
                true,
                true
            )
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c);
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
    public void InvestmentProfileStep_WithFundRaiserCompanyPayload_Fails()
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
            null,
            FundRaiserCompanyPayload: new FundRaiserCompanyPayload(
                CompanyLegalName: "Acme Fundraise Ltd",
                TradingBrandName: "Acme Raise",
                RegistrationType: "LTD",
                RegistrationNumber: "RC789456",
                CompanyLoginEmail: "ops@acmeraise.com",
                DateOfRegistration: new DateTime(2020, 1, 15),
                CompanyWebsite: "https://acmeraise.com",
                BusinessAddress: "123 Business Street",
                RegisteredAddress: "123 Business Street",
                CompanyEmail: "contact@acmeraise.com",
                CompanyPhone: "+2348011111111"
            )
        );

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c);
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
        result.ShouldHaveValidationErrorFor(c => c);
    }

    [Fact]
    public void KycStep_WithKycAndCorporateDocsTogether_Passes()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            new KycPayload(KycIdType.NationalIdCard, "12345678901", "21234567890", null, null, null, null, null),
            CorporateOciDocumentsPayload: new CorporateOciDocumentsPayload(
                "inc.pdf",
                "status.pdf",
                "board.pdf"
            )
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void KycStep_WithBothCorporateDocPayloads_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            null,
            CorporateQiiDocumentsPayload: new CorporateQiiDocumentsPayload(
                "qii-status.pdf",
                "qii-license.pdf",
                "qii-board.pdf"
            ),
            CorporateOciDocumentsPayload: new CorporateOciDocumentsPayload(
                "oci-inc.pdf",
                "oci-status.pdf",
                "oci-board.pdf"
            )
        );

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c);
    }

    [Fact]
    public void KycStep_WithFundRaiserDocumentsAndRepresentative_Passes()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            new KycPayload(KycIdType.NationalIdCard, "12345678901", "21234567890", "gov-id.png", "address.png", "selfie.png", null, null),
            FundRaiserBusinessDocumentsPayload: new FundRaiserBusinessDocumentsPayload(
                "founders.png",
                "deck.png",
                "memo.png",
                "terms.png",
                null,
                "Business overview",
                "Technology",
                "Equity Investment Contracts",
                "Micro",
                10_000_000m,
                "Pre-Seed Round"
            ),
            FundRaiserRepresentativePayload: new FundRaiserRepresentativePayload(
                "John Doe",
                "Director",
                "+2348011111111",
                new DateTime(1990, 1, 1),
                "john@example.com",
                "Nigerian",
                "Nigeria",
                "Lekki, Lagos"
            )
        );

        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void KycStep_WithFundRaiserDocumentsWithoutRepresentative_Passes()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            null,
            FundRaiserBusinessDocumentsPayload: new FundRaiserBusinessDocumentsPayload(
                "founders.png",
                "deck.png",
                "memo.png",
                "terms.png",
                null,
                "Business overview",
                "Technology",
                "Equity Investment Contracts",
                "Micro",
                10_000_000m,
                "Pre-Seed Round"
            ),
            FundRaiserRepresentativePayload: null
        );

        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void KycStep_WithQiiProfilePayload_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Kyc,
            null,
            null,
            null,
            CorporateQiiProfilePayload: new CorporateQiiProfilePayload(
                [QiiInstitutionType.Bank],
                null,
                true,
                true,
                true
            )
        );
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c);
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

    [Fact]
    public void CorporateQiiProfile_WithNoInstitutionTypes_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestmentProfile,
            null,
            null,
            null,
            CorporateQiiProfilePayload: new CorporateQiiProfilePayload(
                InstitutionTypes: [],
                OtherInstitutionType: null,
                HasValidQiiRegistrationOrLicense: true,
                HasApprovedAlternativeInvestmentMandate: true,
                ConfirmsSecNigeriaQiiCriteria: true
            )
        );

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.CorporateQiiProfilePayload!.InstitutionTypes);
    }

    [Fact]
    public void CorporateQiiProfile_WithNullInstitutionTypes_FailsWithoutThrowing()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestmentProfile,
            null,
            null,
            null,
            CorporateQiiProfilePayload: new CorporateQiiProfilePayload(
                InstitutionTypes: null!,
                OtherInstitutionType: null,
                HasValidQiiRegistrationOrLicense: true,
                HasApprovedAlternativeInvestmentMandate: true,
                ConfirmsSecNigeriaQiiCriteria: true
            )
        );

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.CorporateQiiProfilePayload!.InstitutionTypes);
    }

    [Fact]
    public void CorporateQiiProfile_WithOtherInstitutionButNoDetail_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestmentProfile,
            null,
            null,
            null,
            CorporateQiiProfilePayload: new CorporateQiiProfilePayload(
                InstitutionTypes: [QiiInstitutionType.OtherRegulatedInstitution],
                OtherInstitutionType: null,
                HasValidQiiRegistrationOrLicense: true,
                HasApprovedAlternativeInvestmentMandate: true,
                ConfirmsSecNigeriaQiiCriteria: true
            )
        );

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.CorporateQiiProfilePayload!.OtherInstitutionType);
    }

    [Fact]
    public void CorporateOciProfile_WithoutNetAssetValueRange_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.InvestmentProfile,
            null,
            null,
            null,
            CorporateOciProfilePayload: new CorporateOciProfilePayload(
                HasBoardResolutionOrInternalMandate: true,
                NetAssetValueRange: null,
                HasFinancialCapacityToWithstandLoss: true,
                UnderstandsCrowdfundingHighRiskLoss: true,
                HasQualifiedInvestmentProfessionalsAccess: true
            )
        );

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.CorporateOciProfilePayload!.NetAssetValueRange);
    }

    [Fact]
    public void ReviewStep_WithFundRaiserPaymentPayload_Passes()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Review,
            null,
            null,
            null,
            FundRaiserPaymentPayload: new FundRaiserPaymentPayload(
                PaymentMethod: "Bank Transfer",
                PaymentReference: "PAY-REF-001",
                PaymentStatus: "Paid",
                ApplicationFeePaid: true
            )
        );

        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ReviewStep_WithoutPayload_Fails()
    {
        var cmd = new SaveOnboardingCommand(OnboardingStep.Review, null, null, null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c);
    }

    [Fact]
    public void ReviewStep_WithApplicationFeeUnpaid_Fails()
    {
        var cmd = new SaveOnboardingCommand(
            OnboardingStep.Review,
            null,
            null,
            null,
            FundRaiserPaymentPayload: new FundRaiserPaymentPayload(
                PaymentMethod: "Card",
                PaymentReference: "PAY-REF-002",
                PaymentStatus: "Pending",
                ApplicationFeePaid: false
            )
        );

        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.FundRaiserPaymentPayload!.ApplicationFeePaid);
    }
}
