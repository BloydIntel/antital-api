using FluentValidation;

namespace Antital.Application.Features.Authentication.ResendVerificationEmail;

public class ResendVerificationEmailCommandValidator : AbstractValidator<ResendVerificationEmailCommand>
{
    public ResendVerificationEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.");
    }
}
