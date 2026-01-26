using BuildingBlocks.Infrastructure.Implementations;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Antital.Infrastructure.QueryTexts;

namespace Antital.Infrastructure.Repositories;

public class AnotherSampleModelRepository(
    DBContext dbContext
    ) : ReadOnlyRepository<AnotherSampleModel>(dbContext), IAnotherSampleModelRepository
{
    public async Task<int> GetTotalCount(CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.QueryGetAsync<int>(Queries.GetAnotherSampleModelTotalCount, cancellationToken);
        return result;
    }
}