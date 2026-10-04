using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public sealed class AdminInvestorSuspensionAction : TrackableEntity
{
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string? Channel { get; set; }
    public string? RequestId { get; set; }
    public string? EvidenceJson { get; set; }
    public User User { get; set; } = null!;
}
