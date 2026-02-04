using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Onboarding.SubmitOnboarding;

public class SubmitOnboardingCommandHandler(
    IOnboardingUserAccess userAccess,
    IAntitalCurrentUser currentUser,
    IAntitalUnitOfWork unitOfWork,
    IUserOnboardingRepository userOnboardingRepository,
    IUserInvestmentProfileRepository userInvestmentProfileRepository
) : ICommandQueryHandler<SubmitOnboardingCommand>
{
    public async Task<Result> Handle(SubmitOnboardingCommand request, CancellationToken cancellationToken)
    {
        var (userId, _) = await userAccess.RequireVerifiedUserAsync(cancellationToken);

        var onboarding = await userOnboardingRepository.GetByUserIdAsync(userId, cancellationToken);
        if (onboarding == null)
            throw new BadRequestException("Onboarding not started. Complete at least the investor category step.", new Dictionary<string, string[]>());

        if (onboarding.Status == OnboardingStatus.Submitted)
            throw new BadRequestException("Onboarding has already been submitted.", new Dictionary<string, string[]>());

        // Minimum completeness for individual: at least investor category (and ideally profile)
        var profile = await userInvestmentProfileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
            throw new BadRequestException("Complete the investor category and investment profile before submitting.", new Dictionary<string, string[]>());

        var updatedBy = !string.IsNullOrEmpty(currentUser.UserName) ? currentUser.UserName : currentUser.IPAddress ?? "System";
        onboarding.SubmittedAt = DateTime.UtcNow;
        onboarding.Status = OnboardingStatus.Submitted;
        onboarding.CurrentStep = OnboardingStep.Submitted;
        onboarding.Updated(updatedBy);

        await userOnboardingRepository.UpdateAsync(onboarding, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result();
        result.OK();
        return result;
    }
}
