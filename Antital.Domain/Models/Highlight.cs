using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class Highlight : TrackableEntity
{
    public int OfferingId { get; set; }
    public HighlightKind Kind { get; set; }
    public string? Headline { get; set; }
    public string Body { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
