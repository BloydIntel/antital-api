using Antital.Application.Features.Investors.PaymentMethods;
using Antital.Application.Features.Investors.PaymentMethods.AddPaymentMethod;
using FluentValidation;

namespace Antital.Application.Features.Investors.PaymentMethods.AddPaymentMethod;

public class AddPaymentMethodCommandValidator : AbstractValidator<AddPaymentMethodCommand>
{
    public AddPaymentMethodCommandValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(type => PaymentMethodMapper.TryParseType(type, out _))
            .WithMessage("Type must be Bank or Card.");

        RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
        RuleFor(x => x.ProviderName).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Last4)
            .NotEmpty()
            .Matches(@"^\d{4}$")
            .WithMessage("Last4 must be exactly 4 digits.");
    }
}
