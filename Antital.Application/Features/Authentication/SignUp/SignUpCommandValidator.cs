using BuildingBlocks.Application.Methods;
using FluentValidation;

namespace Antital.Application.Features.Authentication.SignUp;

public class SignUpCommandValidator : AbstractValidator<SignUpCommand>
{
    public SignUpCommandValidator()
    {
        RuleFor(x => x.UserType)
            .NotEmpty().WithMessage("User type is required.")
            .Must(BeSupportedUserType).WithMessage("User type must be IndividualInvestor, CorporateInvestor, or Fundraiser.");

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

        // Personal identity/contact fields are required for individual/corporate signup.
        // Fundraiser captures representative details later in onboarding.
        When(x => !x.UserType.Equals("Fundraiser", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .Must(ValidationHelper.IsValidString).WithMessage("Phone number is required.");

            RuleFor(x => x.DateOfBirth)
                .NotNull().WithMessage("Date of birth is required.")
                .Must(d => d.HasValue && d.Value != default).WithMessage("Date of birth is required.")
                .Must(d => d.HasValue && BeAtLeast18YearsOld(d.Value)).WithMessage("You must be at least 18 years old to register.");

            RuleFor(x => x.Nationality)
                .NotEmpty().WithMessage("Nationality is required.")
                .MaximumLength(100).WithMessage("Nationality must not exceed 100 characters.");

            RuleFor(x => x.CountryOfResidence)
                .NotEmpty().WithMessage("Country of residence is required.")
                .MaximumLength(100).WithMessage("Country of residence must not exceed 100 characters.");

            RuleFor(x => x.StateOfResidence)
                .NotEmpty().WithMessage("State of residence is required.")
                .MaximumLength(100).WithMessage("State of residence must not exceed 100 characters.");

            RuleFor(x => x.ResidentialAddress)
                .NotEmpty().WithMessage("Residential address is required.")
                .MinimumLength(10).WithMessage("Residential address must be at least 10 characters long.")
                .MaximumLength(500).WithMessage("Residential address must not exceed 500 characters.");
        });

        // HasAgreedToTerms validation
        RuleFor(x => x.HasAgreedToTerms)
            .Must(x => x).WithMessage("You must agree to the terms and conditions to register.");

        RuleFor(x => x)
            .Must(NotProvideCorporateFieldsForNonCorporateUserType)
            .WithMessage("Company signup fields are only allowed when user type is CorporateInvestor or Fundraiser.");

        RuleFor(x => x)
            .Must(HaveValidCorporateCategoryIfCorporateUserType)
            .WithMessage("CorporateInvestor signup requires corporate investor category: QualifiedInstitutionalInvestor or OtherCorporateInvestor.");

        RuleFor(x => x)
            .Must(NotProvideCorporateCategoryForNonCorporateUserType)
            .WithMessage("Corporate investor category is only allowed when user type is CorporateInvestor.");
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
        if (dateOfBirth > today.AddYears(-age))
            age--;

        return age >= 18;
    }

    private static bool BeSupportedUserType(string? userType)
    {
        if (string.IsNullOrWhiteSpace(userType))
            return false;

        return userType.Equals("IndividualInvestor", StringComparison.OrdinalIgnoreCase)
            || userType.Equals("CorporateInvestor", StringComparison.OrdinalIgnoreCase)
            || userType.Equals("Fundraiser", StringComparison.OrdinalIgnoreCase);
    }

    private static bool NotProvideCorporateFieldsForNonCorporateUserType(SignUpCommand request)
    {
        if (request.UserType.Equals("CorporateInvestor", StringComparison.OrdinalIgnoreCase)
            || request.UserType.Equals("Fundraiser", StringComparison.OrdinalIgnoreCase))
            return true;

        return string.IsNullOrWhiteSpace(request.CompanyLegalName)
            && string.IsNullOrWhiteSpace(request.TradingBrandName)
            && string.IsNullOrWhiteSpace(request.RegistrationType)
            && string.IsNullOrWhiteSpace(request.RegistrationNumber)
            && string.IsNullOrWhiteSpace(request.CompanyLoginEmail)
            && !request.DateOfRegistration.HasValue
            && string.IsNullOrWhiteSpace(request.CompanyWebsite)
            && string.IsNullOrWhiteSpace(request.BusinessAddress)
            && string.IsNullOrWhiteSpace(request.RegisteredAddress)
            && string.IsNullOrWhiteSpace(request.CompanyEmail)
            && string.IsNullOrWhiteSpace(request.CompanyPhone)
            && string.IsNullOrWhiteSpace(request.RepresentativeFullName)
            && string.IsNullOrWhiteSpace(request.RepresentativeJobTitle)
            && string.IsNullOrWhiteSpace(request.RepresentativePhoneNumber)
            && !request.RepresentativeDateOfBirth.HasValue
            && string.IsNullOrWhiteSpace(request.RepresentativeEmail)
            && string.IsNullOrWhiteSpace(request.RepresentativeNationality)
            && string.IsNullOrWhiteSpace(request.RepresentativeCountryOfResidence)
            && string.IsNullOrWhiteSpace(request.RepresentativeAddress);
    }

    private static bool HaveValidCorporateCategoryIfCorporateUserType(SignUpCommand request)
    {
        if (!request.UserType.Equals("CorporateInvestor", StringComparison.OrdinalIgnoreCase))
            return true;

        return request.CorporateInvestorCategory != null
            && (
                request.CorporateInvestorCategory.Equals("QualifiedInstitutionalInvestor", StringComparison.OrdinalIgnoreCase)
                || request.CorporateInvestorCategory.Equals("OtherCorporateInvestor", StringComparison.OrdinalIgnoreCase)
            );
    }

    private static bool NotProvideCorporateCategoryForNonCorporateUserType(SignUpCommand request)
    {
        if (request.UserType.Equals("CorporateInvestor", StringComparison.OrdinalIgnoreCase))
            return true;

        return string.IsNullOrWhiteSpace(request.CorporateInvestorCategory);
    }
}
