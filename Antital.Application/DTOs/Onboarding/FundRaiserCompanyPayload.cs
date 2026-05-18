namespace Antital.Application.DTOs.Onboarding;

public record FundRaiserCompanyPayload(
    string CompanyLegalName,
    string? TradingBrandName,
    string RegistrationType,
    string RegistrationNumber,
    string CompanyLoginEmail,
    DateTime? DateOfRegistration,
    string? CompanyWebsite,
    string BusinessAddress,
    string RegisteredAddress,
    string CompanyEmail,
    string CompanyPhone
);
