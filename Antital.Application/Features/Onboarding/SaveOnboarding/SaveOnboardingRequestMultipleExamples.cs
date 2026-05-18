using Antital.Application.DTOs.Onboarding;
using Antital.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Onboarding.SaveOnboarding;

/// <summary>
/// Multiple Swagger examples for PUT /api/onboarding so the frontend can see the payload for each step.
/// </summary>
public class SaveOnboardingRequestMultipleExamples : IMultipleExamplesProvider<SaveOnboardingRequest>
{
    public IEnumerable<SwaggerExample<SaveOnboardingRequest>> GetExamples()
    {
        yield return SwaggerExample.Create(
            "Investor Category",
            "Send when saving investor category (step 0). Only investorCategoryPayload is set.",
            new SaveOnboardingRequest(
                Step: OnboardingStep.InvestorCategory,
                InvestorCategoryPayload: new InvestorCategoryPayload(InvestorCategory.Retail),
                InvestmentProfilePayload: null,
                KycPayload: null,
                CorporateCompanyPayload: null,
                CorporateAddressPayload: null,
                CorporateRepresentativePayload: null,
                FundRaiserCompanyPayload: null,
                FundRaiserBusinessDocumentsPayload: null,
                FundRaiserRepresentativePayload: null,
                FundRaiserPaymentPayload: null,
                CorporateQiiProfilePayload: null,
                CorporateOciProfilePayload: null,
                CorporateQiiDocumentsPayload: null,
                CorporateOciDocumentsPayload: null));

        yield return SwaggerExample.Create(
            "Investment Profile (Retail)",
            "Send when saving investment profile (step 1). Only investmentProfilePayload is set. Example: Retail.",
            new SaveOnboardingRequest(
                Step: OnboardingStep.InvestmentProfile,
                InvestorCategoryPayload: null,
                InvestmentProfilePayload: new InvestmentProfilePayload(
                    InvestorCategory.Retail,
                    10m, 20m, "N5m-N10m", 5_000_000m,
                    true, true, true, true, true,
                    null, null, null, null, null, null, null, null,
                    null, null, null, null, null),
                KycPayload: null,
                CorporateCompanyPayload: null,
                CorporateAddressPayload: null,
                CorporateRepresentativePayload: null,
                FundRaiserCompanyPayload: null,
                FundRaiserBusinessDocumentsPayload: null,
                FundRaiserRepresentativePayload: null,
                FundRaiserPaymentPayload: null,
                CorporateQiiProfilePayload: null,
                CorporateOciProfilePayload: null,
                CorporateQiiDocumentsPayload: null,
                CorporateOciDocumentsPayload: null));

        yield return SwaggerExample.Create(
            "KYC",
            "Send when saving KYC (step 2). Only kycPayload is set.",
            new SaveOnboardingRequest(
                Step: OnboardingStep.Kyc,
                InvestorCategoryPayload: null,
                InvestmentProfilePayload: null,
                KycPayload: new KycPayload(KycIdType.NationalIdCard, "12345678901", "21234567890", "path/gov-id.pdf", "path/proof-of-address.pdf", "path/selfie.jpg", null, null),
                CorporateCompanyPayload: null,
                CorporateAddressPayload: null,
                CorporateRepresentativePayload: null,
                FundRaiserCompanyPayload: null,
                FundRaiserBusinessDocumentsPayload: null,
                FundRaiserRepresentativePayload: null,
                FundRaiserPaymentPayload: null,
                CorporateQiiProfilePayload: null,
                CorporateOciProfilePayload: null,
                CorporateQiiDocumentsPayload: null,
                CorporateOciDocumentsPayload: null));

        yield return SwaggerExample.Create(
            "Corporate Company Details",
            "InvestorCategory step requires investorCategoryPayload. This sample also includes corporateCompanyPayload for reference.",
            new SaveOnboardingRequest(
                Step: OnboardingStep.InvestorCategory,
                InvestorCategoryPayload: new InvestorCategoryPayload(InvestorCategory.Retail),
                InvestmentProfilePayload: null,
                KycPayload: null,
                CorporateCompanyPayload: new CorporateCompanyPayload(
                    CompanyLegalName: "Acme Ventures Limited",
                    TradingBrandName: "Acme Ventures",
                    RegistrationType: "LTD (Limited Liability)",
                    RegistrationNumber: "RC123456",
                    CompanyLoginEmail: "ops@acmeventures.com"
                ),
                CorporateAddressPayload: null,
                CorporateRepresentativePayload: null,
                FundRaiserCompanyPayload: null,
                FundRaiserBusinessDocumentsPayload: null,
                FundRaiserRepresentativePayload: null,
                FundRaiserPaymentPayload: null,
                CorporateQiiProfilePayload: null,
                CorporateOciProfilePayload: null,
                CorporateQiiDocumentsPayload: null,
                CorporateOciDocumentsPayload: null));

        yield return SwaggerExample.Create(
            "Corporate QII Profile",
            "Send when saving QII investment profile answers. Only corporateQiiProfilePayload is set.",
            new SaveOnboardingRequest(
                Step: OnboardingStep.InvestmentProfile,
                InvestorCategoryPayload: null,
                InvestmentProfilePayload: null,
                KycPayload: null,
                CorporateCompanyPayload: null,
                CorporateAddressPayload: null,
                CorporateRepresentativePayload: null,
                FundRaiserCompanyPayload: null,
                FundRaiserBusinessDocumentsPayload: null,
                FundRaiserRepresentativePayload: null,
                FundRaiserPaymentPayload: null,
                CorporateQiiProfilePayload: new CorporateQiiProfilePayload(
                    InstitutionTypes: [QiiInstitutionType.AssetManagementCompany, QiiInstitutionType.CorporateFinanceInstitution],
                    OtherInstitutionType: null,
                    HasValidQiiRegistrationOrLicense: true,
                    HasApprovedAlternativeInvestmentMandate: true,
                    ConfirmsSecNigeriaQiiCriteria: true
                ),
                CorporateOciProfilePayload: null,
                CorporateQiiDocumentsPayload: null,
                CorporateOciDocumentsPayload: null));

        yield return SwaggerExample.Create(
            "Corporate OCI Profile",
            "Send when saving OCI investment profile answers. Only corporateOciProfilePayload is set.",
            new SaveOnboardingRequest(
                Step: OnboardingStep.InvestmentProfile,
                InvestorCategoryPayload: null,
                InvestmentProfilePayload: null,
                KycPayload: null,
                CorporateCompanyPayload: null,
                CorporateAddressPayload: null,
                CorporateRepresentativePayload: null,
                FundRaiserCompanyPayload: null,
                FundRaiserBusinessDocumentsPayload: null,
                FundRaiserRepresentativePayload: null,
                FundRaiserPaymentPayload: null,
                CorporateQiiProfilePayload: null,
                CorporateOciProfilePayload: new CorporateOciProfilePayload(
                    HasBoardResolutionOrInternalMandate: true,
                    NetAssetValueRange: OciNetAssetValueRange.Range10To50Million,
                    HasFinancialCapacityToWithstandLoss: true,
                    UnderstandsCrowdfundingHighRiskLoss: true,
                    HasQualifiedInvestmentProfessionalsAccess: true
                ),
                CorporateQiiDocumentsPayload: null,
                CorporateOciDocumentsPayload: null));

        yield return SwaggerExample.Create(
            "Corporate OCI Additional Docs",
            "Send when saving OCI document uploads. Only corporateOciDocumentsPayload is set.",
            new SaveOnboardingRequest(
                Step: OnboardingStep.Kyc,
                InvestorCategoryPayload: null,
                InvestmentProfilePayload: null,
                KycPayload: null,
                CorporateCompanyPayload: null,
                CorporateAddressPayload: null,
                CorporateRepresentativePayload: null,
                FundRaiserCompanyPayload: null,
                FundRaiserBusinessDocumentsPayload: null,
                FundRaiserRepresentativePayload: null,
                FundRaiserPaymentPayload: null,
                CorporateQiiProfilePayload: null,
                CorporateOciProfilePayload: null,
                CorporateQiiDocumentsPayload: null,
                CorporateOciDocumentsPayload: new CorporateOciDocumentsPayload(
                    IncorporationCertificateDocumentPathOrKey: "docs/incorporation-certificate.pdf",
                    RecentStatusReportDocumentPathOrKey: "docs/recent-status-report.pdf",
                    BoardResolutionDocumentPathOrKey: "docs/board-resolution.pdf"
                )));
    }
}
