using Antital.Domain.Enums;

namespace Antital.Application.DTOs.Onboarding;

/// <summary>
/// Request for PUT /onboarding. Step plus the payload for that step.
/// </summary>
public record SaveOnboardingRequest(
    OnboardingStep Step,
    InvestorCategoryPayload? InvestorCategoryPayload,
    InvestmentProfilePayload? InvestmentProfilePayload,
    KycPayload? KycPayload,
    CorporateCompanyPayload? CorporateCompanyPayload = null,
    CorporateAddressPayload? CorporateAddressPayload = null,
    CorporateRepresentativePayload? CorporateRepresentativePayload = null,
    CorporateQiiProfilePayload? CorporateQiiProfilePayload = null,
    CorporateOciProfilePayload? CorporateOciProfilePayload = null,
    CorporateQiiDocumentsPayload? CorporateQiiDocumentsPayload = null,
    CorporateOciDocumentsPayload? CorporateOciDocumentsPayload = null
);
