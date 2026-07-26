using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Antital.Infrastructure.Integrations;

public sealed class ExternalProviderCheckRecorder(
    AntitalDBContext dbContext,
    ICurrentUser currentUser,
    ILogger<ExternalProviderCheckRecorder> logger
) : IExternalProviderCheckRecorder
{
    public async Task RecordAsync(ExternalProviderCheckEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            var row = new ExternalProviderCheck
            {
                Provider = entry.Provider,
                Operation = entry.Operation,
                UserId = entry.UserId,
                ExternalReference = Truncate(entry.ExternalReference, 100),
                Success = entry.Success,
                StatusCode = entry.StatusCode,
                ErrorCode = Truncate(entry.ErrorCode, 100),
                RequestFingerprint = Truncate(entry.RequestFingerprint, 64),
            };

            var actor = string.IsNullOrWhiteSpace(currentUser.UserName) ? "system" : currentUser.UserName;
            row.Created(actor);

            dbContext.ExternalProviderChecks.Add(row);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Never fail the provider call because audit persistence failed.
            logger.LogError(
                ex,
                "Failed to persist ExternalProviderCheck for {Provider}/{Operation}",
                entry.Provider,
                entry.Operation);
        }
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        return value.Length <= max ? value : value[..max];
    }
}
