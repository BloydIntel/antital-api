using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IInvestmentOfferingRepository
{
    Task<(IReadOnlyList<InvestmentOffering> Items, int TotalCount)> ListPublishedAsync(
        int page,
        int pageSize,
        string? category,
        OfferingRiskLevel? risk,
        string? search,
        CancellationToken cancellationToken);

    Task<InvestmentOffering?> GetPublishedShellByIdAsync(int id, CancellationToken cancellationToken);

    Task<InvestmentOffering?> GetPublishedShellBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<InvestmentOffering?> GetPublishedShellByIdOrSlugAsync(string idOrSlug, CancellationToken cancellationToken);

    Task<int?> GetPublishedOfferingIdAsync(string idOrSlug, CancellationToken cancellationToken);

    Task<IReadOnlyList<Highlight>> GetHighlightsAsync(int offeringId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OfferingContentBlock>> GetContentBlocksAsync(int offeringId, CancellationToken cancellationToken);

    Task<IReadOnlyList<TeamMember>> GetTeamMembersAsync(int offeringId, CancellationToken cancellationToken);

    Task<IReadOnlyList<FinancialMetric>> GetFinancialMetricsAsync(int offeringId, CancellationToken cancellationToken);

    Task<IReadOnlyList<UseOfProceedsItem>> GetUseOfProceedsItemsAsync(int offeringId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OfferingRisk>> GetRisksAsync(int offeringId, CancellationToken cancellationToken);

    Task<IReadOnlyList<OfferingDocument>> GetDocumentsAsync(int offeringId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MediaAsset>> GetMediaAssetsAsync(int offeringId, CancellationToken cancellationToken);

    Task<(IReadOnlyList<OfferingUpdate> Items, int TotalCount)> GetUpdatesAsync(
        int offeringId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Testimonial>> GetTestimonialsAsync(int offeringId, CancellationToken cancellationToken);
}
