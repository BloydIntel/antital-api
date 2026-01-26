using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using Antital.Domain.Interfaces;
using StackExchange.Redis;

namespace Antital.Application.Features.SampleModel.Commands.DeleteSampleModel;

public class DeleteSampleModelCommandHandler(
    IAntitalUnitOfWork unitOfWork,
    IConnectionMultiplexer connectionMultiplexer
    ) : ICommandQueryHandler<DeleteSampleModelCommand>
{
    private readonly IDatabase redisDatabase = connectionMultiplexer.GetDatabase();

    public async Task<Result> Handle(DeleteSampleModelCommand request, CancellationToken cancellationToken)
    {
        var existEntity = await unitOfWork.SampleModelRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(BuildingBlocks.Resources.Messages.NotFound);

        await unitOfWork.SampleModelRepository.DeleteAsync(existEntity, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await redisDatabase.KeyDeleteAsync("SampleModelTotalCount");

        var result = new Result();
        result.OK();
        return result;
    }
}