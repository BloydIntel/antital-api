namespace Antital.Application.DTOs.Admin;

public record AdminActivityLogSummaryDto(
    int TotalActivities,
    int CriticalAlerts,
    int FinancialEvents,
    int ComplianceEvents,
    int SupportActivities,
    int SystemEvents);

public record AdminActivityLogItemDto(
    string Id,
    string Event,
    string EventType,
    string Module,
    string Entity,
    string PerformedBy,
    DateTime OccurredAtUtc,
    string Priority,
    string Status,
    string? Detail);

public record AdminActivityLogResponse(
    AdminActivityLogSummaryDto Summary,
    IReadOnlyList<AdminActivityLogItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
