using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class InvestmentOffering : TrackableEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Tagline { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public OfferingRiskLevel RiskLevel { get; set; }
    public OfferingStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }

    public OfferingFunding? Funding { get; set; }
    public DealTerms? DealTerms { get; set; }
    public OfferingCorporateProfile? CorporateProfile { get; set; }
    public ICollection<Highlight> Highlights { get; set; } = [];
    public ICollection<OfferingContentBlock> ContentBlocks { get; set; } = [];
    public ICollection<TeamMember> TeamMembers { get; set; } = [];
    public ICollection<FinancialMetric> FinancialMetrics { get; set; } = [];
    public ICollection<UseOfProceedsItem> UseOfProceedsItems { get; set; } = [];
    public ICollection<OfferingRisk> Risks { get; set; } = [];
    public ICollection<OfferingDocument> Documents { get; set; } = [];
    public ICollection<MediaAsset> MediaAssets { get; set; } = [];
    public ICollection<OfferingUpdate> Updates { get; set; } = [];
    public ICollection<Testimonial> Testimonials { get; set; } = [];
}
