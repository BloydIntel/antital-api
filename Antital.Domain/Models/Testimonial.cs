using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class Testimonial : TrackableEntity
{
    public int OfferingId { get; set; }
    public string Quote { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorTitle { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
