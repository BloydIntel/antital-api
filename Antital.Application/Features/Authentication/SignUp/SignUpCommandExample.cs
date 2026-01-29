using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Authentication.SignUp;

public class SignUpCommandExample : IExamplesProvider<SignUpCommand>
{
    public SignUpCommand GetExamples()
    {
        return new SignUpCommand(
            FirstName: "John",
            LastName: "Doe",
            Email: "john.doe@example.com",
            PreferredName: "Johnny",
            PhoneNumber: "+2348012345678",
            DateOfBirth: new DateTime(1990, 1, 1),
            Nationality: "Nigerian",
            CountryOfResidence: "Nigeria",
            StateOfResidence: "Lagos",
            ResidentialAddress: "123 Main Street, Victoria Island, Lagos",
            Password: "SecurePass123!",
            ConfirmPassword: "SecurePass123!",
            HasAgreedToTerms: true
        );
    }
}
