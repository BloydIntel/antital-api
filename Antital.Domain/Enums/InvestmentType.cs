using System.ComponentModel;

namespace Antital.Domain.Enums;

/// <summary>
/// Types of investments (Sophisticated Investor profile - multi-select).
/// </summary>
public enum InvestmentType
{
    [Description("Equities")]
    Equities = 0,

    [Description("Fixed income")]
    FixedIncome = 1,

    [Description("Private equity / venture capital")]
    PrivateEquityVentureCapital = 2,

    [Description("High-risk or speculative investments")]
    HighRiskSpeculative = 3,

    [Description("Alternative investments")]
    AlternativeInvestments = 4
}
