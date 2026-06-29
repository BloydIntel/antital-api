using System.ComponentModel;

namespace Antital.Domain.Enums;

public enum PaymentTransactionStatus
{
    [Description("Pending")]
    Pending = 0,

    [Description("Success")]
    Success = 1,

    [Description("Failed")]
    Failed = 2
}
