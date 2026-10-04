using Antital.Application.DTOs.Admin;
using Antital.Application.Features.Admin.GetAdminDashboard;
using Antital.Application.Features.Admin.GetAdminActivityLogs;
using Antital.Application.Features.Admin.GetAdminAlerts;
using Antital.Domain.Interfaces;
using BuildingBlocks.API.Controllers;
using BuildingBlocks.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Antital.API.Controllers;

[SwaggerTag("Admin")]
[Route("api/admin")]
[Authorize(Policy = "AdminPolicy")]
[ApiController]
public class AdminController(IMediator mediator) : BaseController
{
    [HttpGet("dashboard")]
    [SwaggerOperation("Get Admin Dashboard", "Returns API-backed operational metrics and recent activity for administrators.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<AdminDashboardResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid period", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Administrator access required", typeof(void))]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string period = AdminDashboardPeriodResolver.DefaultPeriod,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetAdminDashboardQuery(period), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("activity-logs")]
    [SwaggerOperation("Get Admin Activity Logs", "Returns filtered, paginated activity events for administrators.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<AdminActivityLogResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid pagination", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Administrator access required", typeof(void))]
    public async Task<IActionResult> GetActivityLogs(
        [FromQuery] GetAdminActivityLogsQuery request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(request, cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("flags-and-alerts")]
    [SwaggerOperation("Get Admin Flags and Alerts", "Returns filtered, paginated platform alerts for administrators.")]
    public async Task<IActionResult> GetFlagsAndAlerts(
        [FromQuery] GetAdminAlertsQuery request,
        CancellationToken cancellationToken = default) =>
        ApiResult(await mediator.Send(request, cancellationToken));

    [HttpGet("flags-and-alerts/{flagId}")]
    public async Task<IActionResult> GetFlag(string flagId, [FromServices] IAdminAlertsRepository repository, CancellationToken cancellationToken = default)
    {
        var alert = await repository.GetByPublicIdAsync(flagId, cancellationToken);
        if (alert is null) return NotFound();
        return Ok(new { isSuccess = true, value = new { id = alert.Id, flagId = alert.PublicId, alert.Type, severity = alert.Severity.ToString().ToUpperInvariant(), status = alert.Status.ToString(), entityAffected = alert.EntityAffected, alert.Description, occurredAtUtc = alert.OccurredAtUtc } });
    }

    public sealed record UpdateFlagRequest(string? Status, int? AssigneeUserId, string? ResolutionNote);

    [HttpPatch("flags-and-alerts/{flagId}")]
    public async Task<IActionResult> UpdateFlag(string flagId, [FromBody] UpdateFlagRequest request, [FromServices] IAdminAlertsRepository repository, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Status) && !Enum.TryParse<Antital.Domain.Enums.AlertStatus>(request.Status, true, out _))
            return BadRequest(new { isSuccess = false, error = "Invalid alert status." });
        var alert = await repository.UpdateAsync(flagId, new AdminAlertUpdate(request.Status, request.AssigneeUserId, request.ResolutionNote), User.Identity?.Name ?? "admin", cancellationToken);
        if (alert is null) return NotFound();
        return Ok(new { isSuccess = true, value = new { flagId = alert.PublicId, status = alert.Status.ToString(), assigneeUserId = alert.AssigneeUserId, resolutionNote = alert.ResolutionNote } });
    }
}
