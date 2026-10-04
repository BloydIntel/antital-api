using Antital.Domain.Enums;

namespace Antital.Domain.Interfaces;

public sealed record AdminInvestorQueryOptions(string? Status, string? KycStatus, bool? HighNetWorth, string? Search, DateTime? From, DateTime? To, string? SortBy, bool Descending, int Page, int PageSize);
public sealed record AdminInvestorListResult(int TotalInvestors, int PendingKyc, int SuspendedAccounts, decimal TotalWalletBalance, int TotalCount, IReadOnlyList<AdminInvestorListItem> Items);
public sealed record AdminInvestorListItem(int Id, string InvestorId, string FirstName, string LastName, string Email, UserTypeEnum UserType, decimal WalletBalance, DateTime JoinedAt, InvestorAccountStatus AccountStatus, InvestorKycStatus KycStatus);
public sealed record AdminInvestorDetail(int Id, string InvestorId, string FirstName, string LastName, string Email, DateTime JoinedAt, string PhoneNumber, DateTime DateOfBirth, string CountryOfResidence, string StateOfResidence, string ResidentialAddress, UserTypeEnum UserType, InvestorAccountStatus AccountStatus, InvestorKycStatus KycStatus, string? KycReviewNote, DateTime? KycReviewedAt, decimal WalletBalance, decimal TotalInvested, int ActivePositions, decimal EstimatedReturns, IReadOnlyList<AdminInvestorHolding> Holdings, IReadOnlyList<AdminInvestorTransaction> Transactions, AdminInvestorKycReview KycReview);
public sealed record AdminInvestorKycReview(string? Bvn, string? Nin, IReadOnlyList<AdminInvestorKycDocument> Documents, IReadOnlyList<AdminInvestorVerificationCheck> Checks);
public sealed record AdminInvestorKycDocument(string Type, string? PathOrKey, DateTime? VerifiedAt, bool Available);
public sealed record AdminInvestorVerificationCheck(string Name, bool Passed, DateTime? VerifiedAt);
public sealed record AdminInvestorHolding(string Campaign, string Instrument, decimal Amount, decimal CurrentValue, decimal Returns, string Status);
public sealed record AdminInvestorTransaction(int Id, string Type, decimal Amount, string Currency, string Status, DateTime OccurredAt);
public sealed record AdminInvestorMutation(string? KycStatus, string? Note, bool? Suspended);

public interface IAdminInvestorsRepository
{
    Task<AdminInvestorListResult> ListAsync(AdminInvestorQueryOptions options, CancellationToken cancellationToken = default);
    Task<AdminInvestorDetail?> GetAsync(string investorId, CancellationToken cancellationToken = default);
    Task<AdminInvestorDetail?> UpdateAsync(string investorId, AdminInvestorMutation mutation, string updatedBy, CancellationToken cancellationToken = default);
}
