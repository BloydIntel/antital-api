using Antital.Application.Features.Onboarding.SubmitOnboarding;
using FluentValidation.TestHelper;
using Xunit;

namespace Antital.Test.Application.Features.Onboarding;

public class SubmitOnboardingCommandValidatorTests
{
    private readonly SubmitOnboardingCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.TestValidate(new SubmitOnboardingCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }
}
