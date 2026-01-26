using BuildingBlocks.Domain.Interfaces;

namespace Antital.Domain.Interfaces;

public interface IAntitalUnitOfWork : IUnitOfWork
{
    public ISampleModelRepository SampleModelRepository { get; init; }
    public IAnotherSampleModelRepository AnotherSampleModelRepository { get; init; }
}