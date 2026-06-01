using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Resources;

namespace Antital.Application.Features.Investments;

public class InvestmentOfferingAccess(IInvestmentOfferingRepository repository)
{
    public async Task<int> RequirePublishedOfferingIdAsync(string idOrSlug, CancellationToken cancellationToken)
    {
        var offeringId = await repository.GetPublishedOfferingIdAsync(idOrSlug, cancellationToken);
        if (!offeringId.HasValue)
        {
            throw new NotFoundException(Messages.NotFound);
        }

        return offeringId.Value;
    }
}
