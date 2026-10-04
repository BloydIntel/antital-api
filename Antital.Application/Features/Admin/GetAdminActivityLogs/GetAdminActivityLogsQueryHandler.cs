using Antital.Application.DTOs.Admin;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Admin.GetAdminActivityLogs;

public sealed class GetAdminActivityLogsQueryHandler(IAdminDashboardRepository repository)
    : ICommandQueryHandler<GetAdminActivityLogsQuery, AdminActivityLogResponse>
{
    private const int MaxPageSize = 100;

    public async Task<Result<AdminActivityLogResponse>> Handle(
        GetAdminActivityLogsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > MaxPageSize)
        {
            throw new BadRequestException(
                "Invalid activity log pagination.",
                new Dictionary<string, string[]>
                {
                    ["page"] = request.Page < 1 ? ["Page must be at least 1."] : [],
                    ["pageSize"] = request.PageSize is < 1 or > MaxPageSize
                        ? [$"Page size must be between 1 and {MaxPageSize}."]
                        : []
                });
        }

        var result = await repository.GetActivityLogsAsync(
            new AdminActivityLogQuery(
                request.Page,
                request.PageSize,
                request.Search,
                request.EventType,
                request.Module,
                request.Priority,
                request.Status),
            cancellationToken);

        var totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(result.TotalCount / (double)request.PageSize);
        var response = new AdminActivityLogResponse(
            new AdminActivityLogSummaryDto(
                result.TotalCount,
                0,
                result.CountsByEventType.GetValueOrDefault("Financial"),
                result.CountsByEventType.GetValueOrDefault("Compliance"),
                result.CountsByEventType.GetValueOrDefault("Support"),
                result.CountsByEventType.GetValueOrDefault("System")),
            result.Items.Select(item => new AdminActivityLogItemDto(
                item.Id,
                item.Description,
                item.EventType,
                item.Module,
                item.Subject,
                "System",
                item.OccurredAtUtc,
                item.Priority,
                item.Status,
                item.Description)).ToList(),
            request.Page,
            request.PageSize,
            result.TotalCount,
            totalPages);

        var apiResult = new Result<AdminActivityLogResponse>();
        apiResult.AddValue(response);
        apiResult.OK();
        return apiResult;
    }
}
