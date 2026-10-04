using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class PlatformAlert : TrackableEntity
{
    public string PublicId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public AlertStatus Status { get; set; } = AlertStatus.Open;
    public string EntityAffected { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public int? AssigneeUserId { get; set; }
    public string? ResolutionNote { get; set; }
}
