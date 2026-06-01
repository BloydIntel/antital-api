using Antital.Application.DTOs.Investments;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Resources;

namespace Antital.Application.Features.Investments.GetOfferingShell;

public class GetOfferingShellQueryHandler(IInvestmentOfferingRepository repository)
    : ICommandQueryHandler<GetOfferingShellQuery, OfferingShellResponse>
{
    public async Task<Result<OfferingShellResponse>> Handle(GetOfferingShellQuery request, CancellationToken cancellationToken)
    {
        var offering = await repository.GetPublishedShellByIdOrSlugAsync(request.IdOrSlug, cancellationToken);
        if (offering?.Funding == null || offering.DealTerms == null)
        {
            throw new NotFoundException(Messages.NotFound);
        }

        var response = InvestmentMappers.ToShellResponse(offering);

        var result = new Result<OfferingShellResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
