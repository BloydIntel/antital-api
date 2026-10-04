using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public sealed record AdminAlertsResult(int Critical, int Warnings, int ActionedToday, int TotalCount, IReadOnlyList<PlatformAlert> Items);
public sealed record AdminAlertsQueryOptions(string? Type, string? Severity, string? Status, string? Search, int Page, int PageSize);
public sealed record AdminAlertUpdate(string? Status, int? AssigneeUserId, string? ResolutionNote);
public interface IAdminAlertsRepository
{
    Task<AdminAlertsResult> GetAsync(AdminAlertsQueryOptions request, CancellationToken cancellationToken = default);
    Task<PlatformAlert?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default);
    Task<PlatformAlert?> UpdateAsync(string publicId, AdminAlertUpdate update, string updatedBy, CancellationToken cancellationToken = default);
}
