using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class PaymentTransaction : TrackableEntity
{
    public int OrderId { get; set; }
    public string Provider { get; set; } = "Paystack";
    public string Reference { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public PaymentTransactionStatus Status { get; set; }
    public string? RawPayloadJson { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public virtual InvestmentOrder Order { get; set; } = null!;
}
