using System.Globalization;

namespace Antital.Application.Features.Investors;

public readonly record struct DashboardPeriodRange(DateTime StartUtc, DateTime EndUtc);

public static class DashboardPeriodResolver
{
    private static readonly HashSet<string> RollingMonthPeriods = new(StringComparer.Ordinal)
    {
        "last-3-months",
        "last-6-months",
        "last-12-months",
    };

    public static bool TryResolve(string? period, out DashboardPeriodRange range, out string? errorMessage)
    {
        range = default;
        errorMessage = null;

        var normalized = string.IsNullOrWhiteSpace(period)
            ? "this-month"
            : period.Trim().ToLowerInvariant();

        var nowLagos = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LagosTimeZone);

        DateTime startLagos;
        DateTime endLagos;

        if (normalized == "this-month")
        {
            startLagos = new DateTime(nowLagos.Year, nowLagos.Month, 1);
            endLagos = startLagos.AddMonths(1);
        }
        else if (normalized == "last-month")
        {
            var lastMonth = nowLagos.AddMonths(-1);
            startLagos = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            endLagos = startLagos.AddMonths(1);
        }
        else if (RollingMonthPeriods.Contains(normalized))
        {
            var monthCount = normalized switch
            {
                "last-3-months" => 3,
                "last-6-months" => 6,
                "last-12-months" => 12,
                _ => 0,
            };

            startLagos = new DateTime(nowLagos.Year, nowLagos.Month, 1).AddMonths(-(monthCount - 1));
            endLagos = new DateTime(nowLagos.Year, nowLagos.Month, 1).AddMonths(1);
        }
        else
        {
            errorMessage = "Period must be this-month, last-month, last-3-months, last-6-months, or last-12-months.";
            return false;
        }

        range = new DashboardPeriodRange(
            TimeZoneInfo.ConvertTimeToUtc(startLagos, LagosTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(endLagos, LagosTimeZone));

        return true;
    }

    public static string ToPeriodLabel(int year, int month) =>
        new DateTime(year, month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture);

    private static TimeZoneInfo LagosTimeZone => ResolveLagosTimeZone();

    private static TimeZoneInfo ResolveLagosTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Lagos");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("W. Central Africa Standard Time");
        }
    }
}
