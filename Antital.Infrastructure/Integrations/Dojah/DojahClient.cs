using System.Text.Json;
using Antital.Domain.Configuration;
using Antital.Domain.Interfaces;
using Antital.Infrastructure.Integrations.Dojah.Refit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Antital.Infrastructure.Integrations.Dojah;

public sealed class DojahClient(
    IDojahApi dojahApi,
    IOptions<DojahSettings> options,
    ILogger<DojahClient> logger
) : IDojahClient
{
    public Task<DojahIdentityLookupResult> LookupBvnAsync(
        string bvn,
        CancellationToken cancellationToken = default)
    {
        if (!IsElevenDigits(bvn))
        {
            return Task.FromResult(
                DojahIdentityLookupResult.Fail(400, null, "BVN must be exactly 11 digits."));
        }

        return SendIdentityAsync(
            ct => dojahApi.LookupBvnAsync(bvn.Trim(), ct),
            "BVN lookup",
            cancellationToken);
    }

    public Task<DojahIdentityLookupResult> LookupNinAsync(
        string nin,
        CancellationToken cancellationToken = default)
    {
        if (!IsElevenDigits(nin))
        {
            return Task.FromResult(
                DojahIdentityLookupResult.Fail(400, null, "NIN must be exactly 11 digits."));
        }

        return SendIdentityAsync(
            ct => dojahApi.LookupNinAsync(nin.Trim(), ct),
            "NIN lookup",
            cancellationToken);
    }

    public Task<DojahIdentityLookupResult> LookupPassportAsync(
        string passportNumber,
        string surname,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(passportNumber))
        {
            return Task.FromResult(
                DojahIdentityLookupResult.Fail(400, null, "Passport number is required."));
        }

        if (string.IsNullOrWhiteSpace(surname))
        {
            return Task.FromResult(
                DojahIdentityLookupResult.Fail(400, null, "Surname is required for passport lookup."));
        }

        return SendIdentityAsync(
            ct => dojahApi.LookupPassportAsync(passportNumber.Trim(), surname.Trim(), ct),
            "passport lookup",
            cancellationToken);
    }

    public Task<DojahIdentityLookupResult> LookupDriversLicenceAsync(
        string licenseNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(licenseNumber))
        {
            return Task.FromResult(
                DojahIdentityLookupResult.Fail(400, null, "Driver's licence number is required."));
        }

        return SendIdentityAsync(
            ct => dojahApi.LookupDriversLicenceAsync(licenseNumber.Trim(), ct),
            "driver's licence lookup",
            cancellationToken);
    }

    public Task<DojahLookupResult> CheckLivenessAsync(
        string imageBase64,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeBase64Image(imageBase64);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Task.FromResult(new DojahLookupResult(false, 400, null, "image Base64 is required."));
        }

        return SendRawAsync(
            ct => dojahApi.CheckLivenessAsync(new DojahLivenessRequest(normalized), ct),
            "liveness check",
            cancellationToken);
    }

    public async Task<DojahWidgetVerificationResult> GetWidgetVerificationAsync(
        string referenceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referenceId) || referenceId.Trim().Length <= 10)
        {
            return DojahWidgetVerificationResult.Fail(
                400,
                null,
                "Reference id must be longer than 10 characters.");
        }

        var raw = await SendRawAsync(
            ct => dojahApi.GetWidgetVerificationAsync(referenceId.Trim(), ct),
            "widget verification lookup",
            cancellationToken);

        if (!raw.IsSuccess)
        {
            return DojahWidgetVerificationResult.Fail(
                raw.StatusCode,
                raw.RawBody,
                raw.ErrorMessage ?? "Unable to fetch Dojah verification.");
        }

        return ParseWidgetVerification(raw, referenceId.Trim());
    }

    internal static DojahWidgetVerificationResult ParseWidgetVerification(
        DojahLookupResult raw,
        string fallbackReferenceId)
    {
        if (string.IsNullOrWhiteSpace(raw.RawBody))
        {
            return DojahWidgetVerificationResult.Fail(raw.StatusCode, raw.RawBody, "Dojah returned an empty body.");
        }

        try
        {
            using var document = JsonDocument.Parse(raw.RawBody);
            var root = document.RootElement;

            var payload = root;
            if (root.TryGetProperty("entity", out var entity)
                && entity.ValueKind is JsonValueKind.Object)
            {
                payload = entity;
            }

            var overallStatus = ReadBool(payload, "status") ?? ReadBool(root, "status") ?? false;
            var verificationStatus = ReadString(
                payload,
                "verification_status",
                "verificationStatus");
            var resolvedReferenceId =
                ReadString(payload, "reference_id", "referenceId")
                ?? fallbackReferenceId;
            var selfieUrl = ReadString(payload, "selfie_url", "selfieUrl");

            var selfiePassed = false;
            if (TryGetPropertyIgnoreCase(payload, "data", out var data)
                && TryGetPropertyIgnoreCase(data, "selfie", out var selfie)
                && ReadBool(selfie, "status") == true)
            {
                selfiePassed = true;
                selfieUrl ??= ReadNestedString(selfie, "data", "selfie_url")
                    ?? ReadNestedString(selfie, "data", "selfieUrl");
            }

            var statusCompleted = IsCompletedVerificationStatus(verificationStatus);
            var completed = overallStatus || statusCompleted || selfiePassed;

            if (!completed)
            {
                return DojahWidgetVerificationResult.Fail(
                    raw.StatusCode,
                    raw.RawBody,
                    $"Dojah verification is not complete (status={verificationStatus ?? "unknown"}).");
            }

            return new DojahWidgetVerificationResult(
                true,
                raw.StatusCode,
                resolvedReferenceId,
                verificationStatus,
                overallStatus || statusCompleted,
                selfiePassed || statusCompleted || overallStatus,
                selfieUrl,
                raw.RawBody,
                null);
        }
        catch (JsonException)
        {
            return DojahWidgetVerificationResult.Fail(raw.StatusCode, raw.RawBody, "Dojah response was not valid JSON.");
        }
    }

    private static bool IsCompletedVerificationStatus(string? verificationStatus) =>
        !string.IsNullOrWhiteSpace(verificationStatus)
        && (string.Equals(verificationStatus, "Completed", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verificationStatus, "Successful", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verificationStatus, "Approved", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verificationStatus, "Success", StringComparison.OrdinalIgnoreCase));

    private static bool? ReadBool(JsonElement element, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (!TryGetPropertyIgnoreCase(element, name, out var property))
            {
                continue;
            }

            return property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(property.GetString(), out var parsed) => parsed,
                _ => null,
            };
        }

        return null;
    }

    private static string? ReadNestedString(JsonElement parent, string childName, string propertyName)
    {
        if (!TryGetPropertyIgnoreCase(parent, childName, out var child))
        {
            return null;
        }

        return ReadString(child, propertyName);
    }

    private async Task<DojahIdentityLookupResult> SendIdentityAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> send,
        string operationName,
        CancellationToken cancellationToken)
    {
        var raw = await SendRawAsync(send, operationName, cancellationToken);
        if (!raw.IsSuccess)
        {
            return DojahIdentityLookupResult.Fail(raw.StatusCode, raw.RawBody, raw.ErrorMessage ?? "Dojah lookup failed.");
        }

        return ParseIdentityEntity(raw);
    }

    private async Task<DojahLookupResult> SendRawAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> send,
        string operationName,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.AppId) || string.IsNullOrWhiteSpace(settings.PrivateKey))
        {
            return new DojahLookupResult(
                false,
                0,
                null,
                "Dojah AppId or PrivateKey is not configured.");
        }

        try
        {
            using var response = await send(cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Dojah {Operation} failed with {StatusCode}: {Body}",
                    operationName,
                    (int)response.StatusCode,
                    Truncate(body));

                return new DojahLookupResult(
                    false,
                    (int)response.StatusCode,
                    body,
                    $"Dojah returned {(int)response.StatusCode}.");
            }

            return new DojahLookupResult(true, (int)response.StatusCode, body, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dojah {Operation} request failed.", operationName);
            return new DojahLookupResult(false, 0, null, ex.Message);
        }
    }

    internal static DojahIdentityLookupResult ParseIdentityEntity(DojahLookupResult raw)
    {
        if (string.IsNullOrWhiteSpace(raw.RawBody))
        {
            return DojahIdentityLookupResult.Fail(raw.StatusCode, raw.RawBody, "Dojah returned an empty body.");
        }

        try
        {
            using var document = JsonDocument.Parse(raw.RawBody);
            if (!document.RootElement.TryGetProperty("entity", out var entity) ||
                entity.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                return DojahIdentityLookupResult.Fail(
                    raw.StatusCode,
                    raw.RawBody,
                    "Dojah response did not include an entity.");
            }

            var firstName = ReadString(entity, "first_name", "firstName");
            var middleName = ReadString(entity, "middle_name", "middleName", "other_names", "otherNames");
            var lastName = ReadString(entity, "last_name", "lastName", "surname");
            var dateOfBirth = ReadString(entity, "date_of_birth", "dateOfBirth", "birthDate", "birth_date");
            var photo = ReadString(entity, "photo", "image");

            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                return DojahIdentityLookupResult.Fail(
                    raw.StatusCode,
                    raw.RawBody,
                    "Dojah entity did not include a usable name.");
            }

            return new DojahIdentityLookupResult(
                true,
                raw.StatusCode,
                firstName,
                middleName,
                lastName,
                dateOfBirth,
                photo,
                raw.RawBody,
                null);
        }
        catch (JsonException)
        {
            return DojahIdentityLookupResult.Fail(raw.StatusCode, raw.RawBody, "Dojah response was not valid JSON.");
        }
    }

    private static string? ReadString(JsonElement entity, params string[] propertyNames)
    {
        foreach (var name in propertyNames)
        {
            if (!TryGetPropertyIgnoreCase(entity, name, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                var value = property.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }
        }

        return null;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string name, out JsonElement property)
    {
        if (element.TryGetProperty(name, out property))
        {
            return true;
        }

        foreach (var candidate in element.EnumerateObject())
        {
            if (string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        property = default;
        return false;
    }

    private static bool IsElevenDigits(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Trim().Length == 11
        && value.Trim().All(char.IsDigit);

    private static string? NormalizeBase64Image(string? imageBase64)
    {
        if (string.IsNullOrWhiteSpace(imageBase64))
        {
            return null;
        }

        var value = imageBase64.Trim();
        var commaIndex = value.IndexOf(',');
        if (value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex > 0)
        {
            value = value[(commaIndex + 1)..];
        }

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string Truncate(string? value, int max = 500)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= max ? value : value[..max] + "…";
    }
}
