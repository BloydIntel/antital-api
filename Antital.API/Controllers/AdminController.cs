using Antital.Application.DTOs.Admin;
using Antital.Application.Features.Admin.GetAdminDashboard;
using Antital.Application.Features.Admin.GetAdminActivityLogs;
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
}
