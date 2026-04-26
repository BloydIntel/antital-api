using BuildingBlocks.Application.Methods;
using FluentValidation;

namespace Antital.Application.Features.Authentication.RequestUnverifiedUserOtp;

public class RequestUnverifiedUserOtpCommandValidator : AbstractValidator<RequestUnverifiedUserOtpCommand>
{
    public RequestUnverifiedUserOtpCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Must(ValidationHelper.IsValidEmail).WithMessage("Email must be in a valid format.");
    }
}
