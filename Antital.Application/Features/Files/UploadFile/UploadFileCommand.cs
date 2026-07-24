using Antital.Application.DTOs.Files;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Files.UploadFile;

public record UploadFileCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string? Folder = null
) : ICommandQuery<FileUploadDto>;

public class UploadFileCommandHandler(
    IFileUploadService fileUploadService
) : ICommandQueryHandler<UploadFileCommand, FileUploadDto>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "text/csv",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    };

    public async Task<Result<FileUploadDto>> Handle(
        UploadFileCommand request,
        CancellationToken cancellationToken)
    {
        Validate(request);

        var uploaded = await fileUploadService.UploadAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            request.Folder,
            cancellationToken);

        var result = new Result<FileUploadDto>();
        result.AddValue(
            new FileUploadDto(
                uploaded.Url,
                uploaded.PublicId,
                uploaded.Bytes,
                uploaded.ContentType,
                uploaded.OriginalFileName,
                uploaded.ResourceType));
        result.OK();
        return result;
    }

    private static void Validate(UploadFileCommand request)
    {
        if (request.Content is null || request.Length <= 0)
        {
            throw new BadRequestException(
                "Invalid file.",
                new Dictionary<string, string[]> { ["file"] = ["File is required."] });
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new BadRequestException(
                "Invalid file.",
                new Dictionary<string, string[]> { ["file"] = ["File name is required."] });
        }

        if (string.IsNullOrWhiteSpace(request.ContentType) || !AllowedContentTypes.Contains(request.ContentType))
        {
            throw new BadRequestException(
                "Unsupported file type.",
                new Dictionary<string, string[]>
                {
                    ["file"] =
                    [
                        "Allowed types: PDF, images (jpeg/png/webp/gif), Word, Excel, CSV."
                    ]
                });
        }

        if (request.Length > FileUploadLimits.MaxFileBytes)
        {
            throw new BadRequestException(
                "File too large.",
                new Dictionary<string, string[]>
                {
                    ["file"] =
                    [
                        $"Maximum file size is {FileUploadLimits.MaxFileBytes / (1024 * 1024)} MB."
                    ]
                });
        }
    }
}
