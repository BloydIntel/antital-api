using System.Net;
using System.Text;
using Antital.Domain.Configuration;
using Antital.Domain.Integrations.Paystack;
using Antital.Infrastructure.Integrations.Paystack;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Antital.Test.Infrastructure.Integrations;

public class PaystackClientTests
{
    [Fact]
    public async Task InitializeTransactionAsync_ParsesSnakeCaseResponse()
    {
        const string body = """
            {
              "status": true,
              "message": "Authorization URL created",
              "data": {
                "authorization_url": "https://checkout.paystack.com/test-session",
                "access_code": "access-code-test",
                "reference": "ANT-ORD-1-abc"
              }
            }
            """;

        var handler = new StubHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });

        var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.paystack.co/") };
        var paystackClient = new PaystackClient(
            client,
            Options.Create(new PaystackSettings { SecretKey = "sk_test_key" }),
            NullLogger<PaystackClient>.Instance);

        var result = await paystackClient.InitializeTransactionAsync(
            new PaystackInitializeRequest("user@example.com", 51_250, "ANT-ORD-1-abc", "http://localhost/callback", ["card"]));

        result.Success.Should().BeTrue();
        result.AuthorizationUrl.Should().Be("https://checkout.paystack.com/test-session");
        result.AccessCode.Should().Be("access-code-test");
        result.Reference.Should().Be("ANT-ORD-1-abc");
        result.Message.Should().Be("Authorization URL created");
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
