using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Antital.Infrastructure.Repositories;

public class FundraiserInvestorMessagesRepository(AntitalDBContext context)
    : IFundraiserInvestorMessagesRepository
{
    public async Task<(IReadOnlyList<OfferingInvestorMessage> Items, int TotalCount, int UnansweredCount)> ListMessagesAsync(
        int offeringId,
        bool? answered,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = context.OfferingInvestorMessages
            .AsNoTracking()
            .Include(m => m.AskerUser)
            .Where(m => m.OfferingId == offeringId && !m.IsDeleted);

        var unansweredCount = await baseQuery.CountAsync(m => m.RepliedAt == null, cancellationToken);

        var filtered = answered switch
        {
            true => baseQuery.Where(m => m.RepliedAt != null),
            false => baseQuery.Where(m => m.RepliedAt == null),
            null => baseQuery,
        };

        var totalCount = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .OrderByDescending(m => m.RepliedAt == null)
            .ThenByDescending(m => m.AskedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount, unansweredCount);
    }

    public Task<OfferingInvestorMessage?> GetByIdAsync(
        int messageId,
        CancellationToken cancellationToken = default) =>
        context.OfferingInvestorMessages
            .Include(m => m.AskerUser)
            .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted, cancellationToken);

    public async Task<(int TotalCount, int AnsweredCount, double? AverageResponseTimeHours)> GetAnalyticsAsync(
        int offeringId,
        CancellationToken cancellationToken = default)
    {
        var messages = await context.OfferingInvestorMessages
            .AsNoTracking()
            .Where(m => m.OfferingId == offeringId && !m.IsDeleted)
            .Select(m => new { m.AskedAt, m.RepliedAt })
            .ToListAsync(cancellationToken);

        var totalCount = messages.Count;
        var answered = messages.Where(m => m.RepliedAt.HasValue).ToList();
        var answeredCount = answered.Count;

        double? averageHours = null;
        if (answeredCount > 0)
        {
            averageHours = answered
                .Average(m => (m.RepliedAt!.Value - m.AskedAt).TotalHours);
        }

        return (totalCount, answeredCount, averageHours);
    }

    public async Task AddAsync(OfferingInvestorMessage message, CancellationToken cancellationToken = default) =>
        await context.OfferingInvestorMessages.AddAsync(message, cancellationToken);

    public Task UpdateAsync(OfferingInvestorMessage message, CancellationToken cancellationToken = default)
    {
        context.OfferingInvestorMessages.Update(message);
        return Task.CompletedTask;
    }
}
