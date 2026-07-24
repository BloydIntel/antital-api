using Antital.Application.Features.Investments.ProcessPaystackWebhook;
using Antital.Application.Features.Onboarding.ApplicationFeePayment;
using FluentAssertions;
using Moq;
using Xunit;

namespace Antital.Test.Application.Features.Investments;

public class ProcessPaystackWebhookCommandHandlerTests
{
    private readonly Mock<IInvestmentPaymentConfirmationService> _paymentConfirmationService = new();
    private readonly Mock<IApplicationFeePaymentConfirmationService> _applicationFeeConfirmationService = new();

    [Fact]
    public async Task Handle_MissingEvent_ReturnsIgnoredWithoutCallingPaymentService()
    {
        var handler = new ProcessPaystackWebhookCommandHandler(
            _paymentConfirmationService.Object,
            _applicationFeeConfirmationService.Object);
        const string payload = """{"data":{"reference":"ANT-ORD-1-abc","amount":102500}}""";

        var result = await handler.Handle(new ProcessPaystackWebhookCommand(payload), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        _paymentConfirmationService.VerifyNoOtherCalls();
        _applicationFeeConfirmationService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_MissingData_ReturnsIgnoredWithoutCallingPaymentService()
    {
        var handler = new ProcessPaystackWebhookCommandHandler(
            _paymentConfirmationService.Object,
            _applicationFeeConfirmationService.Object);
        const string payload = """{"event":"charge.success"}""";

        var result = await handler.Handle(new ProcessPaystackWebhookCommand(payload), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        _paymentConfirmationService.VerifyNoOtherCalls();
        _applicationFeeConfirmationService.VerifyNoOtherCalls();
    }
}
