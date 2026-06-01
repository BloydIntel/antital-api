using Antital.Application.DTOs.Investments;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetOfferingTestimonials;

public record GetOfferingTestimonialsQuery(string IdOrSlug) : ICommandQuery<IReadOnlyList<TestimonialDto>>;

public class GetOfferingTestimonialsQueryHandler(
    InvestmentOfferingAccess offeringAccess,
    IInvestmentOfferingRepository repository)
    : ICommandQueryHandler<GetOfferingTestimonialsQuery, IReadOnlyList<TestimonialDto>>
{
    public async Task<Result<IReadOnlyList<TestimonialDto>>> Handle(
        GetOfferingTestimonialsQuery request,
        CancellationToken cancellationToken)
    {
        var offeringId = await offeringAccess.RequirePublishedOfferingIdAsync(request.IdOrSlug, cancellationToken);
        var testimonials = await repository.GetTestimonialsAsync(offeringId, cancellationToken);
        var dtos = testimonials.Select(InvestmentMappers.ToTestimonialDto).ToList();

        var result = new Result<IReadOnlyList<TestimonialDto>>();
        result.AddValue(dtos);
        result.OK();
        return result;
    }
}
