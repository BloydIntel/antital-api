using FluentValidation;

namespace Antital.Application.Features.Onboarding.InitializeApplicationFeePayment;

public class InitializeApplicationFeePaymentCommandValidator
    : AbstractValidator<InitializeApplicationFeePaymentCommand>
{
    public InitializeApplicationFeePaymentCommandValidator()
    {
        RuleFor(x => x.Channel).IsInEnum();
    }
}
