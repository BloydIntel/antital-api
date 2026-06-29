using Antital.Application.Features.Investments.ProcessPaystackWebhook;
using Antital.Infrastructure.Integrations.Paystack;
using BuildingBlocks.API.Controllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Antital.API.Controllers;

[SwaggerTag("Payments")]
[Route("api/payments")]
[ApiController]
public class PaymentsController(
    IMediator mediator,
    PaystackSignatureValidator signatureValidator) : BaseController
{
    [HttpPost("paystack/webhook")]
    [AllowAnonymous]
    [SwaggerOperation(
        "Paystack Webhook",
        "Receives Paystack charge events and confirms investment orders. Requires the x-paystack-signature header.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Accepted")]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid signature")]
    public async Task<IActionResult> PaystackWebhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["x-paystack-signature"].FirstOrDefault();

        if (!signatureValidator.IsValid(rawBody, signature))
        {
            return BadRequest();
        }

        await mediator.Send(new ProcessPaystackWebhookCommand(rawBody), cancellationToken);
        return Ok();
    }
}
