using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

/// <summary>
/// Daily engagement counters for an offering (page views, unique visitors, shares).
/// </summary>
public class OfferingEngagementDaily : TrackableEntity
{
    public int OfferingId { get; set; }

    /// <summary>UTC calendar date (time component ignored).</summary>
    public DateTime Date { get; set; }

    public int PageViews { get; set; }
    public int UniqueVisitors { get; set; }
    public int Shares { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
