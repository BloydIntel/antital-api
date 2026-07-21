using Antital.Domain.Models;
using BuildingBlocks.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Antital.Infrastructure;

public class AntitalDBContext(
    DbContextOptions<AntitalDBContext> options
    ) : DBContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserOnboarding> UserOnboardings { get; set; }
    public DbSet<UserInvestmentProfile> UserInvestmentProfiles { get; set; }
    public DbSet<UserKyc> UserKycs { get; set; }
    public DbSet<InvestmentOffering> InvestmentOfferings { get; set; }
    public DbSet<OfferingFunding> OfferingFundings { get; set; }
    public DbSet<DealTerms> DealTerms { get; set; }
    public DbSet<Highlight> Highlights { get; set; }
    public DbSet<OfferingContentBlock> OfferingContentBlocks { get; set; }
    public DbSet<ContentBlockItem> ContentBlockItems { get; set; }
    public DbSet<TeamMember> TeamMembers { get; set; }
    public DbSet<FinancialMetric> FinancialMetrics { get; set; }
    public DbSet<UseOfProceedsItem> UseOfProceedsItems { get; set; }
    public DbSet<OfferingRisk> OfferingRisks { get; set; }
    public DbSet<OfferingDocument> OfferingDocuments { get; set; }
    public DbSet<MediaAsset> MediaAssets { get; set; }
    public DbSet<OfferingUpdate> OfferingUpdates { get; set; }
    public DbSet<Testimonial> Testimonials { get; set; }
    public DbSet<OfferingCorporateProfile> OfferingCorporateProfiles { get; set; }
    public DbSet<InvestorWallet> InvestorWallets { get; set; }
    public DbSet<InvestorHolding> InvestorHoldings { get; set; }
    public DbSet<InvestorWatchlistItem> InvestorWatchlistItems { get; set; }
    public DbSet<InvestorPortfolioPerformancePoint> InvestorPortfolioPerformancePoints { get; set; }
    public DbSet<InvestmentOrder> InvestmentOrders { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<InvestorPaymentMethod> InvestorPaymentMethods { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enforce UTC for all DateTime properties
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => DateTime.SpecifyKind(v, v.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : v.Kind).ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue
                ? DateTime.SpecifyKind(v.Value, v.Value.Kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : v.Value.Kind).ToUniversalTime()
                : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            // Unique index on Email
            entity.HasIndex(e => e.Email)
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            // Index on EmailVerificationToken for performance
            entity.HasIndex(e => e.EmailVerificationToken)
                .HasFilter("\"EmailVerificationToken\" IS NOT NULL AND \"IsDeleted\" = false");

            // Configure string lengths
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.PreferredName)
                .HasMaxLength(50);

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Nationality)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.CountryOfResidence)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.StateOfResidence)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.ResidentialAddress)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.EmailVerificationToken)
                .HasMaxLength(500);

            entity.Property(e => e.RefreshTokenHash)
                .HasMaxLength(500);

            entity.Property(e => e.UnverifiedOtpHash)
                .HasMaxLength(500);

            entity.HasIndex(e => e.RefreshTokenHash)
                .HasFilter("\"RefreshTokenHash\" IS NOT NULL AND \"IsDeleted\" = false");
        });

        // UserOnboarding: one per user (unique UserId per flow if we scope by flow later)
        modelBuilder.Entity<UserOnboarding>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // UserInvestmentProfile: one per user
        modelBuilder.Entity<UserInvestmentProfile>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // UserKyc: one per user
        modelBuilder.Entity<UserKyc>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        ConfigureInvestorDashboard(modelBuilder);
        ConfigureInvestmentOfferings(modelBuilder);
        ConfigureInvestmentCheckout(modelBuilder);
    }

    private static void ConfigureInvestmentCheckout(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvestmentOrder>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Offering).WithMany().HasForeignKey(e => e.OfferingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.InvestorHolding).WithMany().HasForeignKey(e => e.InvestorHoldingId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Currency).HasMaxLength(10).IsRequired();
            entity.Property(e => e.PaystackReference).HasMaxLength(100);

            entity.HasIndex(e => e.PaystackReference)
                .IsUnique()
                .HasFilter("\"PaystackReference\" IS NOT NULL AND \"IsDeleted\" = false");

            entity.HasIndex(e => new { e.UserId, e.OfferingId, e.Status })
                .HasFilter("\"Status\" = 0 AND \"IsDeleted\" = false");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasOne(e => e.Order).WithMany(o => o.PaymentTransactions).HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Reference).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Channel).HasMaxLength(50);

            entity.HasIndex(e => e.Reference)
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
        });
    }

    private static void ConfigureInvestorDashboard(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvestorWallet>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => e.UserId).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.Property(e => e.Currency).HasMaxLength(10).IsRequired();
        });

        modelBuilder.Entity<InvestorHolding>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Offering).WithMany().HasForeignKey(e => e.OfferingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.UserId, e.OfferingId, e.InvestedAt });
        });

        modelBuilder.Entity<InvestorWatchlistItem>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Offering).WithMany().HasForeignKey(e => e.OfferingId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.UserId, e.OfferingId }).IsUnique().HasFilter("\"IsDeleted\" = false");
        });

        modelBuilder.Entity<InvestorPortfolioPerformancePoint>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(e => new { e.UserId, e.Year, e.Month }).IsUnique().HasFilter("\"IsDeleted\" = false");
        });

        modelBuilder.Entity<InvestorPaymentMethod>(entity =>
        {
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Title).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Subtitle).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ProviderName).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Last4).HasMaxLength(4).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.IsDefault }).HasFilter("\"IsDeleted\" = false AND \"IsDefault\" = true");
        });
    }

    private static void ConfigureInvestmentOfferings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InvestmentOffering>(entity =>
        {
            entity.HasIndex(e => e.Slug).IsUnique().HasFilter("\"IsDeleted\" = false");
            entity.Property(e => e.Slug).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Tagline).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CoverImageUrl).HasMaxLength(500).IsRequired();

            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.OwnerUserId)
                .HasFilter("\"OwnerUserId\" IS NOT NULL AND \"IsDeleted\" = false");
        });

        modelBuilder.Entity<OfferingFunding>(entity =>
        {
            entity.HasOne(e => e.Offering).WithOne(o => o.Funding).HasForeignKey<OfferingFunding>(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OfferingId).IsUnique();
        });

        modelBuilder.Entity<DealTerms>(entity =>
        {
            entity.HasOne(e => e.Offering).WithOne(o => o.DealTerms).HasForeignKey<DealTerms>(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OfferingId).IsUnique();
        });

        modelBuilder.Entity<OfferingCorporateProfile>(entity =>
        {
            entity.HasOne(e => e.Offering).WithOne(o => o.CorporateProfile).HasForeignKey<OfferingCorporateProfile>(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.OfferingId).IsUnique();
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.Jurisdiction).HasMaxLength(100);
            entity.Property(e => e.RegistrationId).HasMaxLength(100);
        });

        modelBuilder.Entity<Highlight>(entity =>
        {
            entity.HasOne(e => e.Offering).WithMany(o => o.Highlights).HasForeignKey(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Headline).HasMaxLength(200);
            entity.Property(e => e.Body).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<OfferingContentBlock>(entity =>
        {
            entity.HasOne(e => e.Offering).WithMany(o => o.ContentBlocks).HasForeignKey(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(300);
            entity.Property(e => e.Summary).HasMaxLength(4000);
        });

        modelBuilder.Entity<ContentBlockItem>(entity =>
        {
            entity.HasOne(e => e.ContentBlock).WithMany(b => b.Items).HasForeignKey(e => e.ContentBlockId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Label).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Body).HasMaxLength(4000).IsRequired();
        });

        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasOne(e => e.Offering).WithMany(o => o.TeamMembers).HasForeignKey(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Bio).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<FinancialMetric>(entity =>
        {
            entity.HasOne(e => e.Offering).WithMany(o => o.FinancialMetrics).HasForeignKey(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.MetricName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PeriodLabel).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CurrencyCode).HasMaxLength(10);
        });

        modelBuilder.Entity<UseOfProceedsItem>(entity =>
        {
            entity.HasOne(e => e.Offering).WithMany(o => o.UseOfProceedsItems).HasForeignKey(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Category).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<OfferingRisk>(entity =>
        {
            entity.HasOne(e => e.Offering).WithMany(o => o.Risks).HasForeignKey(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Category).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Mitigation).HasMaxLength(4000).IsRequired();
        });

        modelBuilder.Entity<OfferingDocument>(entity =>
        {
            entity.HasOne(e => e.Offering).WithMany(o => o.Documents).HasForeignKey(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.FileUrl).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.HasOne(e => e.Offering).WithMany(o => o.MediaAssets).HasForeignKey(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Url).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<OfferingUpdate>(entity =>
        {
            entity.HasOne(e => e.Offering).WithMany(o => o.Updates).HasForeignKey(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Body).HasMaxLength(8000).IsRequired();
        });

        modelBuilder.Entity<Testimonial>(entity =>
        {
            entity.HasOne(e => e.Offering).WithMany(o => o.Testimonials).HasForeignKey(e => e.OfferingId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Quote).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.AuthorName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AuthorTitle).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
        });
    }
}
