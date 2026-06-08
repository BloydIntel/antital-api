using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class InvestorWatchlistItem : TrackableEntity
{
    public int UserId { get; set; }
    public int OfferingId { get; set; }
    public decimal ChangePercent { get; set; }
    public DateTime AddedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual InvestmentOffering Offering { get; set; } = null!;
}
