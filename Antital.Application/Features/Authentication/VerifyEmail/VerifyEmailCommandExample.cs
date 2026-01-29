using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Authentication.VerifyEmail;

public class VerifyEmailCommandExample : IExamplesProvider<VerifyEmailCommand>
{
    public VerifyEmailCommand GetExamples()
    {
        return new VerifyEmailCommand(
            Email: "user@example.com",
            Token: "verification_token_12345"
        );
    }
}
