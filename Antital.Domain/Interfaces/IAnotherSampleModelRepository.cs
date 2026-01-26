using BuildingBlocks.Domain.Interfaces;
using Antital.Domain.Models;

namespace Antital.Domain.Interfaces;

public interface IAnotherSampleModelRepository : IReadOnlyRepository<AnotherSampleModel>
{
    public Task<int> GetTotalCount(CancellationToken cancellationToken = default);
}
