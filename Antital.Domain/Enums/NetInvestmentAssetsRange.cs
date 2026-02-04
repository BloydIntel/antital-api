using System.ComponentModel;

namespace Antital.Domain.Enums;

/// <summary>
/// HNI net investment assets range (SEC Nigeria).
/// </summary>
public enum NetInvestmentAssetsRange
{
    [Description("₦100 million – ₦250 million")]
    Range100_250M = 0,

    [Description("₦250 million – ₦500 million")]
    Range250_500M = 1,

    [Description("Above ₦500 million")]
    Above500M = 2
}
