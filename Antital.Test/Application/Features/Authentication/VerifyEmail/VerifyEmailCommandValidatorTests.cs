using Antital.Application.Features.Authentication.VerifyEmail;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.VerifyEmail;

public class VerifyEmailCommandValidatorTests
{
    private readonly VerifyEmailCommandValidator _validator;

    public VerifyEmailCommandValidatorTests()
    {
        _validator = new VerifyEmailCommandValidator();
    }

    private VerifyEmailCommand CreateValidCommand()
    {
        return new VerifyEmailCommand(
            Email: "user@example.com",
            Token: "verification_token_12345"
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

    #region Token Validation Tests

    [Fact]
    public void Validate_TokenIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Token = string.Empty };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token)
            .WithErrorMessage("Verification token is required.");
    }

    [Fact]
    public void Validate_TokenIsNull_ShouldHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand() with { Token = null! };

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void Validate_TokenValid_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Token);
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
