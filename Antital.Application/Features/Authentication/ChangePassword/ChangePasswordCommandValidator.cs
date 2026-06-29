using FluentValidation;

namespace Antital.Application.Features.Authentication.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty()
            .WithMessage("Current password is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.")
            .Must(BeValidPassword)
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("Passwords must match");

        RuleFor(x => x)
            .Must(x => !string.Equals(x.CurrentPassword, x.NewPassword, StringComparison.Ordinal))
            .WithMessage("New password must be different from current password.");
    }

    private static bool BeValidPassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}
