using BuildingBlocks.Infrastructure.Implementations;
using Microsoft.EntityFrameworkCore;
using Antital.Domain.Models;

namespace Antital.Infrastructure;

public class AntitalDBContext(
    DbContextOptions<AntitalDBContext> options
    ) : DBContext(options)
{
    public DbSet<SampleModel> SampleModels { get; set; }
    public DbSet<AnotherSampleModel> AnotherSampleModels { get; set; }
}
