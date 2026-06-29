using BuildingBlocks.Application.Methods;
using FluentValidation;

namespace Antital.Application.Features.Investors.UpdateInvestorProfile;

public class UpdateInvestorProfileCommandValidator : AbstractValidator<UpdateInvestorProfileCommand>
{
    public UpdateInvestorProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .Must(ValidationHelper.IsValidString).WithMessage("First name is required.")
            .MinimumLength(2).WithMessage("First name must be at least 2 characters long.")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .Must(ValidationHelper.IsValidString).WithMessage("Last name is required.")
            .MinimumLength(2).WithMessage("Last name must be at least 2 characters long.")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

        RuleFor(x => x.PreferredName)
            .MaximumLength(50).WithMessage("Preferred name must not exceed 50 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.PreferredName));

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Must(ValidationHelper.IsValidString).WithMessage("Phone number is required.");

        RuleFor(x => x.ResidentialAddress)
            .NotEmpty().WithMessage("Residential address is required.")
            .MinimumLength(10).WithMessage("Residential address must be at least 10 characters long.")
            .MaximumLength(500).WithMessage("Residential address must not exceed 500 characters.");

        RuleFor(x => x.StateOfResidence)
            .NotEmpty().WithMessage("State of residence is required.")
            .MaximumLength(100).WithMessage("State of residence must not exceed 100 characters.");

        RuleFor(x => x.CountryOfResidence)
            .NotEmpty().WithMessage("Country of residence is required.")
            .MaximumLength(100).WithMessage("Country of residence must not exceed 100 characters.");
    }
}
