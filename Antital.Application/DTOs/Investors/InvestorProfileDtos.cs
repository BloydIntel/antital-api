using Antital.Domain.Enums;

namespace Antital.Application.DTOs.Investors;

public record InvestorProfileResponse(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    string? PreferredName,
    string PhoneNumber,
    string ResidentialAddress,
    string StateOfResidence,
    string CountryOfResidence,
    DateTime DateOfBirth,
    string Nationality,
    UserTypeEnum UserType,
    bool IsEmailVerified);

public record UpdateInvestorProfileRequest(
    string FirstName,
    string LastName,
    string? PreferredName,
    string PhoneNumber,
    string ResidentialAddress,
    string StateOfResidence,
    string CountryOfResidence);
