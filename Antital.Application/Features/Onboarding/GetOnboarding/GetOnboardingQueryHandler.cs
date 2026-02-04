using Antital.Application.DTOs.Onboarding;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Onboarding.GetOnboarding;

public class GetOnboardingQueryHandler(
    IOnboardingUserAccess userAccess,
    IUserOnboardingRepository userOnboardingRepository,
    IUserInvestmentProfileRepository userInvestmentProfileRepository,
    IUserKycRepository userKycRepository
) : ICommandQueryHandler<GetOnboardingQuery, OnboardingResponse>
{
    public async Task<Result<OnboardingResponse>> Handle(GetOnboardingQuery request, CancellationToken cancellationToken)
    {
        var (userId, user) = await userAccess.RequireVerifiedUserAsync(cancellationToken);

        var onboarding = await userOnboardingRepository.GetByUserIdAsync(userId, cancellationToken);
        var profile = await userInvestmentProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        var kyc = await userKycRepository.GetByUserIdAsync(userId, cancellationToken);

        var currentStep = onboarding?.CurrentStep ?? OnboardingStep.InvestorCategory;
        var status = onboarding?.Status ?? OnboardingStatus.Draft;
        var submittedAt = onboarding?.SubmittedAt;

        var personalInfo = new OnboardingPersonalInfoDto(
            $"{user.FirstName} {user.LastName}".Trim(),
            user.Email,
            user.PreferredName,
            user.PhoneNumber,
            user.DateOfBirth
        );

        var locationInfo = new OnboardingLocationInfoDto(
            user.Nationality,
            user.CountryOfResidence,
            user.StateOfResidence,
            user.ResidentialAddress
        );

        var investorProfile = profile.ToDto();
        var kycDto = kyc.ToDto();

        var response = new OnboardingResponse(
            currentStep,
            status,
            submittedAt,
            personalInfo,
            locationInfo,
            investorProfile,
            kycDto
        );

        var result = new Result<OnboardingResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
