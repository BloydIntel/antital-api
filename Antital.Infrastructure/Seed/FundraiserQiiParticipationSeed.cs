using Antital.Domain.Enums;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Antital.Infrastructure.Seed;

/// <summary>
/// Seeds sample QII corporate investors with confirmed holdings / pending orders
/// on each fundraiser's primary owned offering (same selection rule as dashboard).
/// Idempotent per offering.
/// </summary>
public static class FundraiserQiiParticipationSeed
{
    private const string SeedActor = "Seed";
    private const string PreferredFundraiserEmail = "johneseyin@gmail.com";

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

        // Prefer the known local fundraiser first so demo data lands on their primary campaign.
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

        var confirmed = await EnsureQiiUserAsync(
            context,
            "qii-stanbic-seed@example.com",
            "Stanbic IBTC Asset Mgmt",
            QiiInstitutionType.AssetManagementCompany,
            cancellationToken);
        var pending = await EnsureQiiUserAsync(
            context,
            "qii-arm-seed@example.com",
            "ARM Investment Managers",
            QiiInstitutionType.VentureCapitalOrPrivateEquityFund,
            cancellationToken);

        var seededCount = 0;
        foreach (var ownerUserId in owners)
        {
            var offering = await GetPrimaryOfferingAsync(context, ownerUserId, cancellationToken);
            if (offering == null)
            {
                continue;
            }

            var hasQii = await (
                from h in context.InvestorHoldings
                join p in context.UserInvestmentProfiles on h.UserId equals p.UserId
                where h.OfferingId == offering.Id
                      && !h.IsDeleted
                      && !p.IsDeleted
                      && p.InvestorCategory == InvestorCategory.QualifiedInstitutionalInvestor
                select h.Id
            ).AnyAsync(cancellationToken);

            var hasPending = await (
                from o in context.InvestmentOrders
                join p in context.UserInvestmentProfiles on o.UserId equals p.UserId
                where o.OfferingId == offering.Id
                      && !o.IsDeleted
                      && !p.IsDeleted
                      && o.Status == InvestmentOrderStatus.PendingPayment
                      && p.InvestorCategory == InvestorCategory.QualifiedInstitutionalInvestor
                select o.Id
            ).AnyAsync(cancellationToken);

            if (hasQii || hasPending)
            {
                continue;
            }

            var now = DateTime.UtcNow;
            var holding = new InvestorHolding
            {
                UserId = confirmed.Id,
                OfferingId = offering.Id,
                InvestedAmount = 40_000_000m,
                CurrentValue = 40_000_000m,
                Returns = 0m,
                UnitHolding = 400,
                InvestedAt = now.AddDays(-20),
            };
            holding.Created(SeedActor);

            var order = new InvestmentOrder
            {
                UserId = pending.Id,
                OfferingId = offering.Id,
                Units = 185,
                SharePrice = 100_000m,
                Subtotal = 18_500_000m,
                PlatformFeePercent = 0m,
                PlatformFee = 0m,
                TotalAmount = 18_500_000m,
                Currency = "NGN",
                Status = InvestmentOrderStatus.PendingPayment,
                ExpiresAt = now.AddDays(2),
            };
            order.Created(SeedActor);

            context.InvestorHoldings.Add(holding);
            context.InvestmentOrders.Add(order);
            seededCount++;
        }

        if (seededCount == 0)
        {
            return;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Seeded QII participation samples on {Count} primary owned offering(s).",
            seededCount);
    }

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

    private static async Task<User> EnsureQiiUserAsync(
        AntitalDBContext context,
        string email,
        string companyName,
        QiiInstitutionType institutionType,
        CancellationToken cancellationToken)
    {
        var user = await context.Users.FirstOrDefaultAsync(
            u => u.Email == email && !u.IsDeleted,
            cancellationToken);

        if (user == null)
        {
            user = new User
            {
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                UserType = UserTypeEnum.CorporateInvestor,
                IsEmailVerified = true,
                FirstName = "QII",
                LastName = "Investor",
                PhoneNumber = "+2348011111111",
                DateOfBirth = new DateTime(1985, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Nationality = "Nigerian",
                CountryOfResidence = "Nigeria",
                StateOfResidence = "Lagos",
                ResidentialAddress = "Victoria Island",
                HasAgreedToTerms = true,
            };
            user.Created(SeedActor);
            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);
        }

        var profile = await context.UserInvestmentProfiles.FirstOrDefaultAsync(
            p => p.UserId == user.Id && !p.IsDeleted,
            cancellationToken);

        if (profile == null)
        {
            profile = new UserInvestmentProfile
            {
                UserId = user.Id,
                InvestorCategory = InvestorCategory.QualifiedInstitutionalInvestor,
                CompanyLegalName = companyName,
                QiiInstitutionTypes = institutionType.ToString(),
                HasValidQiiRegistrationOrLicense = true,
                ConfirmsSecNigeriaQiiCriteria = true,
            };
            profile.Created(SeedActor);
            context.UserInvestmentProfiles.Add(profile);
            await context.SaveChangesAsync(cancellationToken);
        }

        return user;
    }
}
