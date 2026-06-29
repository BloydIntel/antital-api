using FluentValidation;

namespace Antital.Application.Features.Investments.InitializeInvestmentPayment;

public class InitializeInvestmentPaymentCommandValidator : AbstractValidator<InitializeInvestmentPaymentCommand>
{
    public InitializeInvestmentPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).GreaterThan(0);
        RuleFor(x => x.Channel).IsInEnum();
    }
}
