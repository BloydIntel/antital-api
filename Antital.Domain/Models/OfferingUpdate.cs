using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class OfferingUpdate : TrackableEntity
{
    public int OfferingId { get; set; }
    public OfferingUpdateStatus Status { get; set; } = OfferingUpdateStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int LikeCount { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
