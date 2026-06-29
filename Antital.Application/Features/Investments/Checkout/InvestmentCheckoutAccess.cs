using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Antital.Application.Features.Onboarding;
using BuildingBlocks.Application.Exceptions;

namespace Antital.Application.Features.Investments.Checkout;

public interface IInvestmentCheckoutAccess
{
    Task<(int UserId, User User)> RequireEligibleInvestorAsync(CancellationToken cancellationToken = default);
}

public class InvestmentCheckoutAccess(
    IOnboardingUserAccess onboardingUserAccess,
    IUserOnboardingRepository onboardingRepository
) : IInvestmentCheckoutAccess
{
    public async Task<(int UserId, User User)> RequireEligibleInvestorAsync(CancellationToken cancellationToken = default)
    {
        var (userId, user) = await onboardingUserAccess.RequireVerifiedUserAsync(cancellationToken);

        var onboarding = await onboardingRepository.GetByUserIdAsync(userId, cancellationToken);
        if (onboarding == null || !IsOnboardingCompleteEnoughToInvest(onboarding.Status))
        {
            throw new ForbiddenException("Complete and submit onboarding before investing.");
        }

        return (userId, user);
    }

    private static bool IsOnboardingCompleteEnoughToInvest(OnboardingStatus status) =>
        status is OnboardingStatus.Submitted
            or OnboardingStatus.UnderReview
            or OnboardingStatus.Activated;
}
