using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class InvestorPaymentMethod : TrackableEntity
{
    public int UserId { get; set; }
    public InvestorPaymentMethodType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string Last4 { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsVerified { get; set; } = true;

    public virtual User User { get; set; } = null!;
}
