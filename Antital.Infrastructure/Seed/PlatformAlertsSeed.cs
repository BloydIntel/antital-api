using Antital.Domain.Enums;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Antital.Infrastructure.Seed;

/// <summary>Seeds representative alerts for local/demo environments. Idempotent by PublicId.</summary>
public static class PlatformAlertsSeed
{
    public static async Task SeedAsync(AntitalDBContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        if (await context.PlatformAlerts.AnyAsync(x => !x.IsDeleted, cancellationToken)) return;
        var now = DateTime.UtcNow;
        var rows = new[]
        {
            Alert("FLG-1092", "AML/Fraud", AlertSeverity.Critical, AlertStatus.Open, "INV-8921", "Large transaction from blacklisted IP", now.AddMinutes(-10)),
            Alert("FLG-1091", "Regulatory", AlertSeverity.High, AlertStatus.Open, "Platform", "SEC Q3 report submission deadline approaching", now.AddHours(-1)),
            Alert("FLG-1090", "Operational", AlertSeverity.Medium, AlertStatus.Acknowledged, "CMP-104", "Escrow release blocked due to missing signature", now.AddHours(-3)),
            Alert("FLG-1089", "AML/Fraud", AlertSeverity.High, AlertStatus.Open, "TRD-4432", "Suspicious wash trading pattern detected", now.AddHours(-5)),
            Alert("FLG-1088", "Operational", AlertSeverity.Low, AlertStatus.Resolved, "INV-7732", "Investment cap breach warning", now.AddDays(-1)),
            Alert("FLG-1087", "AML/Fraud", AlertSeverity.Critical, AlertStatus.Open, "INV-3321", "Multiple failed login attempts followed by high transfer", now.AddDays(-1)),
            Alert("FLG-1086", "Regulatory", AlertSeverity.Medium, AlertStatus.Dismissed, "Platform", "Annual KYC audit log update required", now.AddDays(-2)),
            Alert("FLG-1085", "Operational", AlertSeverity.High, AlertStatus.Open, "CMP-202", "Disbursement payout failure on active node", now.AddDays(-2)),
            Alert("FLG-1084", "AML/Fraud", AlertSeverity.Low, AlertStatus.Open, "INV-1102", "Unusual profile update from new location", now.AddDays(-3)),
            Alert("FLG-1083", "Operational", AlertSeverity.Critical, AlertStatus.Open, "TRD-1029", "API key exposure detected on public repository", now.AddDays(-3))
        };
        foreach (var row in rows) row.Created("Seed");
        await context.PlatformAlerts.AddRangeAsync(rows, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} platform alerts.", rows.Length);
    }

    private static PlatformAlert Alert(string id, string type, AlertSeverity severity, AlertStatus status, string entity, string description, DateTime occurred) => new()
    { PublicId = id, Type = type, Severity = severity, Status = status, EntityAffected = entity, Description = description, OccurredAtUtc = occurred };
}
