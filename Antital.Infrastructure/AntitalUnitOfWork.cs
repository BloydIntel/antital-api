using BuildingBlocks.Infrastructure.Implementations;
using Antital.Domain.Interfaces;

namespace Antital.Infrastructure;

public class AntitalUnitOfWork(
    DBContext dbContext,
    ISampleModelRepository sampleModelRepository,
    IAnotherSampleModelRepository anotherSampleModelRepository
    ) : UnitOfWork(dbContext), IAntitalUnitOfWork
{
    public ISampleModelRepository SampleModelRepository { get; init; } = sampleModelRepository;
    public IAnotherSampleModelRepository AnotherSampleModelRepository { get; init; } = anotherSampleModelRepository;
}
