using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class FinancialMetric : TrackableEntity
{
    public int OfferingId { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty;
    public int PeriodSortOrder { get; set; }
    public decimal? Value { get; set; }
    public FinancialMetricUnit Unit { get; set; }
    public string? CurrencyCode { get; set; }
    public FinancialValueType ValueType { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
