using BuildingBlocks.Application.Methods;
using FluentValidation;

namespace Antital.Application.Features.Authentication.SignUp;

public class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        // Email validation
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Must(ValidationHelper.IsValidEmail).WithMessage("Email must be in a valid format.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");

        // Password validation
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Must(BeValidPassword).WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");

        // ConfirmPassword validation
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.Password).WithMessage("Password and confirm password must match.");

        // FirstName validation
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .Must(ValidationHelper.IsValidString).WithMessage("First name is required.")
            .MinimumLength(2).WithMessage("First name must be at least 2 characters long.")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

        // LastName validation
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .Must(ValidationHelper.IsValidString).WithMessage("Last name is required.")
            .MinimumLength(2).WithMessage("Last name must be at least 2 characters long.")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

        // PhoneNumber validation
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Must(ValidationHelper.IsValidString).WithMessage("Phone number is required.");

        // DateOfBirth validation - must be 18+ years old
        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Must(BeAtLeast18YearsOld).WithMessage("You must be at least 18 years old to register.");

        // Nationality validation
        RuleFor(x => x.Nationality)
            .NotEmpty().WithMessage("Nationality is required.")
            .MaximumLength(100).WithMessage("Nationality must not exceed 100 characters.");

        // CountryOfResidence validation
        RuleFor(x => x.CountryOfResidence)
            .NotEmpty().WithMessage("Country of residence is required.")
            .MaximumLength(100).WithMessage("Country of residence must not exceed 100 characters.");

        // StateOfResidence validation
        RuleFor(x => x.StateOfResidence)
            .NotEmpty().WithMessage("State of residence is required.")
            .MaximumLength(100).WithMessage("State of residence must not exceed 100 characters.");

        // ResidentialAddress validation
        RuleFor(x => x.ResidentialAddress)
            .NotEmpty().WithMessage("Residential address is required.")
            .MinimumLength(10).WithMessage("Residential address must be at least 10 characters long.")
            .MaximumLength(500).WithMessage("Residential address must not exceed 500 characters.");

        // HasAgreedToTerms validation
        RuleFor(x => x.HasAgreedToTerms)
            .Must(x => x == true).WithMessage("You must agree to the terms and conditions to register.");
    }

    private static bool BeValidPassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }

    private static bool BeAtLeast18YearsOld(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        
        // Adjust age if birthday hasn't occurred this year
        if (dateOfBirth.Date > today.AddYears(-age))
            age--;

        return age >= 18;
    }
}
