using Antital.Domain.Interfaces;

namespace Antital.Test.Fakes;

public class FakeFileUploadService : IFileUploadService
{
    public Func<string, string, string?, FileUploadResult>? UploadHandler { get; set; }

    public Task<FileUploadResult> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string? folder = null,
        CancellationToken cancellationToken = default)
    {
        if (UploadHandler != null)
        {
            return Task.FromResult(UploadHandler(fileName, contentType, folder));
        }

        var publicId = $"test/{Guid.NewGuid():N}";
        return Task.FromResult(
            new FileUploadResult(
                $"https://res.cloudinary.com/test/raw/upload/{publicId}/{fileName}",
                publicId,
                content.CanSeek ? content.Length : 0,
                contentType,
                fileName,
                contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "image" : "raw"));
    }
}
