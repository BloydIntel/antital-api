namespace Antital.Domain.Interfaces;

public static class FileUploadLimits
{
    public const long MaxFileBytes = 20 * 1024 * 1024;
}

public record FileUploadResult(
    string Url,
    string PublicId,
    long Bytes,
    string ContentType,
    string OriginalFileName,
    string ResourceType);

public interface IFileUploadService
{
    Task<FileUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default);
}
