using Antital.Worker.API.Model;
using Microsoft.EntityFrameworkCore;

namespace Antital.Worker.API.Services;

public class TestModelService(Antital.WorkerDBContext context) : ITestModelService
{
    public async Task<List<TestModel>> GetAll(CancellationToken cancellationToken)
    {
        return await context.TestModels.ToListAsync(cancellationToken);
    }

    public async Task Add(string name, CancellationToken cancellationToken)
    {
        var test = TestModel.Create(name);
        context.TestModels.Add(test);
        await context.SaveChangesAsync(cancellationToken);
    }
}
