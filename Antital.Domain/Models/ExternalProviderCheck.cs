using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

/// <summary>
/// Durable audit trail for third-party provider calls (Dojah, Paystack, etc.).
/// Do not store full PII identifiers — use <see cref="RequestFingerprint"/> only.
/// </summary>
public class ExternalProviderCheck : TrackableEntity
{
    public string Provider { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? ExternalReference { get; set; }
    public bool Success { get; set; }
    public int StatusCode { get; set; }
    public string? ErrorCode { get; set; }
    /// <summary>Masked request identifier, e.g. ****2222.</summary>
    public string? RequestFingerprint { get; set; }

    public virtual User? User { get; set; }
}
