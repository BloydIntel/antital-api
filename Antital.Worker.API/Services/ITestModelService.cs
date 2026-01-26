using Antital.Worker.API.Model;

namespace Antital.Worker.API.Services;

public interface ITestModelService
{
    Task<List<TestModel>> GetAll(CancellationToken cancellationToken);
    Task Add(string name, CancellationToken cancellationToken);
}
