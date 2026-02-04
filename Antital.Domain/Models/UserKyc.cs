using BuildingBlocks.Domain.Models;
using Antital.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Antital.Domain.Models;

/// <summary>
/// KYC (Identity Verification) data for a user. Residential address comes from User.
/// </summary>
public class UserKyc : TrackableEntity
{
    public int UserId { get; set; }
    public KycIdType IdType { get; set; }

    [MaxLength(50)]
    public string? Nin { get; set; }

    [MaxLength(50)]
    public string? Bvn { get; set; }

    [MaxLength(500)]
    public string? GovernmentIdDocumentPathOrKey { get; set; }

    [MaxLength(500)]
    public string? ProofOfAddressDocumentPathOrKey { get; set; }

    [MaxLength(500)]
    public string? SelfieVerificationPathOrKey { get; set; }

    [MaxLength(500)]
    public string? IncomeVerificationPathOrKey { get; set; }
    /// <summary>Comma-separated IncomeVerificationDocumentType enum values (e.g. "0,2").</summary>
    [MaxLength(50)]
    public string? IncomeVerificationDocumentTypes { get; set; }

    public DateTime? GovernmentIdVerifiedAt { get; set; }
    public DateTime? ProofOfAddressVerifiedAt { get; set; }
    public DateTime? SelfieVerifiedAt { get; set; }
    public DateTime? IncomeVerifiedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
