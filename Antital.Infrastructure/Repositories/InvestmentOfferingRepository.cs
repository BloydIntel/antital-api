using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Domain.Interfaces;
using BuildingBlocks.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class InvestmentOfferingRepository(
    DBContext dbContext,
    ICurrentUser currentUser
) : Repository<InvestmentOffering>(dbContext, currentUser), IInvestmentOfferingRepository
{
    private IQueryable<InvestmentOffering> PublishedOfferings =>
        SetAsNoTracking.Where(o => o.Status == OfferingStatus.Published && !o.IsDeleted);

    public async Task<(IReadOnlyList<InvestmentOffering> Items, int TotalCount)> ListPublishedAsync(
        int page,
        int pageSize,
        string? category,
        OfferingRiskLevel? risk,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = PublishedOfferings
            .Include(o => o.Funding)
            .Include(o => o.DealTerms)
            .Where(o => o.Funding != null && o.DealTerms != null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(o => o.Category == category);
        }

        if (risk.HasValue)
        {
            query = query.Where(o => o.RiskLevel == risk.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(o =>
                o.Name.Contains(term) ||
                o.Tagline.Contains(term) ||
                o.Category.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.PublishedAt)
            .ThenBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<InvestmentOffering?> GetPublishedShellByIdAsync(int id, CancellationToken cancellationToken) =>
        GetPublishedShellQuery()
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<InvestmentOffering?> GetPublishedShellBySlugAsync(string slug, CancellationToken cancellationToken) =>
        GetPublishedShellQuery()
            .FirstOrDefaultAsync(o => o.Slug == slug, cancellationToken);

    public async Task<InvestmentOffering?> GetPublishedShellByIdOrSlugAsync(
        string idOrSlug,
        CancellationToken cancellationToken)
    {
        if (int.TryParse(idOrSlug, out var id))
        {
            return await GetPublishedShellByIdAsync(id, cancellationToken);
        }

        return await GetPublishedShellBySlugAsync(idOrSlug, cancellationToken);
    }

    public async Task<int?> GetPublishedOfferingIdAsync(string idOrSlug, CancellationToken cancellationToken)
    {
        if (int.TryParse(idOrSlug, out var id))
        {
            return await PublishedOfferings
                .Where(o => o.Id == id)
                .Select(o => (int?)o.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return await PublishedOfferings
            .Where(o => o.Slug == idOrSlug)
            .Select(o => (int?)o.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Highlight>> GetHighlightsAsync(int offeringId, CancellationToken cancellationToken) =>
        await _dbContext.Set<Highlight>()
            .AsNoTracking()
            .Where(h => h.OfferingId == offeringId && !h.IsDeleted)
            .OrderBy(h => h.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OfferingContentBlock>> GetContentBlocksAsync(int offeringId, CancellationToken cancellationToken) =>
        await _dbContext.Set<OfferingContentBlock>()
            .AsNoTracking()
            .Include(b => b.Items.Where(i => !i.IsDeleted))
            .Where(b => b.OfferingId == offeringId && !b.IsDeleted)
            .OrderBy(b => b.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TeamMember>> GetTeamMembersAsync(int offeringId, CancellationToken cancellationToken) =>
        await _dbContext.Set<TeamMember>()
            .AsNoTracking()
            .Where(m => m.OfferingId == offeringId && !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FinancialMetric>> GetFinancialMetricsAsync(int offeringId, CancellationToken cancellationToken) =>
        await _dbContext.Set<FinancialMetric>()
            .AsNoTracking()
            .Where(m => m.OfferingId == offeringId && !m.IsDeleted)
            .OrderBy(m => m.MetricName)
            .ThenBy(m => m.PeriodSortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<UseOfProceedsItem>> GetUseOfProceedsItemsAsync(int offeringId, CancellationToken cancellationToken) =>
        await _dbContext.Set<UseOfProceedsItem>()
            .AsNoTracking()
            .Where(i => i.OfferingId == offeringId && !i.IsDeleted)
            .OrderBy(i => i.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OfferingRisk>> GetRisksAsync(int offeringId, CancellationToken cancellationToken) =>
        await _dbContext.Set<OfferingRisk>()
            .AsNoTracking()
            .Where(r => r.OfferingId == offeringId && !r.IsDeleted)
            .OrderBy(r => r.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OfferingDocument>> GetDocumentsAsync(int offeringId, CancellationToken cancellationToken) =>
        await _dbContext.Set<OfferingDocument>()
            .AsNoTracking()
            .Where(d => d.OfferingId == offeringId && !d.IsDeleted)
            .OrderBy(d => d.Title)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MediaAsset>> GetMediaAssetsAsync(int offeringId, CancellationToken cancellationToken) =>
        await _dbContext.Set<MediaAsset>()
            .AsNoTracking()
            .Where(m => m.OfferingId == offeringId && !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<OfferingUpdate> Items, int TotalCount)> GetUpdatesAsync(
        int offeringId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<OfferingUpdate>()
            .AsNoTracking()
            .Where(u => u.OfferingId == offeringId && !u.IsDeleted);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Testimonial>> GetTestimonialsAsync(int offeringId, CancellationToken cancellationToken) =>
        await _dbContext.Set<Testimonial>()
            .AsNoTracking()
            .Where(t => t.OfferingId == offeringId && !t.IsDeleted)
            .OrderBy(t => t.SortOrder)
            .ToListAsync(cancellationToken);

    private IQueryable<InvestmentOffering> GetPublishedShellQuery() =>
        PublishedOfferings
            .Include(o => o.Funding)
            .Include(o => o.DealTerms)
            .Include(o => o.CorporateProfile)
            .Where(o => o.Funding != null && o.DealTerms != null);
}
