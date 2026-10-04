using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public sealed class AdminAlertsRepository(AntitalDBContext context) : IAdminAlertsRepository
{
    public Task<PlatformAlert?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default) =>
        context.PlatformAlerts.AsNoTracking().FirstOrDefaultAsync(x => !x.IsDeleted && x.PublicId == publicId, cancellationToken);

    public async Task<PlatformAlert?> UpdateAsync(string publicId, AdminAlertUpdate update, string updatedBy, CancellationToken cancellationToken = default)
    {
        var alert = await context.PlatformAlerts.FirstOrDefaultAsync(x => !x.IsDeleted && x.PublicId == publicId, cancellationToken);
        if (alert is null) return null;
        if (!string.IsNullOrWhiteSpace(update.Status) && Enum.TryParse<AlertStatus>(update.Status, true, out var status)) alert.Status = status;
        if (update.AssigneeUserId.HasValue) alert.AssigneeUserId = update.AssigneeUserId;
        if (update.ResolutionNote is not null) alert.ResolutionNote = update.ResolutionNote;
        alert.Updated(updatedBy);
        await context.SaveChangesAsync(cancellationToken);
        return alert;
    }

    public async Task<AdminAlertsResult> GetAsync(AdminAlertsQueryOptions request, CancellationToken cancellationToken = default)
    {
        var query = context.PlatformAlerts.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(request.Type)) query = query.Where(x => x.Type == request.Type);
        if (Enum.TryParse<AlertSeverity>(request.Severity, true, out var severity)) query = query.Where(x => x.Severity == severity);
        if (Enum.TryParse<AlertStatus>(request.Status, true, out var status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(request.Search)) query = query.Where(x => x.PublicId.Contains(request.Search) || x.EntityAffected.Contains(request.Search) || x.Description.Contains(request.Search));
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.OccurredAtUtc).ThenByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;
        var summary = context.PlatformAlerts.AsNoTracking().Where(x => !x.IsDeleted);
        var critical = await summary.CountAsync(x => x.Severity == AlertSeverity.Critical && x.Status != AlertStatus.Resolved && x.Status != AlertStatus.Dismissed, cancellationToken);
        var warnings = await summary.CountAsync(x => (x.Severity == AlertSeverity.High || x.Severity == AlertSeverity.Medium) && x.Status != AlertStatus.Resolved && x.Status != AlertStatus.Dismissed, cancellationToken);
        var actioned = await summary.CountAsync(x => (x.Status == AlertStatus.Resolved || x.Status == AlertStatus.Dismissed) && x.UpdatedAt >= today, cancellationToken);
        return new AdminAlertsResult(critical, warnings, actioned, total, items);
    }
}
