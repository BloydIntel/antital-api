using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class OfferingDocument : TrackableEntity
{
    public int OfferingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public DocumentCategory Category { get; set; } = DocumentCategory.Core;
    public DocumentReviewStatus ReviewStatus { get; set; } = DocumentReviewStatus.PendingApproval;
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
    public string? CloudinaryPublicId { get; set; }
    public int? PageCount { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
