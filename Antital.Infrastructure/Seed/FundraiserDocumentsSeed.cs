using Antital.Domain.Enums;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Antital.Infrastructure.Seed;

/// <summary>
/// Seeds demo document-management rows on each fundraiser's primary owned offering.
/// Idempotent: skips offerings that already have 5+ documents.
/// </summary>
public static class FundraiserDocumentsSeed
{
    private const string SeedActor = "Seed";
    private const string PreferredFundraiserEmail = "johneseyin@gmail.com";

    private static readonly (string Title, DocumentCategory Category, DocumentReviewStatus Status, long Bytes, string Url)[] DemoDocs =
    [
        ("Offering Memorandum.Pdf", DocumentCategory.Core, DocumentReviewStatus.Approved, 2_400_000L,
            "https://res.cloudinary.com/demo/raw/upload/v1/antital/docs/offering-memorandum.pdf"),
        ("Financial Audit Report 2024.pdf", DocumentCategory.Financial, DocumentReviewStatus.Approved, 5_100_000L,
            "https://res.cloudinary.com/demo/raw/upload/v1/antital/docs/financial-audit-2024.pdf"),
        ("Projected Valuation Model.xlsx", DocumentCategory.Analytics, DocumentReviewStatus.PendingApproval, 1_200_000L,
            "https://res.cloudinary.com/demo/raw/upload/v1/antital/docs/valuation-model.xlsx"),
        ("Environmental Impact Study.pdf", DocumentCategory.Analytics, DocumentReviewStatus.PendingApproval, 1_400_000L,
            "https://res.cloudinary.com/demo/raw/upload/v1/antital/docs/environmental-impact.pdf"),
        ("Offering Memorandum Revision.Pdf", DocumentCategory.Regulatory, DocumentReviewStatus.RevisionRequested, 8_400_000L,
            "https://res.cloudinary.com/demo/raw/upload/v1/antital/docs/offering-memorandum-revision.pdf"),
    ];

    public static async Task SeedAsync(
        AntitalDBContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var owners = await context.InvestmentOfferings
            .AsNoTracking()
            .Where(o => !o.IsDeleted && o.OwnerUserId != null)
            .Select(o => o.OwnerUserId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (owners.Count == 0)
        {
            return;
        }

        var preferred = await context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted && u.Email == PreferredFundraiserEmail)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (preferred.HasValue && owners.Contains(preferred.Value))
        {
            owners.Remove(preferred.Value);
            owners.Insert(0, preferred.Value);
        }

        var seeded = 0;
        foreach (var ownerUserId in owners)
        {
            var offering = await GetPrimaryOfferingAsync(context, ownerUserId, cancellationToken);
            if (offering == null)
            {
                continue;
            }

            var existingCount = await context.OfferingDocuments
                .CountAsync(d => d.OfferingId == offering.Id && !d.IsDeleted, cancellationToken);
            if (existingCount >= DemoDocs.Length)
            {
                continue;
            }

            // Replace sparse seed prospectus with full demo set for management UI.
            var existing = await context.OfferingDocuments
                .Where(d => d.OfferingId == offering.Id && !d.IsDeleted)
                .ToListAsync(cancellationToken);
            foreach (var doc in existing)
            {
                doc.Deleted(SeedActor);
            }

            var created = 0;
            for (var i = 0; i < DemoDocs.Length; i++)
            {
                var demo = DemoDocs[i];
                var entity = new OfferingDocument
                {
                    OfferingId = offering.Id,
                    Title = demo.Title,
                    FileUrl = demo.Url,
                    DocumentType = MapDocumentType(demo.Category),
                    Category = demo.Category,
                    ReviewStatus = demo.Status,
                    FileSizeBytes = demo.Bytes,
                    ContentType = demo.Title.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                        ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                        : "application/pdf",
                    CloudinaryPublicId = $"antital/docs/seed-{offering.Id}-{i + 1}",
                    PageCount = demo.Category == DocumentCategory.Core ? 45 : null,
                };
                entity.Created(SeedActor);
                context.OfferingDocuments.Add(entity);
                created++;
            }

            seeded += created;
        }

        if (seeded == 0)
        {
            return;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} fundraiser offering documents.", seeded);
    }

    private static DocumentType MapDocumentType(DocumentCategory category) =>
        category switch
        {
            DocumentCategory.Core => DocumentType.Prospectus,
            DocumentCategory.Financial => DocumentType.FinancialModel,
            _ => DocumentType.Other,
        };

    private static async Task<InvestmentOffering?> GetPrimaryOfferingAsync(
        AntitalDBContext context,
        int ownerUserId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await context.InvestmentOfferings
            .AsNoTracking()
            .Include(o => o.DealTerms)
            .Where(o => o.OwnerUserId == ownerUserId && !o.IsDeleted)
            .OrderByDescending(o => o.Status == OfferingStatus.Published)
            .ThenBy(o => o.DealTerms != null && o.DealTerms.Deadline >= now ? 0 : 1)
            .ThenBy(o => o.DealTerms != null ? o.DealTerms.Deadline : DateTime.MaxValue)
            .ThenByDescending(o => o.PublishedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
