using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.SampleModel.Queries.GetSampleModelTotalCount;

public record GetSampleModelTotalCountQuery(
    ) : ICommandQuery<int>;