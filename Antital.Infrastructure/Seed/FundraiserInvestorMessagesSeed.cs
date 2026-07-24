using Antital.Domain.Enums;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Antital.Infrastructure.Seed;

/// <summary>
/// Seeds sample investor inbox messages on owned offerings for local fundraiser testing.
/// Idempotent: skips an offering when it already has messages.
/// </summary>
public static class FundraiserInvestorMessagesSeed
{
    public static async Task SeedAsync(
        AntitalDBContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var ownedOfferings = await context.InvestmentOfferings
            .Where(o => !o.IsDeleted && o.OwnerUserId != null)
            .OrderBy(o => o.Id)
            .ToListAsync(cancellationToken);

        if (ownedOfferings.Count == 0)
        {
            return;
        }

        var askers = await context.Users
            .Where(u => !u.IsDeleted &&
                        (u.UserType == UserTypeEnum.IndividualInvestor ||
                         u.UserType == UserTypeEnum.CorporateInvestor))
            .OrderBy(u => u.Id)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (askers.Count == 0)
        {
            logger.LogInformation("Fundraiser investor messages seed skipped: no investor users found.");
            return;
        }

        var seededCount = 0;
        foreach (var offering in ownedOfferings)
        {
            var hasMessages = await context.OfferingInvestorMessages
                .AnyAsync(m => m.OfferingId == offering.Id && !m.IsDeleted, cancellationToken);
            if (hasMessages)
            {
                continue;
            }

            var askerA = askers[0];
            var askerB = askers[Math.Min(1, askers.Count - 1)];
            var askerC = askers[Math.Min(2, askers.Count - 1)];
            var now = DateTime.UtcNow;

            var messages = new[]
            {
                CreateMessage(
                    offering.Id,
                    askerA.Id,
                    "What are your projected returns for the next 24 months?",
                    OfferingInvestorMessageVisibility.Private,
                    now.AddHours(-1),
                    reply: null,
                    repliedAt: null),
                CreateMessage(
                    offering.Id,
                    askerB.Id,
                    "Will there be a secondary market for these units?",
                    OfferingInvestorMessageVisibility.Public,
                    now.AddHours(-3),
                    reply: null,
                    repliedAt: null),
                CreateMessage(
                    offering.Id,
                    askerC.Id,
                    "What is the minimum investment amount for this offering?",
                    OfferingInvestorMessageVisibility.Private,
                    now.AddHours(-3),
                    reply: "The minimum investment is 10M.",
                    repliedAt: now.AddHours(-2)),
            };

            await context.OfferingInvestorMessages.AddRangeAsync(messages, cancellationToken);
            seededCount += messages.Length;
        }

        if (seededCount == 0)
        {
            return;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} fundraiser investor inbox messages.", seededCount);
    }

    private static OfferingInvestorMessage CreateMessage(
        int offeringId,
        int askerUserId,
        string question,
        OfferingInvestorMessageVisibility visibility,
        DateTime askedAt,
        string? reply,
        DateTime? repliedAt)
    {
        var message = new OfferingInvestorMessage
        {
            OfferingId = offeringId,
            AskerUserId = askerUserId,
            Question = question,
            Visibility = visibility,
            AskedAt = askedAt,
            Reply = reply,
            RepliedAt = repliedAt,
        };
        message.Created("Seed");
        return message;
    }
}
