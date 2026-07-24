using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace Antital.Domain.Models;

public class OfferingInvestorMessage : TrackableEntity
{
    public int OfferingId { get; set; }
    public int AskerUserId { get; set; }

    [MaxLength(4000)]
    public string Question { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string? Reply { get; set; }

    public OfferingInvestorMessageVisibility Visibility { get; set; } =
        OfferingInvestorMessageVisibility.Private;

    public DateTime AskedAt { get; set; }
    public DateTime? RepliedAt { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
    public virtual User AskerUser { get; set; } = null!;
}
