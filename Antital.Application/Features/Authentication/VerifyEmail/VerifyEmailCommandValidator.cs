using BuildingBlocks.Application.Methods;
using FluentValidation;

namespace Antital.Application.Features.Authentication.VerifyEmail;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        // Email validation
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Must(ValidationHelper.IsValidEmail).WithMessage("Email must be in a valid format.");

        // Token validation
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required.");
    }
}
