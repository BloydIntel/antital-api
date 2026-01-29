using BuildingBlocks.Application.Methods;
using FluentValidation;

namespace Antital.Application.Features.Authentication.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        // Email validation
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Must(ValidationHelper.IsValidEmail).WithMessage("Email must be in a valid format.");

        // Password validation
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
