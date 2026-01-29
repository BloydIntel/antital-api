using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Authentication.ForgotPassword;

public class ForgotPasswordCommandExample : IExamplesProvider<ForgotPasswordCommand>
{
    public ForgotPasswordCommand GetExamples()
    {
        return new ForgotPasswordCommand("user@example.com");
    }
}
