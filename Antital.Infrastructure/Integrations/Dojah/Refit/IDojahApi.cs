using System.Text.Json.Serialization;
using Refit;

namespace Antital.Infrastructure.Integrations.Dojah.Refit;

public interface IDojahApi
{
    [Get("/api/v1/kyc/bvn/full")]
    Task<HttpResponseMessage> LookupBvnAsync([AliasAs("bvn")] string bvn, CancellationToken cancellationToken = default);

    [Get("/api/v1/kyc/nin")]
    Task<HttpResponseMessage> LookupNinAsync([AliasAs("nin")] string nin, CancellationToken cancellationToken = default);

    [Get("/api/v1/kyc/passport")]
    Task<HttpResponseMessage> LookupPassportAsync(
        [AliasAs("passport_number")] string passportNumber,
        [AliasAs("surname")] string surname,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/kyc/dl")]
    Task<HttpResponseMessage> LookupDriversLicenceAsync(
        [AliasAs("license_number")] string licenseNumber,
        CancellationToken cancellationToken = default);

    [Post("/api/v1/ml/liveness/")]
    Task<HttpResponseMessage> CheckLivenessAsync(
        [Body] DojahLivenessRequest request,
        CancellationToken cancellationToken = default);

    [Get("/api/v1/kyc/verification")]
    Task<HttpResponseMessage> GetWidgetVerificationAsync(
        [AliasAs("reference_id")] string referenceId,
        CancellationToken cancellationToken = default);
}

public sealed record DojahLivenessRequest(
    [property: JsonPropertyName("image")] string Image);
