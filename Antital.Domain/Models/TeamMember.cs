using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class TeamMember : TrackableEntity
{
    public int OfferingId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
