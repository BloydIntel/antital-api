using Antital.Application.Features.Authentication.ForgotPassword;
using FluentValidation.TestHelper;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.ForgotPassword;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Email_Invalid()
    {
        var result = _validator.TestValidate(new ForgotPasswordCommand("not-an-email"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Pass_When_Email_Valid()
    {
        var result = _validator.TestValidate(new ForgotPasswordCommand("user@example.com"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
