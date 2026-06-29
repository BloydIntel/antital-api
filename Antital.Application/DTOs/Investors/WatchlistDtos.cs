namespace Antital.Application.DTOs.Investors;

public record WatchlistItemDto(
    int OfferingId,
    string Slug,
    string Name,
    string Sector,
    string Risk,
    int? DaysLeft,
    int FundingProgressPercent,
    decimal ChangePercent,
    DateTime AddedAt,
    string? RecentUpdate,
    DateTime? RecentUpdateAt,
    int RemindersCount);

public record WatchlistCountsDto(
    int All,
    int EndingSoon,
    int NearTarget);

public record WatchlistResponse(
    IReadOnlyList<WatchlistItemDto> Items,
    WatchlistCountsDto Counts);

public record AddToWatchlistRequest(int OfferingId);

public record WatchlistStatusResponse(bool IsWatchlisted);
