using Antital.Domain.Enums;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Antital.Infrastructure.Seed;

public static class WalletTransactionSeed
{
    private const string SeedActor = "system";

    public static async Task SeedAsync(
        AntitalDBContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var demoUser = await context.Users
            .Where(u => u.IsEmailVerified && u.UserType == UserTypeEnum.IndividualInvestor && !u.IsDeleted)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (demoUser == null)
        {
            return;
        }

        var hasPaidOrder = await context.InvestmentOrders
            .AnyAsync(
                o => o.UserId == demoUser.Id && o.Status == InvestmentOrderStatus.Paid && !o.IsDeleted,
                cancellationToken);

        if (hasPaidOrder)
        {
            return;
        }

        var offering = await context.InvestmentOfferings
            .Include(o => o.Funding)
            .Where(o => o.Status == OfferingStatus.Published && !o.IsDeleted)
            .OrderBy(o => o.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (offering?.Funding == null)
        {
            return;
        }

        var units = 50;
        var sharePrice = offering.Funding.SharePrice;
        var subtotal = units * sharePrice;
        var platformFeePercent = 2.5m;
        var platformFee = Math.Round(subtotal * platformFeePercent / 100m, 2);
        var totalAmount = subtotal + platformFee;

        var order = new InvestmentOrder
        {
            UserId = demoUser.Id,
            OfferingId = offering.Id,
            Units = units,
            SharePrice = sharePrice,
            Subtotal = subtotal,
            PlatformFeePercent = platformFeePercent,
            PlatformFee = platformFee,
            TotalAmount = totalAmount,
            Currency = "NGN",
            Status = InvestmentOrderStatus.Paid,
            PaymentChannel = PaymentChannel.Card,
            PaystackReference = $"ANT-ORD-seed-{offering.Slug}-{Guid.NewGuid():N}",
            PaidAt = DateTime.UtcNow.AddDays(-2),
        };
        order.Created(SeedActor);

        context.InvestmentOrders.Add(order);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded paid investment order {OrderId} for user {UserId} ({Email}).",
            order.Id,
            demoUser.Id,
            demoUser.Email);
    }
}
