using Antital.Domain.Enums;

namespace Antital.Application.DTOs.Onboarding;

public record CorporateQiiProfilePayload(
    IReadOnlyCollection<QiiInstitutionType> InstitutionTypes,
    string? OtherInstitutionType,
    bool? HasValidQiiRegistrationOrLicense,
    bool? HasApprovedAlternativeInvestmentMandate,
    bool? ConfirmsSecNigeriaQiiCriteria
);
