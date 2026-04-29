namespace Antital.Application.DTOs.Onboarding;

public record CorporateAddressPayload(
    DateTime DateOfRegistration,
    string? CompanyWebsite,
    string BusinessAddress,
    string RegisteredAddress,
    string CompanyEmail,
    string CompanyPhone
);
