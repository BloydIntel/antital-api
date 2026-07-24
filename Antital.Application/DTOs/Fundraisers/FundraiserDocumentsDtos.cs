namespace Antital.Application.DTOs.Fundraisers;

public record FundraiserDocumentDto(
    int Id,
    string Title,
    string Category,
    string Status,
    string FileUrl,
    long FileSizeBytes,
    string ContentType,
    DateTime LastUpdatedAt);

public record FundraiserDocumentsResponse(
    int? OfferingId,
    string? OfferingSlug,
    IReadOnlyList<FundraiserDocumentDto> Items);
