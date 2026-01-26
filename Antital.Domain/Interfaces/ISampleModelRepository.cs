using BuildingBlocks.Domain.Interfaces;
using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface ISampleModelRepository : IRepository<SampleModel>
{
    public Task<int> GetTotalCount(CancellationToken cancellationToken = default);
}
