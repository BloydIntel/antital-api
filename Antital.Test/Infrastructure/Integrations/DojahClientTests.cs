using System.Net;
using System.Text;
using Antital.Domain.Configuration;
using Antital.Domain.Interfaces;
using Antital.Infrastructure.Integrations.Dojah;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Antital.Test.Infrastructure.Integrations;

public class DojahClientTests
{
    [Fact]
    public async Task LookupBvnAsync_ParsesSnakeCaseEntity()
    {
        const string body = """
            {
              "entity": {
                "bvn": "2*****234567",
                "first_name": "JOHN",
                "last_name": "MUSA",
                "middle_name": "DOE",
                "date_of_birth": "1997-05-16",
                "image": "BASE64IMG"
              }
            }
            """;

        var client = CreateClient(HttpStatusCode.OK, body);
        var result = await client.LookupBvnAsync("22222222222");

        result.IsSuccess.Should().BeTrue();
        result.FirstName.Should().Be("JOHN");
        result.LastName.Should().Be("MUSA");
        result.MiddleName.Should().Be("DOE");
        result.DateOfBirth.Should().Be("1997-05-16");
        result.PhotoBase64.Should().Be("BASE64IMG");
    }

    [Fact]
    public async Task LookupNinAsync_RejectsInvalidLength()
    {
        var client = CreateClient(HttpStatusCode.OK, "{}");
        var result = await client.LookupNinAsync("123");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.ErrorMessage.Should().Contain("11 digits");
    }

    [Fact]
    public async Task LookupPassportAsync_ParsesSurnameAsLastName()
    {
        const string body = """
            {
              "entity": {
                "passport_number": "A00123456",
                "surname": "JOHN",
                "first_name": "DOE",
                "other_names": "MOSES",
                "date_of_birth": "10/06/1993",
                "photo": "PASSPORTPHOTO"
              }
            }
            """;

        var client = CreateClient(HttpStatusCode.OK, body);
        var result = await client.LookupPassportAsync("A00123456", "JOHN");

        result.IsSuccess.Should().BeTrue();
        result.FirstName.Should().Be("DOE");
        result.LastName.Should().Be("JOHN");
        result.MiddleName.Should().Be("MOSES");
        result.DateOfBirth.Should().Be("10/06/1993");
        result.PhotoBase64.Should().Be("PASSPORTPHOTO");
    }

    [Fact]
    public async Task LookupDriversLicenceAsync_ParsesCamelCaseEntity()
    {
        const string body = """
            {
              "entity": {
                "licenseNo": "FKJ494A2133",
                "firstName": "JOHN",
                "lastName": "MUSA",
                "middleName": "",
                "birthDate": "28-09-1998",
                "photo": "DLPHOTO"
              }
            }
            """;

        var client = CreateClient(HttpStatusCode.OK, body);
        var result = await client.LookupDriversLicenceAsync("FKJ494A2133");

        result.IsSuccess.Should().BeTrue();
        result.FirstName.Should().Be("JOHN");
        result.LastName.Should().Be("MUSA");
        result.DateOfBirth.Should().Be("28-09-1998");
        result.PhotoBase64.Should().Be("DLPHOTO");
    }

    [Fact]
    public async Task LookupBvnAsync_PropagatesHttpFailure()
    {
        var client = CreateClient(HttpStatusCode.Unauthorized, """{"error":"bad key"}""");
        var result = await client.LookupBvnAsync("22222222222");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        result.RawBody.Should().Contain("bad key");
    }

    [Fact]
    public async Task LookupBvnAsync_FailsWhenEntityMissing()
    {
        var client = CreateClient(HttpStatusCode.OK, """{"message":"ok"}""");
        var result = await client.LookupBvnAsync("22222222222");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("entity");
    }

    [Fact]
    public async Task GetWidgetVerificationAsync_ParsesEntityWrappedCompletedLiveness()
    {
        const string body = """
            {
              "entity": {
                "reference_id": "ANT-KYC-12345678901",
                "verification_status": "Completed",
                "verification_mode": "LIVENESS",
                "selfie_url": "https://images.dojah.io/selfie.jpg",
                "data": {
                  "selfie": {
                    "status": true,
                    "message": "Successfully validated your liveness",
                    "data": { "selfie_url": "https://images.dojah.io/selfie.jpg" }
                  }
                }
              }
            }
            """;

        var client = CreateClient(HttpStatusCode.OK, body);
        var result = await client.GetWidgetVerificationAsync("ANT-KYC-12345678901");

        result.IsSuccess.Should().BeTrue();
        result.ReferenceId.Should().Be("ANT-KYC-12345678901");
        result.VerificationStatus.Should().Be("Completed");
        result.SelfiePassed.Should().BeTrue();
        result.SelfieUrl.Should().Be("https://images.dojah.io/selfie.jpg");
    }

    [Fact]
    public async Task GetWidgetVerificationAsync_TreatsApprovedStatusAsComplete()
    {
        const string body = """
            {
              "entity": {
                "reference_id": "ANT-KYC-abcdefghijk",
                "verificationStatus": "Approved",
                "status": true
              }
            }
            """;

        var client = CreateClient(HttpStatusCode.OK, body);
        var result = await client.GetWidgetVerificationAsync("ANT-KYC-abcdefghijk");

        result.IsSuccess.Should().BeTrue();
        result.VerificationStatus.Should().Be("Approved");
    }

    [Fact]
    public async Task GetWidgetVerificationAsync_FailsWhenIncomplete()
    {
        const string body = """
            {
              "entity": {
                "reference_id": "ANT-KYC-pending0001",
                "verification_status": "Ongoing",
                "status": false
              }
            }
            """;

        var client = CreateClient(HttpStatusCode.OK, body);
        var result = await client.GetWidgetVerificationAsync("ANT-KYC-pending0001");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Ongoing");
    }

    private static DojahClient CreateClient(HttpStatusCode statusCode, string body)
    {
        var handler = new StubHttpMessageHandler(
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://sandbox.dojah.io/") };
        return new DojahClient(
            httpClient,
            Options.Create(new DojahSettings
            {
                AppId = "test-app",
                PrivateKey = "test_sk",
                BaseUrl = "https://sandbox.dojah.io",
                Enabled = true,
            }),
            NullLogger<DojahClient>.Instance);
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
