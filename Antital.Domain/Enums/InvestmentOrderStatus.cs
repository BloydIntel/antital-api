using System.ComponentModel;

namespace Antital.Domain.Enums;

public enum InvestmentOrderStatus
{
    [Description("Pending Payment")]
    PendingPayment = 0,

    [Description("Paid")]
    Paid = 1,

    [Description("Failed")]
    Failed = 2,

    [Description("Expired")]
    Expired = 3,

    [Description("Cancelled")]
    Cancelled = 4
}
