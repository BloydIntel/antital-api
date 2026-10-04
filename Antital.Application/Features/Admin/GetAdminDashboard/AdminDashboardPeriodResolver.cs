namespace Antital.Application.Features.Admin.GetAdminDashboard;

public readonly record struct AdminDashboardPeriodRange(
    DateTime CurrentStartUtc,
    DateTime CurrentEndUtc,
    DateTime PreviousStartUtc,
    DateTime PreviousEndUtc);

public static class AdminDashboardPeriodResolver
{
    public const string DefaultPeriod = "last-30-days";
    public const string ValidationMessage =
        "Period must be last-7-days, last-30-days, or last-90-days.";

    public static bool TryResolve(
        string? period,
        DateTimeOffset now,
        out AdminDashboardPeriodRange range,
        out string? errorMessage)
    {
        range = default;
        errorMessage = null;

        var normalized = string.IsNullOrWhiteSpace(period)
            ? DefaultPeriod
            : period.Trim().ToLowerInvariant();

        var dayCount = normalized switch
        {
            "last-7-days" => 7,
            "last-30-days" => 30,
            "last-90-days" => 90,
            _ => 0,
        };

        if (dayCount == 0)
        {
            errorMessage = ValidationMessage;
            return false;
        }

        var currentEndUtc = now.ToUniversalTime().UtcDateTime;
        var currentStartUtc = currentEndUtc.AddDays(-dayCount);
        var previousEndUtc = currentStartUtc;
        var previousStartUtc = previousEndUtc.AddDays(-dayCount);

        range = new AdminDashboardPeriodRange(
            currentStartUtc,
            currentEndUtc,
            previousStartUtc,
            previousEndUtc);

        return true;
    }
}
