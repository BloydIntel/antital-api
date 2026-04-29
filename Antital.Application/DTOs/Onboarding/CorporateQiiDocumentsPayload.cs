namespace Antital.Application.DTOs.Onboarding;

public record CorporateQiiDocumentsPayload(
    string? RecentStatusReportDocumentPathOrKey,
    string? QiiLicenseEvidenceDocumentPathOrKey,
    string? BoardResolutionDocumentPathOrKey
);
