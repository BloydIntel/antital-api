using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class DealTerms : TrackableEntity
{
    public int OfferingId { get; set; }
    public long TotalSharesOffered { get; set; }
    public decimal PricePerShare { get; set; }
    public decimal MinimumInvestment { get; set; }
    public decimal MaximumInvestment { get; set; }
    public decimal MinimumThreshold { get; set; }
    public decimal FundingGoal { get; set; }
    public DateTime Deadline { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
