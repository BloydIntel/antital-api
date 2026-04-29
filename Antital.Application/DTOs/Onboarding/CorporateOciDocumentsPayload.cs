namespace Antital.Application.DTOs.Onboarding;

public record CorporateOciDocumentsPayload(
    string? IncorporationCertificateDocumentPathOrKey,
    string? RecentStatusReportDocumentPathOrKey,
    string? BoardResolutionDocumentPathOrKey
);
