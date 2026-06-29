using System.ComponentModel;

namespace Antital.Domain.Enums;

/// <summary>Checkout payment channel selected by the investor (maps to Paystack channels).</summary>
public enum PaymentChannel
{
    [Description("Card")]
    Card = 0,

    [Description("Bank Transfer")]
    Transfer = 1,

    [Description("Opay")]
    Opay = 2
}
