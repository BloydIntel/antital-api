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

    [HttpGet("investors")]
    public async Task<IActionResult> GetInvestors([FromServices] IAdminInvestorsRepository repository, [FromQuery] string? status = null, [FromQuery] string? kycStatus = null, [FromQuery] bool? highNetWorth = null, [FromQuery] string? search = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] string? sortBy = null, [FromQuery] bool descending = true, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100) return BadRequest(new { isSuccess = false, error = "Invalid pagination." });
        var result = await repository.ListAsync(new AdminInvestorQueryOptions(status, kycStatus, highNetWorth, search, from, to, sortBy, descending, page, pageSize), cancellationToken);
        return Ok(new { isSuccess = true, value = new { summary = new { totalInvestors = result.TotalInvestors, pendingKyc = result.PendingKyc, suspendedAccounts = result.SuspendedAccounts, totalWalletBalance = result.TotalWalletBalance }, items = result.Items, page, pageSize, totalCount = result.TotalCount, totalPages = (int)Math.Ceiling(result.TotalCount / (double)pageSize) } });
    }

    [HttpGet("investors/{investorId}")]
    public async Task<IActionResult> GetInvestor(string investorId, [FromServices] IAdminInvestorsRepository repository, CancellationToken cancellationToken = default)
    {
        var result = await repository.GetAsync(investorId, cancellationToken);
        return result is null ? NotFound() : Ok(new { isSuccess = true, value = result });
    }

    public sealed record UpdateInvestorRequest(string? KycStatus, string? Note, bool? Suspended, string? SuspensionAction, string? EvidenceJson);

    [HttpPatch("investors/{investorId}")]
    public async Task<IActionResult> UpdateInvestor(string investorId, [FromBody] UpdateInvestorRequest request, [FromServices] IAdminInvestorsRepository repository, CancellationToken cancellationToken = default)
    {
        if (request.KycStatus is not null && !Enum.TryParse<Antital.Domain.Enums.InvestorKycStatus>(request.KycStatus, true, out _)) return BadRequest(new { isSuccess = false, error = "Invalid KYC status." });
        if (request.KycStatus is not null && string.IsNullOrWhiteSpace(request.Note)) return BadRequest(new { isSuccess = false, error = "A review note is required for KYC updates." });
        if (request.SuspensionAction is not null && request.SuspensionAction is not ("note" or "contact" or "str")) return BadRequest(new { isSuccess = false, error = "Invalid suspension action." });
        if (request.SuspensionAction is not null && string.IsNullOrWhiteSpace(request.Note)) return BadRequest(new { isSuccess = false, error = "A note is required for suspension actions." });
        var requestId = Request.Headers.TryGetValue("Idempotency-Key", out var key) ? key.ToString() : null;
        var result = await repository.UpdateAsync(investorId, new AdminInvestorMutation(request.KycStatus, request.Note, request.Suspended, request.SuspensionAction, requestId, request.EvidenceJson), User.Identity?.Name ?? "admin", cancellationToken);
        return result is null ? NotFound() : Ok(new { isSuccess = true, value = result });
    }
}
