using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Authentication.Login;

public class TokenExample : IExamplesProvider<string>
{
    public string GetExamples()
    {
        return "SampleToken";
    }
}
