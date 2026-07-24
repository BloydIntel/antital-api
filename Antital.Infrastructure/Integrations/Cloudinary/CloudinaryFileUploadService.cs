using Antital.Domain.Configuration;
using Antital.Domain.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Antital.Infrastructure.Integrations.Cloudinary;

public class CloudinaryFileUploadService(
    IOptions<CloudinarySettings> options,
    ILogger<CloudinaryFileUploadService> logger
) : IFileUploadService
{
    public async Task<FileUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        var settings = options.Value;
        var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
        var cloudinary = new CloudinaryDotNet.Cloudinary(account)
        {
            Api = { Secure = true }
        };

        var targetFolder = string.IsNullOrWhiteSpace(folder)
            ? settings.FolderName
            : folder.Trim().Trim('/');

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = targetFolder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false,
        };

        var resourceType = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
            ? "image"
            : "raw";

        var uploadResult = await cloudinary.UploadAsync(uploadParams, resourceType, cancellationToken);

        if (uploadResult.Error != null || string.IsNullOrWhiteSpace(uploadResult.SecureUrl?.ToString()))
        {
            var message = uploadResult.Error?.Message ?? "Unable to upload file to storage.";
            logger.LogWarning("Cloudinary upload failed for {FileName}: {Error}", fileName, message);
            throw new InvalidOperationException($"Cloudinary upload failed: {message}");
        }

        return new FileUploadResult(
            uploadResult.SecureUrl.ToString(),
            uploadResult.PublicId,
            uploadResult.Bytes,
            contentType,
            fileName,
            uploadResult.ResourceType ?? resourceType);
    }

    private void EnsureConfigured()
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.CloudName)
            || string.IsNullOrWhiteSpace(settings.ApiKey)
            || string.IsNullOrWhiteSpace(settings.ApiSecret))
        {
            throw new InvalidOperationException(
                "Cloudinary is not configured. Set Cloudinary:CloudName/ApiKey/ApiSecret or Cloudinary_Cloud_Name/Cloudinary_API_Key/Cloudinary_API_Secret.");
        }
    }
}
