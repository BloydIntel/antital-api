using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.SampleModel.Commands.DeleteSampleModel;

public record DeleteSampleModelCommand(
    int Id
    ) : ICommandQuery;