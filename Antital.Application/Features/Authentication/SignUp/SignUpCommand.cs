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
    bool HasAgreedToTerms,
    string UserType = "IndividualInvestor",
    string? CorporateInvestorCategory = null,
    string? CompanyLegalName = null,
    string? TradingBrandName = null,
    string? RegistrationType = null,
    string? RegistrationNumber = null,
    string? CompanyLoginEmail = null,
    DateTime? DateOfRegistration = null,
    string? CompanyWebsite = null,
    string? BusinessAddress = null,
    string? RegisteredAddress = null,
    string? CompanyEmail = null,
    string? CompanyPhone = null,
    string? RepresentativeFullName = null,
    string? RepresentativeJobTitle = null,
    string? RepresentativePhoneNumber = null,
    DateTime? RepresentativeDateOfBirth = null,
    string? RepresentativeEmail = null,
    string? RepresentativeNationality = null,
    string? RepresentativeCountryOfResidence = null,
    string? RepresentativeAddress = null
) : ICommandQuery<AuthResponseDto>;
