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

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required.");
    }
}
