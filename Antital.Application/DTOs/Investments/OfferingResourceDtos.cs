namespace Antital.Application.DTOs.Investments;

public record HighlightDto(
    int Id,
    string Kind,
    string? Headline,
    string Body,
    int SortOrder);

public record ContentBlockItemDto(
    int Id,
    string Label,
    string Body,
    int SortOrder);

public record ContentBlockDto(
    int Id,
    string BlockType,
    string? Key,
    string? Title,
    string? Summary,
    int SortOrder,
    IReadOnlyList<ContentBlockItemDto> Items);

public record TeamMemberDto(
    int Id,
    string Name,
    string Title,
    string Bio,
    string? ImageUrl,
    int SortOrder);

public record FinancialMetricDto(
    int Id,
    string MetricName,
    string PeriodLabel,
    int PeriodSortOrder,
    decimal? Value,
    string Unit,
    string? CurrencyCode,
    string ValueType);

public record UseOfProceedsItemDto(
    int Id,
    decimal? AllocationPercent,
    string Category,
    string Description,
    int SortOrder);

public record OfferingFinancialsResponse(
    IReadOnlyList<FinancialMetricDto> Metrics,
    IReadOnlyList<UseOfProceedsItemDto> UseOfProceeds);

public record OfferingRiskDto(
    int Id,
    string Category,
    string Description,
    string Mitigation,
    int SortOrder);

public record OfferingDocumentDto(
    int Id,
    string Title,
    string FileUrl,
    string DocumentType,
    int? PageCount);

public record MediaAssetDto(
    int Id,
    string AssetType,
    string Url,
    int SortOrder);

public record OfferingUpdateDto(
    int Id,
    DateTime PublishedAt,
    string Title,
    string Body,
    int LikeCount);

public record OfferingUpdatesResponse(
    IReadOnlyList<OfferingUpdateDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public record TestimonialDto(
    int Id,
    string Quote,
    string AuthorName,
    string AuthorTitle,
    string? ImageUrl,
    int SortOrder);
