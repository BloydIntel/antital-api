using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class OfferingFunding : TrackableEntity
{
    public int OfferingId { get; set; }
    public decimal RaisedAmount { get; set; }
    public decimal FundingGoal { get; set; }
    public decimal? MinimumRaise { get; set; }
    public int InvestorCount { get; set; }
    public decimal SharePrice { get; set; }
    public decimal? TargetRating { get; set; }
    public decimal MinInvestment { get; set; }
    public decimal MaxInvestment { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
