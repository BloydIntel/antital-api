using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class InvestorWallet : TrackableEntity
{
    public int UserId { get; set; }
    public decimal AvailableBalance { get; set; }
    public string Currency { get; set; } = "NGN";

    public virtual User User { get; set; } = null!;
}
