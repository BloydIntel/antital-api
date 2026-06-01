using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class UseOfProceedsItem : TrackableEntity
{
    public int OfferingId { get; set; }
    public decimal? AllocationPercent { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
