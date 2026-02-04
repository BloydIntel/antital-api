using Antital.Application.DTOs.Onboarding;
using Antital.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Onboarding.SaveOnboarding;

/// <summary>
/// Example for PUT /api/onboarding - saving the Investor Category step.
/// For InvestmentProfile step, send step: 1 and investmentProfilePayload. For Kyc step, send step: 2 and kycPayload.
/// </summary>
public class SaveOnboardingRequestExample : IExamplesProvider<SaveOnboardingRequest>
{
    public SaveOnboardingRequest GetExamples()
    {
        return new SaveOnboardingRequest(
            Step: OnboardingStep.InvestorCategory,
            InvestorCategoryPayload: new InvestorCategoryPayload(InvestorCategory.Retail),
            InvestmentProfilePayload: null,
            KycPayload: null
        );
    }
}
