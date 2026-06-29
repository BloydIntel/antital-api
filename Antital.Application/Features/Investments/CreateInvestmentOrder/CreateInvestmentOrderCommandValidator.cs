using FluentValidation;

namespace Antital.Application.Features.Investments.CreateInvestmentOrder;

public class CreateInvestmentOrderCommandValidator : AbstractValidator<CreateInvestmentOrderCommand>
{
    public CreateInvestmentOrderCommandValidator()
    {
        RuleFor(x => x.OfferingId).GreaterThan(0);
        RuleFor(x => x.Units).GreaterThan(0);
    }
}
