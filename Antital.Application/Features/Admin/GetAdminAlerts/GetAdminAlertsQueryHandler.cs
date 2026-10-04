using Antital.Application.DTOs.Admin;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Admin.GetAdminAlerts;

public sealed class GetAdminAlertsQueryHandler(IAdminAlertsRepository repository)
    : ICommandQueryHandler<GetAdminAlertsQuery, AdminAlertsResponse>
{
    public async Task<Result<AdminAlertsResponse>> Handle(GetAdminAlertsQuery request, CancellationToken cancellationToken)
    {
        if (request.Page < 1 || request.PageSize is < 1 or > 100)
            throw new BadRequestException("Invalid alert pagination.", new Dictionary<string, string[]>());
        var result = await repository.GetAsync(new AdminAlertsQueryOptions(request.Type, request.Severity, request.Status, request.Search, request.Page, request.PageSize), cancellationToken);
        var response = new AdminAlertsResponse(
            new AdminAlertSummaryDto(result.Critical, result.Warnings, result.ActionedToday),
            result.Items.Select(x => new AdminAlertItemDto(x.Id, x.PublicId, x.OccurredAtUtc, x.Type,
                x.Severity.ToString().ToUpperInvariant(), x.Status.ToString(), x.EntityAffected, x.Description)).ToList(),
            request.Page, request.PageSize, result.TotalCount,
            result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)request.PageSize));
        var output = new Result<AdminAlertsResponse>(); output.AddValue(response); output.OK(); return output;
    }
}
