using BuildingBlocks.Application.Features;
using Antital.Application.ViewModels;

namespace Antital.Application.Features.SampleModel.Queries.GetSampleModelById;

public record GetSampleModelByIdQuery(
    int Id
    ) : ICommandQuery<SampleModelViewModel>;
