using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class ContentBlockItem : TrackableEntity
{
    public int ContentBlockId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public virtual OfferingContentBlock ContentBlock { get; set; } = null!;
}
