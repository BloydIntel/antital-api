using BuildingBlocks.Application.Methods;
using FluentValidation;

namespace Antital.Application.Features.Authentication.DeleteUnverifiedUser;

public class DeleteUnverifiedUserCommandValidator : AbstractValidator<DeleteUnverifiedUserCommand>
{
    public DeleteUnverifiedUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Must(ValidationHelper.IsValidEmail).WithMessage("Email must be in a valid format.");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("OTP is required.")
            .Matches(@"^\d{6}$").WithMessage("OTP must be a 6-digit code.");
    }
}
