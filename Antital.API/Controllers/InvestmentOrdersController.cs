using Antital.Application.DTOs.Investments;
using Antital.Application.Features.Investments.GetInvestmentOrder;
using Antital.Application.Features.Investments.InitializeInvestmentPayment;
using Antital.Application.Features.Investments.VerifyInvestmentPayment;
using Antital.Application.Features.Investments.Paystack;
using Antital.Application.Features.Investments.Swagger;
using BuildingBlocks.API.Controllers;
using BuildingBlocks.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.API.Controllers;

[SwaggerTag("Investment Orders")]
[Route("api/investments/orders")]
[Authorize]
[ApiController]
public class InvestmentOrdersController(IMediator mediator) : BaseController
{
    [HttpGet("{orderId:int}")]
    [SwaggerOperation("Get Investment Order", "Returns checkout order status for polling after Paystack payment.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<GetInvestmentOrderResponse>))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(GetInvestmentOrderResponseExample))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Onboarding incomplete", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Order not found", typeof(void))]
    public async Task<IActionResult> GetOrder(int orderId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInvestmentOrderQuery(orderId), cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("{orderId:int}/pay")]
    [SwaggerOperation("Initialize Investment Payment", "Initializes Paystack checkout for a pending investment order.")]
    [SwaggerRequestExample(typeof(InitializeInvestmentPaymentRequest), typeof(InitializeInvestmentPaymentRequestExample))]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<InitializeInvestmentPaymentResponse>))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(InitializeInvestmentPaymentResponseExample))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid order or payment configuration", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Onboarding incomplete", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Order not found", typeof(void))]
    public async Task<IActionResult> InitializePayment(
        int orderId,
        [FromBody] InitializeInvestmentPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var channel = PaystackChannelMapper.ParseChannel(request.Channel);
        var result = await mediator.Send(new InitializeInvestmentPaymentCommand(orderId, channel), cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("{orderId:int}/verify")]
    [SwaggerOperation(
        "Verify Investment Payment",
        "Verifies Paystack payment status and confirms the order. Use after redirect when webhooks cannot reach localhost.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<GetInvestmentOrderResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Payment not complete or invalid order", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Onboarding incomplete", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Order not found", typeof(void))]
    public async Task<IActionResult> VerifyPayment(int orderId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new VerifyInvestmentPaymentCommand(orderId), cancellationToken);
        return ApiResult(result);
    }
}
