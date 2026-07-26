using System.Net;
using System.Text;
using Antital.Domain.Configuration;
using Antital.Domain.Interfaces;
using Antital.Infrastructure.Integrations.Dojah;
using BuildingBlocks.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Antital.Test.Infrastructure.Integrations;

public class AuditingDojahClientTests
{
    [Fact]
    public async Task LookupBvnAsync_RecordsMaskedFingerprint()
    {
        const string body = """
            {
              "entity": {
                "first_name": "JOHN",
                "last_name": "MUSA",
                "date_of_birth": "1997-05-16"
              }
            }
            """;

        var handler = new StubHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://sandbox.dojah.io/") };
        var inner = new DojahClient(
            httpClient,
            Options.Create(new DojahSettings
            {
                AppId = "test-app",
                PrivateKey = "test_sk",
                BaseUrl = "https://sandbox.dojah.io",
                Enabled = true,
            }),
            NullLogger<DojahClient>.Instance);

        ExternalProviderCheckEntry? recorded = null;
        var recorder = new Mock<IExternalProviderCheckRecorder>();
        recorder
            .Setup(r => r.RecordAsync(It.IsAny<ExternalProviderCheckEntry>(), It.IsAny<CancellationToken>()))
            .Callback<ExternalProviderCheckEntry, CancellationToken>((entry, _) => recorded = entry)
            .Returns(Task.CompletedTask);

        var currentUser = new Mock<ICurrentUser>();
        currentUser.SetupGet(c => c.UserName).Returns("42");

        var sut = new AuditingDojahClient(inner, recorder.Object, currentUser.Object);
        var result = await sut.LookupBvnAsync("22222222222");

        result.IsSuccess.Should().BeTrue();
        recorded.Should().NotBeNull();
        recorded!.Provider.Should().Be(ExternalProviderNames.Dojah);
        recorded.Operation.Should().Be(DojahOperations.BvnLookup);
        recorded.UserId.Should().Be(42);
        recorded.Success.Should().BeTrue();
        recorded.RequestFingerprint.Should().Be("*******2222");
        recorded.RequestFingerprint.Should().NotContain("22222222222");
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
