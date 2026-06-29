using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class InvestmentOrder : TrackableEntity
{
    public int UserId { get; set; }
    public int OfferingId { get; set; }
    public int Units { get; set; }
    public decimal SharePrice { get; set; }
    public decimal Subtotal { get; set; }
    public decimal PlatformFeePercent { get; set; }
    public decimal PlatformFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "NGN";
    public InvestmentOrderStatus Status { get; set; }
    public PaymentChannel? PaymentChannel { get; set; }
    public string? PaystackReference { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public int? InvestorHoldingId { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual InvestmentOffering Offering { get; set; } = null!;
    public virtual InvestorHolding? InvestorHolding { get; set; }
    public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = [];
}
