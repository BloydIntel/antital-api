using Antital.Application.Features.Authentication.SignUp;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.SignUp;

public class SignUpCommandValidatorTests
{
    private readonly SignUpCommandValidator _validator;

    public SignUpCommandValidatorTests()
    {
        _validator = new SignUpCommandValidator();
    }

    private SignUpCommand CreateValidCommand()
    {
        return new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PreferredName: "Johnny",
            PhoneNumber: "+1234567890",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street, Lagos, Nigeria",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );
    }

    #region Email Validation Tests

    [Fact]
    public void Validate_EmailIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Email = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Fact]
    public void Validate_EmailIsNull_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Email = null! };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_EmailInvalidFormat_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Email = "invalid-email" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must be in a valid format.");
    }

    [Fact]
    public void Validate_EmailExceeds255Characters_ShouldHaveValidationError()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@example.com"; // 263 characters
        var command = CreateValidCommand() with { Email = longEmail };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must not exceed 255 characters.");
    }

    [Fact]
    public void Validate_EmailValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Password Validation Tests

    [Fact]
    public void Validate_PasswordIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Password = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Fact]
    public void Validate_PasswordLessThan8Characters_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Password = "Short1!" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters long.");
    }

    [Fact]
    public void Validate_PasswordMissingUppercase_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Password = "lowercase123!" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
    }

    [Fact]
    public void Validate_PasswordMissingLowercase_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Password = "UPPERCASE123!" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
    }

    [Fact]
    public void Validate_PasswordMissingNumber_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Password = "NoNumberPass!" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
    }

    [Fact]
    public void Validate_PasswordMissingSpecialCharacter_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Password = "NoSpecial123" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");
    }

    [Fact]
    public void Validate_PasswordValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    #endregion

    #region ConfirmPassword Validation Tests

    [Fact]
    public void Validate_ConfirmPasswordIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { ConfirmPassword = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Confirm password is required.");
    }

    [Fact]
    public void Validate_ConfirmPasswordDoesNotMatch_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { ConfirmPassword = "DifferentPass123!" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Password and confirm password must match.");
    }

    [Fact]
    public void Validate_ConfirmPasswordMatches_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    #endregion

    #region FirstName Validation Tests

    [Fact]
    public void Validate_FirstNameIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { FirstName = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_FirstNameLessThan2Characters_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { FirstName = "J" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name must be at least 2 characters long.");
    }

    [Fact]
    public void Validate_FirstNameExceeds50Characters_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { FirstName = new string('A', 51) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name must not exceed 50 characters.");
    }

    [Fact]
    public void Validate_FirstNameValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    #endregion

    #region LastName Validation Tests

    [Fact]
    public void Validate_LastNameIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { LastName = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_LastNameLessThan2Characters_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { LastName = "D" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name must be at least 2 characters long.");
    }

    [Fact]
    public void Validate_LastNameExceeds50Characters_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { LastName = new string('D', 51) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name must not exceed 50 characters.");
    }

    [Fact]
    public void Validate_LastNameValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    #endregion

    #region PhoneNumber Validation Tests

    [Fact]
    public void Validate_PhoneNumberIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { PhoneNumber = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_PhoneNumberValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    #endregion

    #region DateOfBirth Validation Tests

    [Fact]
    public void Validate_DateOfBirthIsDefault_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { DateOfBirth = default(DateTime) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void Validate_DateOfBirthLessThan18YearsOld_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { DateOfBirth = DateTime.Today.AddYears(-17) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
            .WithErrorMessage("You must be at least 18 years old to register.");
    }

    [Fact]
    public void Validate_DateOfBirthExactly18YearsOld_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { DateOfBirth = DateTime.Today.AddYears(-18) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public void Validate_DateOfBirthMoreThan18YearsOld_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);
    }

    #endregion

    #region Location Fields Validation Tests

    [Fact]
    public void Validate_NationalityIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Nationality = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Nationality);
    }

    [Fact]
    public void Validate_NationalityExceeds100Characters_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Nationality = new string('N', 101) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Nationality)
            .WithErrorMessage("Nationality must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_CountryOfResidenceIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { CountryOfResidence = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CountryOfResidence);
    }

    [Fact]
    public void Validate_CountryOfResidenceExceeds100Characters_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { CountryOfResidence = new string('C', 101) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CountryOfResidence)
            .WithErrorMessage("Country of residence must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_StateOfResidenceIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { StateOfResidence = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StateOfResidence);
    }

    [Fact]
    public void Validate_StateOfResidenceExceeds100Characters_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { StateOfResidence = new string('S', 101) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StateOfResidence)
            .WithErrorMessage("State of residence must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_ResidentialAddressIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { ResidentialAddress = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResidentialAddress);
    }

    [Fact]
    public void Validate_ResidentialAddressLessThan10Characters_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { ResidentialAddress = "Short" };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResidentialAddress)
            .WithErrorMessage("Residential address must be at least 10 characters long.");
    }

    [Fact]
    public void Validate_ResidentialAddressExceeds500Characters_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { ResidentialAddress = new string('A', 501) };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResidentialAddress)
            .WithErrorMessage("Residential address must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_ResidentialAddressValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ResidentialAddress);
    }

    #endregion

    #region HasAgreedToTerms Validation Tests

    [Fact]
    public void Validate_HasAgreedToTermsIsFalse_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { HasAgreedToTerms = false };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HasAgreedToTerms)
            .WithErrorMessage("You must agree to the terms and conditions to register.");
    }

    [Fact]
    public void Validate_HasAgreedToTermsIsTrue_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.HasAgreedToTerms);
    }

    #endregion

    [Fact]
    public void Validate_AllFieldsValid_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
