using Antital.Worker.API.Model;
using Microsoft.EntityFrameworkCore;

namespace Antital.Worker.API;

public class Antital.WorkerDBContext(DbContextOptions<Antital.WorkerDBContext> options) : DbContext(options)
{
    public DbSet<TestModel> TestModels { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestModel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).UseSerialColumn();
        });
    }
}