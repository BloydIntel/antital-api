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
                OnboardingStep.InvestorCategory,
                new InvestorCategoryPayload(InvestorCategory.Retail),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));

        yield return SwaggerExample.Create(
            "Investment Profile (Retail)",
            "Send when saving investment profile (step 1). Only investmentProfilePayload is set. Example: Retail.",
            new SaveOnboardingRequest(
                OnboardingStep.InvestmentProfile,
                null,
                new InvestmentProfilePayload(
                    InvestorCategory.Retail,
                    10m, 20m, "N5m-N10m", 5_000_000m,
                    true, true, true, true, true,
                    null, null, null, null, null, null, null, null,
                    null, null, null, null, null),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null));

        yield return SwaggerExample.Create(
            "KYC",
            "Send when saving KYC (step 2). Only kycPayload is set.",
            new SaveOnboardingRequest(
                OnboardingStep.Kyc,
                null,
                null,
                new KycPayload(KycIdType.NationalIdCard, "12345678901", "21234567890", "path/gov-id.pdf", "path/proof-of-address.pdf", "path/selfie.jpg", null, null),
                null,
                null,
                null,
                null,
                null,
                null,
                null));

        yield return SwaggerExample.Create(
            "Corporate Company Details",
            "Send when saving corporate company details. Only corporateCompanyPayload is set.",
            new SaveOnboardingRequest(
                OnboardingStep.InvestorCategory,
                null,
                null,
                null,
                new CorporateCompanyPayload(
                    CompanyLegalName: "Acme Ventures Limited",
                    TradingBrandName: "Acme Ventures",
                    RegistrationType: "LTD (Limited Liability)",
                    RegistrationNumber: "RC123456",
                    CompanyLoginEmail: "ops@acmeventures.com"
                ),
                null,
                null,
                null,
                null,
                null,
                null));

        yield return SwaggerExample.Create(
            "Corporate QII Profile",
            "Send when saving QII investment profile answers. Only corporateQiiProfilePayload is set.",
            new SaveOnboardingRequest(
                OnboardingStep.InvestmentProfile,
                null,
                null,
                null,
                null,
                null,
                null,
                new CorporateQiiProfilePayload(
                    InstitutionTypes: [QiiInstitutionType.AssetManagementCompany, QiiInstitutionType.CorporateFinanceInstitution],
                    OtherInstitutionType: null,
                    HasValidQiiRegistrationOrLicense: true,
                    HasApprovedAlternativeInvestmentMandate: true,
                    ConfirmsSecNigeriaQiiCriteria: true
                ),
                null,
                null,
                null));

        yield return SwaggerExample.Create(
            "Corporate OCI Profile",
            "Send when saving OCI investment profile answers. Only corporateOciProfilePayload is set.",
            new SaveOnboardingRequest(
                OnboardingStep.InvestmentProfile,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                new CorporateOciProfilePayload(
                    HasBoardResolutionOrInternalMandate: true,
                    NetAssetValueRange: OciNetAssetValueRange._10To50Million,
                    HasFinancialCapacityToWithstandLoss: true,
                    UnderstandsCrowdfundingHighRiskLoss: true,
                    HasQualifiedInvestmentProfessionalsAccess: true
                ),
                null,
                null));

        yield return SwaggerExample.Create(
            "Corporate OCI Additional Docs",
            "Send when saving OCI document uploads. Only corporateOciDocumentsPayload is set.",
            new SaveOnboardingRequest(
                OnboardingStep.Kyc,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                new CorporateOciDocumentsPayload(
                    IncorporationCertificateDocumentPathOrKey: "docs/incorporation-certificate.pdf",
                    RecentStatusReportDocumentPathOrKey: "docs/recent-status-report.pdf",
                    BoardResolutionDocumentPathOrKey: "docs/board-resolution.pdf"
                )));
    }
}
