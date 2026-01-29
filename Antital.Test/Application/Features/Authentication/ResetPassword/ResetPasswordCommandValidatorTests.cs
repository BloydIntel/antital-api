using Antital.Application.Features.Authentication.ResetPassword;
using FluentValidation.TestHelper;
using Xunit;

namespace Antital.Test.Application.Features.Authentication.ResetPassword;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void Should_Fail_When_Token_Empty()
    {
        var result = _validator.TestValidate(new ResetPasswordCommand("", "Password1", "Password1"));
        result.ShouldHaveValidationErrorFor(x => x.Token);
    }

    [Fact]
    public void Should_Fail_When_Passwords_Mismatch()
    {
        var result = _validator.TestValidate(new ResetPasswordCommand("t", "Password1", "Other"));
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }

    [Fact]
    public void Should_Pass_When_Valid()
    {
        var result = _validator.TestValidate(new ResetPasswordCommand("t", "Password1", "Password1"));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
