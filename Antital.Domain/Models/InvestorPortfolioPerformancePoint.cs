using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class InvestorPortfolioPerformancePoint : TrackableEntity
{
    public int UserId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Value { get; set; }

    public virtual User User { get; set; } = null!;
}
