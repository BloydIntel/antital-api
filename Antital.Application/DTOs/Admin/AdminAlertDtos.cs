namespace Antital.Application.DTOs.Admin;

public sealed record AdminAlertSummaryDto(int CriticalAlerts, int Warnings, int ActionedToday);
public sealed record AdminAlertItemDto(
    int Id, string FlagId, DateTime OccurredAtUtc, string Type, string Severity,
    string Status, string EntityAffected, string Description);
public sealed record AdminAlertsResponse(
    AdminAlertSummaryDto Summary, IReadOnlyList<AdminAlertItemDto> Items,
    int Page, int PageSize, int TotalCount, int TotalPages);
