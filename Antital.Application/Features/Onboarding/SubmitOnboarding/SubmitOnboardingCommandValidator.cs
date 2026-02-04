using FluentValidation;

namespace Antital.Application.Features.Onboarding.SubmitOnboarding;

public class SubmitOnboardingCommandValidator : AbstractValidator<SubmitOnboardingCommand>
{
    public SubmitOnboardingCommandValidator()
    {
        // No request body; validation is in the handler (user state, onboarding state).
    }
}
