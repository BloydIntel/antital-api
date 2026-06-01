using Antital.Application.DTOs.Investments;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetOfferingDocuments;

public record GetOfferingDocumentsQuery(string IdOrSlug) : ICommandQuery<IReadOnlyList<OfferingDocumentDto>>;

public class GetOfferingDocumentsQueryHandler(
    InvestmentOfferingAccess offeringAccess,
    IInvestmentOfferingRepository repository)
    : ICommandQueryHandler<GetOfferingDocumentsQuery, IReadOnlyList<OfferingDocumentDto>>
{
    public async Task<Result<IReadOnlyList<OfferingDocumentDto>>> Handle(
        GetOfferingDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var offeringId = await offeringAccess.RequirePublishedOfferingIdAsync(request.IdOrSlug, cancellationToken);
        var documents = await repository.GetDocumentsAsync(offeringId, cancellationToken);
        var dtos = documents.Select(InvestmentMappers.ToDocumentDto).ToList();

        var result = new Result<IReadOnlyList<OfferingDocumentDto>>();
        result.AddValue(dtos);
        result.OK();
        return result;
    }
}
