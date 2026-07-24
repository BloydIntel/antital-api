using Antital.Application.DTOs.Files;
using Antital.Application.Features.Files.UploadFile;
using Antital.Domain.Interfaces;
using BuildingBlocks.API.Controllers;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Antital.API.Controllers;

[SwaggerTag("Files")]
[Route("api/files")]
[Authorize]
[ApiController]
public class FilesController(IMediator mediator) : BaseController
{
    [HttpPost("upload")]
    [RequestSizeLimit(FileUploadLimits.MaxFileBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileUploadLimits.MaxFileBytes)]
    [SwaggerOperation(
        "Upload File",
        "Uploads a file to Cloudinary via the shared storage service. Reusable by documents, onboarding, and other flows.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FileUploadDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid file", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        IFormFile? file,
        [FromForm] string? folder,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            throw new BadRequestException(
                "Invalid file.",
                new Dictionary<string, string[]> { ["file"] = ["File is required."] });
        }

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(
            new UploadFileCommand(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                folder),
            cancellationToken);
        return ApiResult(result);
    }
}
