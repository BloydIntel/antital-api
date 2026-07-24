using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IFundraiserInvestorMessagesRepository
{
    Task<(IReadOnlyList<OfferingInvestorMessage> Items, int TotalCount, int UnansweredCount)> ListMessagesAsync(
        int offeringId,
        bool? answered,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OfferingInvestorMessage?> GetByIdAsync(int messageId, CancellationToken cancellationToken = default);

    Task<(int TotalCount, int AnsweredCount, double? AverageResponseTimeHours)> GetAnalyticsAsync(
        int offeringId,
        CancellationToken cancellationToken = default);

    Task AddAsync(OfferingInvestorMessage message, CancellationToken cancellationToken = default);

    Task UpdateAsync(OfferingInvestorMessage message, CancellationToken cancellationToken = default);
}
