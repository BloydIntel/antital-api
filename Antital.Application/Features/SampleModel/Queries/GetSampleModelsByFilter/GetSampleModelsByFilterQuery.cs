using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Enums;
using Antital.Application.ViewModels;
using Antital.Domain.Enums;

namespace Antital.Application.Features.SampleModel.Queries.GetSampleModelsByFilter;

public record GetSampleModelsByFilterQuery(
    int? MinAge,
    int? MaxAge,
    GenderEnum? Gender,
    OrderSampleModelByFilter? OrderBy,
    int PageSize = 25,
    int PageNumber = 1,
    OrderType OrderType = OrderType.Ascending
    ) : ICommandQuery<PagedList<SampleModelViewModel>>;

public enum OrderSampleModelByFilter
{
    FirstName,
    LastName,
    Age
}