using BuildingBlocks.Application.Features;
using Antital.Domain.Enums;

namespace Antital.Application.Features.SampleModel.Commands.CreateSampleModel;

public record CreateSampleModelCommand(
    string FirstName,
    string LastName,
    int Age,
    GenderEnum Gender,
    string Address
    ) : ICommandQuery;