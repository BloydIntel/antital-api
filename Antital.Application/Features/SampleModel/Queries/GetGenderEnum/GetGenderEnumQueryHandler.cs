using BuildingBlocks.Application.Features;
using BuildingBlocks.Application.ViewModels;
using Antital.Domain.Enums;

namespace Antital.Application.Features.SampleModel.Queries.GetGenderEnum;

public class GetGenderEnumQueryHandler() : 
    GetEnumQueryHandler<GetGenderEnumQuery, GenderEnum>,
    ICommandQueryHandler<GetGenderEnumQuery, IList<EnumViewModel>>
{
}
