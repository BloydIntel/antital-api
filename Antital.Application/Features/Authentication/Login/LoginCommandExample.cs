using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Authentication.Login;

public class LoginCommandExample : IExamplesProvider<LoginCommand>
{
    public LoginCommand GetExamples()
    {
        return new LoginCommand(
            Email: "user@example.com",
            Password: "SecurePass123!"
        );
    }
}
