using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class OfferingDocument : TrackableEntity
{
    public int OfferingId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public int? PageCount { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
