using BuildingBlocks.Application.Features;
using Antital.Domain.Interfaces;

namespace Antital.Application.Features.AnotherSampleModel.Queries.GetAnotherSampleModelTotalCount;

public class GetAnotherSampleModelTotalCountQueryHandler(IAntitalUnitOfWork unitOfWork) : ICommandQueryHandler<GetAnotherSampleModelTotalCountQuery, int>
{
    public async Task<Result<int>> Handle(GetAnotherSampleModelTotalCountQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await unitOfWork.AnotherSampleModelRepository.GetTotalCount(cancellationToken);

        var result = new Result<int>();
        result.AddValue(totalCount);
        result.OK();
        return result;
    }
}