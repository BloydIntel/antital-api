using System.ComponentModel;

namespace Antital.Domain.Enums;

/// <summary>
/// Nigerian SEC investor categorization (Retail, Sophisticated, High Net-Worth).
/// </summary>
public enum InvestorCategory
{
    [Description("Retail Investor")]
    Retail = 0,

    [Description("Sophisticated Investor")]
    Sophisticated = 1,

    [Description("High Net-Worth Investor (HNI)")]
    HighNetWorth = 2
}
