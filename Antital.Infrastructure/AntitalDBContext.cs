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
    }
}
