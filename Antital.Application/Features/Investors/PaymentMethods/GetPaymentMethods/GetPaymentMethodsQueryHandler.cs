using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investors;
using Antital.Application.Features.Investors.PaymentMethods;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.PaymentMethods.GetPaymentMethods;

public class GetPaymentMethodsQueryHandler(
    IInvestorUserAccess investorUserAccess,
    IInvestorPaymentMethodRepository paymentMethodRepository
) : ICommandQueryHandler<GetPaymentMethodsQuery, PaymentMethodsResponse>
{
    public async Task<Result<PaymentMethodsResponse>> Handle(
        GetPaymentMethodsQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);
        var methods = await paymentMethodRepository.ListByUserAsync(userId, cancellationToken);
        var items = methods.Select(PaymentMethodMapper.ToItem).ToList();

        var response = new PaymentMethodsResponse(items);
        var result = new Result<PaymentMethodsResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
