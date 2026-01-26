using BuildingBlocks.Application.Features;
using BuildingBlocks.Application.ViewModels;

namespace Antital.Application.Features.SampleModel.Queries.GetGenderEnum;

public record GetGenderEnumQuery(
    ) : ICommandQuery<IList<EnumViewModel>>;
