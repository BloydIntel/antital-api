using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Application.Features.Fundraisers.Documents;

public static class FundraiserDocumentsMappers
{
    public static FundraiserDocumentsResponse Empty() =>
        new(null, null, []);

    public static FundraiserDocumentDto ToDto(OfferingDocument document) =>
        new(
            document.Id,
            document.Title,
            ToCategoryLabel(document.Category),
            ToStatusLabel(document.ReviewStatus),
            document.FileUrl,
            document.FileSizeBytes,
            document.ContentType,
            document.UpdatedAt ?? document.CreatedAt);

    public static string ToCategoryLabel(DocumentCategory category) =>
        category switch
        {
            DocumentCategory.Core => "Core",
            DocumentCategory.Financial => "Financial",
            DocumentCategory.Analytics => "Analytics",
            DocumentCategory.Regulatory => "Regulatory",
            _ => "Core",
        };

    public static string ToStatusLabel(DocumentReviewStatus status) =>
        status switch
        {
            DocumentReviewStatus.Approved => "Approved",
            DocumentReviewStatus.PendingApproval => "Pending Approval",
            DocumentReviewStatus.RevisionRequested => "Revision Requested",
            _ => "Pending Approval",
        };

    public static bool TryParseCategory(string? value, out DocumentCategory category)
    {
        category = DocumentCategory.Core;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().Replace(" ", "", StringComparison.Ordinal);
        if (Enum.TryParse(normalized, ignoreCase: true, out DocumentCategory parsed))
        {
            category = parsed;
            return true;
        }

        return false;
    }

    public static DocumentType ToLegacyDocumentType(DocumentCategory category) =>
        category switch
        {
            DocumentCategory.Core => DocumentType.Prospectus,
            DocumentCategory.Financial => DocumentType.FinancialModel,
            _ => DocumentType.Other,
        };
}
