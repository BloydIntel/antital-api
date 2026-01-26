using BuildingBlocks.Application.Features;
using Antital.Application.Specifications.SampleModel;
using Antital.Application.ViewModels;
using Antital.Domain.Interfaces;

namespace Antital.Application.Features.SampleModel.Queries.GetSampleModelsByFilter;

public class GetSampleModelsByFilterQueryHandler(IAntitalUnitOfWork unitOfWork) : ICommandQueryHandler<GetSampleModelsByFilterQuery, PagedList<SampleModelViewModel>>
{
    public async Task<Result<PagedList<SampleModelViewModel>>> Handle(GetSampleModelsByFilterQuery request, CancellationToken cancellationToken)
    {
        var specification = new GetSampleModelsByFilterSpecification(request);
        var (totalCount, data) = await unitOfWork.SampleModelRepository.ListAsync(specification, cancellationToken);

        var viewModel = data.ToViewModel();
        var pagedList = PagedList<SampleModelViewModel>.Create(request.PageSize, request.PageNumber, totalCount, viewModel);

        var result = new Result<PagedList<SampleModelViewModel>>();
        result.AddValue(pagedList);
        result.OK();
        return result;
    }
}