using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investors.GetInvestorDashboard;
using Antital.Application.Features.Investors.GetWalletTransaction;
using Antital.Application.Features.Investors.GetWalletTransactions;
using Antital.Application.Features.Investors.PaymentMethods.AddPaymentMethod;
using Antital.Application.Features.Investors.PaymentMethods.DeletePaymentMethod;
using Antital.Application.Features.Investors.PaymentMethods.GetPaymentMethods;
using Antital.Application.Features.Investors.PaymentMethods.SetDefaultPaymentMethod;
using BuildingBlocks.API.Controllers;
using BuildingBlocks.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Antital.API.Controllers;

[SwaggerTag("Investors")]
[Route("api/investors")]
[Authorize]
[ApiController]
public class InvestorsController(IMediator mediator) : BaseController
{
    [HttpGet("me/dashboard")]
    [SwaggerOperation("Get Investor Dashboard", "Returns dashboard summary, performance, watchlist preview, and holdings for the authenticated investor.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<InvestorDashboardResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid period", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string period = "this-month",
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetInvestorDashboardQuery(period), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/wallet/transactions")]
    [SwaggerOperation(
        "List Wallet Transactions",
        "Returns paginated wallet activity for the authenticated investor. V1 includes paid primary-market investments only.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<WalletTransactionsResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid filter", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    public async Task<IActionResult> GetWalletTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new GetWalletTransactionsQuery(page, pageSize, type, status, from, to),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/wallet/transactions/{transactionId:int}")]
    [SwaggerOperation(
        "Get Wallet Transaction",
        "Returns invoice detail for a paid wallet transaction owned by the authenticated investor.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<WalletTransactionInvoiceResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Transaction not found", typeof(void))]
    public async Task<IActionResult> GetWalletTransaction(int transactionId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWalletTransactionQuery(transactionId), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/payment-methods")]
    [SwaggerOperation("List Payment Methods", "Returns saved bank and card payment methods for the authenticated investor.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<PaymentMethodsResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    public async Task<IActionResult> GetPaymentMethods(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPaymentMethodsQuery(), cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("me/payment-methods")]
    [SwaggerOperation("Add Payment Method", "Adds a masked bank or card payment method for the authenticated investor.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<PaymentMethodResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    public async Task<IActionResult> AddPaymentMethod(
        [FromBody] AddPaymentMethodRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AddPaymentMethodCommand(
                request.Type,
                request.Title,
                request.ProviderName,
                request.Last4,
                request.SetAsDefault),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpPatch("me/payment-methods/{paymentMethodId:int}/default")]
    [SwaggerOperation("Set Default Payment Method", "Marks the given payment method as the investor default.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<PaymentMethodResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Payment method not found", typeof(void))]
    public async Task<IActionResult> SetDefaultPaymentMethod(int paymentMethodId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetDefaultPaymentMethodCommand(paymentMethodId), cancellationToken);
        return ApiResult(result);
    }

    [HttpDelete("me/payment-methods/{paymentMethodId:int}")]
    [SwaggerOperation("Delete Payment Method", "Removes a saved payment method for the authenticated investor.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Payment method not found", typeof(void))]
    public async Task<IActionResult> DeletePaymentMethod(int paymentMethodId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeletePaymentMethodCommand(paymentMethodId), cancellationToken);
        return ApiResult(result);
    }
}
