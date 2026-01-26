using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.AnotherSampleModel.Queries.GetAnotherSampleModelTotalCount;

public record GetAnotherSampleModelTotalCountQuery(
) : ICommandQuery<int>;