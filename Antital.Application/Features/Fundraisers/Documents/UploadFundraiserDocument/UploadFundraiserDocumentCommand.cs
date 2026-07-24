using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Fundraisers.Documents.UploadFundraiserDocument;

public record UploadFundraiserDocumentCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long Length,
    string Title,
    string Category
) : ICommandQuery<FundraiserDocumentDto>;

public class UploadFundraiserDocumentCommandHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository,
    IFundraiserDocumentsRepository documentsRepository,
    IFileUploadService fileUploadService,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<UploadFundraiserDocumentCommand, FundraiserDocumentDto>
{
    public async Task<Result<FundraiserDocumentDto>> Handle(
        UploadFundraiserDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var title = request.Title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            title = Path.GetFileNameWithoutExtension(request.FileName)?.Trim() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new BadRequestException(
                "Title is required.",
                new Dictionary<string, string[]> { ["title"] = ["Title is required."] });
        }

        if (!FundraiserDocumentsMappers.TryParseCategory(request.Category, out var category))
        {
            throw new BadRequestException(
                "Invalid category.",
                new Dictionary<string, string[]>
                {
                    ["category"] = ["Category must be Core, Financial, Analytics, or Regulatory."]
                });
        }

        if (request.Length <= 0)
        {
            throw new BadRequestException(
                "Invalid file.",
                new Dictionary<string, string[]> { ["file"] = ["File is required."] });
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

        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);
        if (offering == null)
        {
            throw new NotFoundException("No owned fundraising campaign found.");
        }

        var uploaded = await fileUploadService.UploadAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            folder: $"antital/offerings/{offering.Id}/documents",
            cancellationToken);

        var document = new OfferingDocument
        {
            OfferingId = offering.Id,
            Title = title.Length > 300 ? title[..300] : title,
            FileUrl = uploaded.Url,
            DocumentType = FundraiserDocumentsMappers.ToLegacyDocumentType(category),
            Category = category,
            ReviewStatus = DocumentReviewStatus.PendingApproval,
            FileSizeBytes = uploaded.Bytes > 0 ? uploaded.Bytes : request.Length,
            ContentType = uploaded.ContentType,
            CloudinaryPublicId = uploaded.PublicId,
        };
        document.Created(ResolveActor());

        await documentsRepository.AddAsync(document, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result<FundraiserDocumentDto>();
        result.AddValue(FundraiserDocumentsMappers.ToDto(document));
        result.OK();
        return result;
    }

    private string ResolveActor() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
}
