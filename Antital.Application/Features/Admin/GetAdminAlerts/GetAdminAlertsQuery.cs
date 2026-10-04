using Antital.Application.DTOs.Admin;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Admin.GetAdminAlerts;

public sealed record GetAdminAlertsQuery(
    string? Type = null, string? Severity = null, string? Status = null,
    string? Search = null, int Page = 1, int PageSize = 20)
    : ICommandQuery<AdminAlertsResponse>;
