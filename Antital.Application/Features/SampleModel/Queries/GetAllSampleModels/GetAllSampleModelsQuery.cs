using BuildingBlocks.Application.Features;
using Antital.Application.ViewModels;

namespace Antital.Application.Features.SampleModel.Queries.GetAllSampleModels;

public record GetAllSampleModelsQuery(
    ) : ICommandQuery<IReadOnlyList<SampleModelViewModel>>;
