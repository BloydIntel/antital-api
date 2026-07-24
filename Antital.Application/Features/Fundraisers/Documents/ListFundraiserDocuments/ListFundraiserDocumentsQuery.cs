using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.Documents.ListFundraiserDocuments;

public record ListFundraiserDocumentsQuery : ICommandQuery<FundraiserDocumentsResponse>;

public class ListFundraiserDocumentsQueryHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository,
    IFundraiserDocumentsRepository documentsRepository
) : ICommandQueryHandler<ListFundraiserDocumentsQuery, FundraiserDocumentsResponse>
{
    public async Task<Result<FundraiserDocumentsResponse>> Handle(
        ListFundraiserDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);

        if (offering == null)
        {
            var empty = new Result<FundraiserDocumentsResponse>();
            empty.AddValue(FundraiserDocumentsMappers.Empty());
            empty.OK();
            return empty;
        }

        var documents = await documentsRepository.ListByOfferingAsync(offering.Id, cancellationToken);
        var result = new Result<FundraiserDocumentsResponse>();
        result.AddValue(
            new FundraiserDocumentsResponse(
                offering.Id,
                offering.Slug,
                documents.Select(FundraiserDocumentsMappers.ToDto).ToList()));
        result.OK();
        return result;
    }
}
