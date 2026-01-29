using Antital.Application.DTOs.Authentication;
using Antital.Domain.Enums;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Authentication.Login;

public class AuthResponseExample : IExamplesProvider<AuthResponseDto>
{
    public AuthResponseDto GetExamples()
    {
        return new AuthResponseDto
        {
            Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            RefreshToken = null,
            UserId = 1,
            Email = "user@example.com",
            UserType = UserTypeEnum.IndividualInvestor,
            IsEmailVerified = true
        };
    }
}
