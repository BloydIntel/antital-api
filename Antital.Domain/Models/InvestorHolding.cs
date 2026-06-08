using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class InvestorHolding : TrackableEntity
{
    public int UserId { get; set; }
    public int OfferingId { get; set; }
    public decimal InvestedAmount { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Returns { get; set; }
    public int UnitHolding { get; set; }
    public DateTime InvestedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual InvestmentOffering Offering { get; set; } = null!;
}
