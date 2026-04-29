namespace Antital.Application.DTOs.Onboarding;

public record CorporateCompanyPayload(
    string CompanyLegalName,
    string TradingBrandName,
    string RegistrationType,
    string RegistrationNumber,
    string CompanyLoginEmail
);
