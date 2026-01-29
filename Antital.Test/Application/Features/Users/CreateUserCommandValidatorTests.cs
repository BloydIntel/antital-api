using Antital.Application.Features.Users.CreateUser;
using Antital.Domain.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace Antital.Test.Application.Features.Users;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void InvalidEmail_Fails()
    {
        var cmd = new CreateUserCommand("bad", "Password123!", "A", "B", null, null, UserTypeEnum.IndividualInvestor);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void ShortPassword_Fails()
    {
        var cmd = new CreateUserCommand("a@b.com", "short", "A", "B", null, null, UserTypeEnum.IndividualInvestor);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void MissingNames_Fail()
    {
        var cmd = new CreateUserCommand("a@b.com", "Password123!", "", "", null, null, UserTypeEnum.IndividualInvestor);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.FirstName);
        result.ShouldHaveValidationErrorFor(c => c.LastName);
    }
}
