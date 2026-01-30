using Antital.Worker.API.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Antital.Worker.API;

public class AntitalWorkerDbContext(DbContextOptions<AntitalWorkerDbContext> options) : DbContext(options)
{
    public DbSet<TestModel> TestModels { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enforce UTC for DateTime properties
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

        modelBuilder.Entity<TestModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseSerialColumn();
        });
    }
}
