namespace Antital.Application.DTOs.Files;

public record FileUploadDto(
    string Url,
    string PublicId,
    long Bytes,
    string ContentType,
    string OriginalFileName,
    string ResourceType);
