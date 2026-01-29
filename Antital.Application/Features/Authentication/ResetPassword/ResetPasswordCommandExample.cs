using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Authentication.ResetPassword;

public class ResetPasswordCommandExample : IExamplesProvider<ResetPasswordCommand>
{
    public ResetPasswordCommand GetExamples()
    {
        return new ResetPasswordCommand(
            Token: "opaque-reset-token",
            NewPassword: "NewStrongP@ss1",
            ConfirmPassword: "NewStrongP@ss1"
        );
    }
}
