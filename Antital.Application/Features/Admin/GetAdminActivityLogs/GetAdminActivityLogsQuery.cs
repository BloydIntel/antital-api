using Antital.Application.DTOs.Admin;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Admin.GetAdminActivityLogs;

public record GetAdminActivityLogsQuery(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    string? EventType = null,
    string? Module = null,
    string? Priority = null,
    string? Status = null) : ICommandQuery<AdminActivityLogResponse>;
