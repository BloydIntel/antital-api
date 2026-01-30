using Antital.Application.Features.Users.UpdateUser;
using Antital.Domain.Enums;
using FluentValidation.TestHelper;
using Xunit;

namespace Antital.Test.Application.Features.Users;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void InvalidId_Fails()
    {
        var cmd = new UpdateUserCommand(0, "A", "B", null, null, UserTypeEnum.IndividualInvestor, null, null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void MissingNames_Fails()
    {
        var cmd = new UpdateUserCommand(1, "", "", null, null, UserTypeEnum.IndividualInvestor, null, null);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.FirstName);
        result.ShouldHaveValidationErrorFor(c => c.LastName);
    }

    [Fact]
    public void ShortPassword_Fails()
    {
        var cmd = new UpdateUserCommand(1, "A", "B", null, null, UserTypeEnum.IndividualInvestor, null, "short");
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(c => c.Password);
    }
}
