using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class OfferingContentBlock : TrackableEntity
{
    public int OfferingId { get; set; }
    public ContentBlockType BlockType { get; set; }
    public string? Key { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public int SortOrder { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
    public ICollection<ContentBlockItem> Items { get; set; } = [];
}
