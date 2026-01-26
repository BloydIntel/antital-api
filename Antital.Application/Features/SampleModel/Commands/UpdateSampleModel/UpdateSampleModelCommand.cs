using BuildingBlocks.Application.Features;
using Antital.Domain.Enums;

namespace Antital.Application.Features.SampleModel.Commands.UpdateSampleModel;

public record UpdateSampleModelCommand(
    int Id,
    string FirstName,
    string LastName,
    int Age,
    GenderEnum Gender,
    string Address
    ) : ICommandQuery;