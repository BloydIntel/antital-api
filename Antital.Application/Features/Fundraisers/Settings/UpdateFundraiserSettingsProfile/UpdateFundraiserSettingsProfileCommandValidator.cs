using BuildingBlocks.Application.Methods;
using FluentValidation;

namespace Antital.Application.Features.Fundraisers.Settings.UpdateFundraiserSettingsProfile;

public class UpdateFundraiserSettingsProfileCommandValidator
    : AbstractValidator<UpdateFundraiserSettingsProfileCommand>
{
    public UpdateFundraiserSettingsProfileCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .Must(ValidationHelper.IsValidString).WithMessage("Company name is required.")
            .MinimumLength(2).WithMessage("Company name must be at least 2 characters long.")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.");

        RuleFor(x => x.RegistrationNumber)
            .MaximumLength(100).WithMessage("Registration number must not exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.RegistrationNumber));

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio must not exceed 2000 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Bio));

        RuleFor(x => x.Website)
            .MaximumLength(300).WithMessage("Website must not exceed 300 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.PublicEmail)
            .EmailAddress().WithMessage("Public email must be a valid email address.")
            .MaximumLength(255).WithMessage("Public email must not exceed 255 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.PublicEmail));

        RuleFor(x => x.Headquarters)
            .MaximumLength(500).WithMessage("Headquarters must not exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Headquarters));

        When(x => x.Contact is not null, () =>
        {
            RuleFor(x => x.Contact!.FullName)
                .MaximumLength(200).WithMessage("Contact full name must not exceed 200 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Contact!.FullName));

            RuleFor(x => x.Contact!.EmailAddress)
                .EmailAddress().WithMessage("Contact email must be a valid email address.")
                .MaximumLength(255).WithMessage("Contact email must not exceed 255 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Contact!.EmailAddress));

            RuleFor(x => x.Contact!.PhoneNumber)
                .MaximumLength(50).WithMessage("Contact phone number must not exceed 50 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Contact!.PhoneNumber));
        });
    }
}
