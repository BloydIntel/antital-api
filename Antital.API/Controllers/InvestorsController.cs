using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investors.AddToWatchlist;
using Antital.Application.Features.Investors.GetInvestorAccount;
using Antital.Application.Features.Investors.GetInvestorDashboard;
using Antital.Application.Features.Investors.GetInvestorProfile;
using Antital.Application.Features.Investors.GetWatchlist;
using Antital.Application.Features.Investors.GetWatchlistStatus;
using Antital.Application.Features.Investors.GetWalletTransaction;
using Antital.Application.Features.Investors.GetWalletTransactions;
using Antital.Application.Features.Investors.PaymentMethods.AddPaymentMethod;
using Antital.Application.Features.Investors.PaymentMethods.DeletePaymentMethod;
using Antital.Application.Features.Investors.PaymentMethods.GetPaymentMethods;
using Antital.Application.Features.Investors.PaymentMethods.SetDefaultPaymentMethod;
using Antital.Application.Features.Investors.RemoveFromWatchlist;
using Antital.Application.Features.Investors.UpdateInvestorProfile;
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

    [HttpGet("me/account")]
    [SwaggerOperation("Get Investor Account", "Returns account summary for the authenticated investor settings page.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<InvestorAccountResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    public async Task<IActionResult> GetAccount(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInvestorAccountQuery(), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/profile")]
    [SwaggerOperation("Get Investor Profile", "Returns profile and location fields for the authenticated investor.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<InvestorProfileResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInvestorProfileQuery(), cancellationToken);
        return ApiResult(result);
    }

    [HttpPut("me/profile")]
    [SwaggerOperation("Update Investor Profile", "Updates editable profile fields for the authenticated investor.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<InvestorProfileResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateInvestorProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateInvestorProfileCommand(
                request.FirstName,
                request.LastName,
                request.PreferredName,
                request.PhoneNumber,
                request.ResidentialAddress,
                request.StateOfResidence,
                request.CountryOfResidence),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/watchlist/status")]
    [SwaggerOperation("Get Watchlist Status", "Returns whether the authenticated investor has watchlisted the given offering.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<WatchlistStatusResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    public async Task<IActionResult> GetWatchlistStatus(
        [FromQuery] int offeringId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWatchlistStatusQuery(offeringId), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/watchlist")]
    [SwaggerOperation("List Watchlist", "Returns the authenticated investor watchlist with offering details and tab counts.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<WatchlistResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    public async Task<IActionResult> GetWatchlist(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWatchlistQuery(), cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("me/watchlist")]
    [SwaggerOperation("Add To Watchlist", "Adds a published investment offering to the authenticated investor watchlist.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<WatchlistItemDto>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Offering not found", typeof(void))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Already on watchlist", typeof(void))]
    public async Task<IActionResult> AddToWatchlist(
        [FromBody] AddToWatchlistRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddToWatchlistCommand(request.OfferingId), cancellationToken);
        return ApiResult(result);
    }

    [HttpDelete("me/watchlist/{offeringId:int}")]
    [SwaggerOperation("Remove From Watchlist", "Removes an offering from the authenticated investor watchlist.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Watchlist item not found", typeof(void))]
    public async Task<IActionResult> RemoveFromWatchlist(int offeringId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveFromWatchlistCommand(offeringId), cancellationToken);
        return ApiResult(result);
    }
}
