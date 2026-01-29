using Antital.Application.DTOs.Authentication;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Authentication.SignUp;

public record SignUpCommand(
    string FirstName,
    string LastName,
    string Email,
    string? PreferredName,
    string PhoneNumber,
    DateTime DateOfBirth,
    string Nationality,
    string CountryOfResidence,
    string StateOfResidence,
    string ResidentialAddress,
    string Password,
    string ConfirmPassword,
    bool HasAgreedToTerms
) : ICommandQuery<AuthResponseDto>;
