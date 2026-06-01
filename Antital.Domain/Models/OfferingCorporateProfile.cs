using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class OfferingCorporateProfile : TrackableEntity
{
    public int OfferingId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public int IncorporationYear { get; set; }
    public string RegistrationId { get; set; } = string.Empty;
    public string? AdditionalNotes { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
