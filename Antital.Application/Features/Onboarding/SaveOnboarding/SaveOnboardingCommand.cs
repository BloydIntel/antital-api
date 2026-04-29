using Antital.Application.DTOs.Onboarding;
using Antital.Domain.Enums;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Onboarding.SaveOnboarding;

public record SaveOnboardingCommand(
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
) : ICommandQuery;
