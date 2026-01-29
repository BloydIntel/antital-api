using BuildingBlocks.Infrastructure.Implementations;
using Antital.Domain.Interfaces;

namespace Antital.Infrastructure;

public class AntitalUnitOfWork(
    DBContext dbContext,
    ISampleModelRepository sampleModelRepository,
    IAnotherSampleModelRepository anotherSampleModelRepository,
    IUserRepository userRepository
    ) : UnitOfWork(dbContext), IAntitalUnitOfWork
{
    public ISampleModelRepository SampleModelRepository { get; init; } = sampleModelRepository;
    public IAnotherSampleModelRepository AnotherSampleModelRepository { get; init; } = anotherSampleModelRepository;
    public IUserRepository UserRepository { get; init; } = userRepository;
}
