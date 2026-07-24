namespace Antital.Application.DTOs.Fundraisers;

public record FundraiserQiiParticipationItemDto(
    int Id,
    string Institution,
    string Type,
    decimal CommitmentAmount,
    string Currency,
    DateTime CommittedAt,
    string Status);

public record FundraiserQiiParticipationResponse(
    int? OfferingId,
    IReadOnlyList<FundraiserQiiParticipationItemDto> Items);
