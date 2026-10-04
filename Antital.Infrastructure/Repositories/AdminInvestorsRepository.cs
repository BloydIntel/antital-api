using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Antital.Infrastructure.Repositories;

public sealed class AdminInvestorsRepository(AntitalDBContext context) : IAdminInvestorsRepository
{
    private IQueryable<User> Investors() => context.Users.AsNoTracking().Where(x => !x.IsDeleted && x.Role != UserRoleEnum.Admin && (x.UserType == UserTypeEnum.IndividualInvestor || x.UserType == UserTypeEnum.CorporateInvestor));

    public async Task<AdminInvestorListResult> ListAsync(AdminInvestorQueryOptions options, CancellationToken cancellationToken = default)
    {
        var query = Investors();
        if (Enum.TryParse<InvestorAccountStatus>(options.Status, true, out var accountStatus)) query = query.Where(x => x.AccountStatus == accountStatus);
        if (Enum.TryParse<InvestorKycStatus>(options.KycStatus, true, out var kycStatus)) query = query.Where(x => context.UserKycs.Any(k => k.UserId == x.Id && !k.IsDeleted && k.ReviewStatus == kycStatus));
        if (options.HighNetWorth == true) query = query.Where(x => context.InvestorWallets.Any(w => w.UserId == x.Id && !w.IsDeleted && w.AvailableBalance >= 1_000_000m));
        if (!string.IsNullOrWhiteSpace(options.Search)) query = query.Where(x => (x.FirstName + " " + x.LastName).Contains(options.Search) || x.Email.Contains(options.Search) || x.Id.ToString() == options.Search);
        if (options.From.HasValue) query = query.Where(x => x.CreatedAt >= options.From.Value);
        if (options.To.HasValue) query = query.Where(x => x.CreatedAt < options.To.Value.AddDays(1));
        var total = await query.CountAsync(cancellationToken);
        var pending = await Investors().CountAsync(x => context.UserKycs.Any(k => k.UserId == x.Id && !k.IsDeleted && k.ReviewStatus == InvestorKycStatus.Pending), cancellationToken);
        var suspended = await Investors().CountAsync(x => x.AccountStatus == InvestorAccountStatus.Suspended, cancellationToken);
        var wallet = await context.InvestorWallets.Where(x => !x.IsDeleted && Investors().Select(u => u.Id).Contains(x.UserId)).SumAsync(x => (decimal?)x.AvailableBalance, cancellationToken) ?? 0;
        query = options.SortBy?.ToLowerInvariant() switch { "name" => options.Descending ? query.OrderByDescending(x => x.LastName).ThenByDescending(x => x.FirstName) : query.OrderBy(x => x.LastName).ThenBy(x => x.FirstName), "wallet" => options.Descending ? query.OrderByDescending(x => context.InvestorWallets.Where(w => w.UserId == x.Id && !w.IsDeleted).Select(w => (decimal?)w.AvailableBalance).FirstOrDefault()) : query.OrderBy(x => context.InvestorWallets.Where(w => w.UserId == x.Id && !w.IsDeleted).Select(w => (decimal?)w.AvailableBalance).FirstOrDefault()), _ => options.Descending ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt) };
        var users = await query.Skip((options.Page - 1) * options.PageSize).Take(options.PageSize).Select(x => new AdminInvestorListItem(x.Id, $"INV-{x.Id:0000}", x.FirstName, x.LastName, x.Email, x.UserType, context.InvestorWallets.Where(w => w.UserId == x.Id && !w.IsDeleted).Select(w => (decimal?)w.AvailableBalance).FirstOrDefault() ?? 0, x.CreatedAt, x.AccountStatus, context.UserKycs.Where(k => k.UserId == x.Id && !k.IsDeleted).Select(k => k.ReviewStatus).FirstOrDefault())).ToListAsync(cancellationToken);
        return new AdminInvestorListResult(await Investors().CountAsync(cancellationToken), pending, suspended, wallet, total, users);
    }

    public async Task<AdminInvestorDetail?> GetAsync(string investorId, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(investorId, out var id)) return null;
        var user = await Investors().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (user is null) return null;
        var kyc = await context.UserKycs.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id && !x.IsDeleted, cancellationToken);
        var wallet = await context.InvestorWallets.AsNoTracking().Where(x => x.UserId == id && !x.IsDeleted).Select(x => (decimal?)x.AvailableBalance).FirstOrDefaultAsync(cancellationToken) ?? 0;
        var holdings = await context.InvestorHoldings.AsNoTracking().Where(x => x.UserId == id && !x.IsDeleted).Include(x => x.Offering).OrderByDescending(x => x.CurrentValue).Select(x => new AdminInvestorHolding(x.Offering.Name, "Equity", x.InvestedAmount, x.CurrentValue, x.Returns, "Performing")).ToListAsync(cancellationToken);
        var orders = await context.InvestmentOrders.AsNoTracking().Where(x => x.UserId == id && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(20).Select(x => new AdminInvestorTransaction(x.Id, "Investment", x.TotalAmount, x.Currency, x.Status.ToString(), x.PaidAt ?? x.CreatedAt)).ToListAsync(cancellationToken);
        var suspensionActions = await context.AdminInvestorSuspensionActions.AsNoTracking().Where(x => x.UserId == id && !x.IsDeleted).OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        var documents = new[]
        {
            new AdminInvestorKycDocument("Identity", kyc?.GovernmentIdDocumentPathOrKey, kyc?.GovernmentIdVerifiedAt, !string.IsNullOrWhiteSpace(kyc?.GovernmentIdDocumentPathOrKey)),
            new AdminInvestorKycDocument("ProofOfAddress", kyc?.ProofOfAddressDocumentPathOrKey, kyc?.ProofOfAddressVerifiedAt, !string.IsNullOrWhiteSpace(kyc?.ProofOfAddressDocumentPathOrKey)),
            new AdminInvestorKycDocument("Selfie", kyc?.SelfieVerificationPathOrKey, kyc?.SelfieVerifiedAt, !string.IsNullOrWhiteSpace(kyc?.SelfieVerificationPathOrKey))
        };
        var checks = new[]
        {
            new AdminInvestorVerificationCheck("BVN Match", !string.IsNullOrWhiteSpace(kyc?.Bvn), kyc?.GovernmentIdVerifiedAt),
            new AdminInvestorVerificationCheck("Identity Document", documents[0].Available, documents[0].VerifiedAt),
            new AdminInvestorVerificationCheck("Proof of Address", documents[1].Available, documents[1].VerifiedAt),
            new AdminInvestorVerificationCheck("Liveness Check", documents[2].Available, documents[2].VerifiedAt)
        };
        var isSuspended = user.AccountStatus == InvestorAccountStatus.Suspended;
        var suspensionNote = isSuspended ? kyc?.ReviewNote : null;
        var evidence = suspensionActions.Where(x => x.Action == "str" && !string.IsNullOrWhiteSpace(x.EvidenceJson)).SelectMany(x => { try { return JsonSerializer.Deserialize<List<UploadedEvidence>>(x.EvidenceJson!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<UploadedEvidence>(); } catch (JsonException) { return new List<UploadedEvidence>(); } }).Select(x => x.Url).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        var suspension = new AdminInvestorSuspensionReview(isSuspended, isSuspended ? user.UpdatedAt : null, suspensionNote ?? "Account activity requires compliance review.", suspensionNote ?? "The account is restricted while compliance review is in progress.", isSuspended ? new[] { "Account restricted pending compliance review" } : Array.Empty<string>(), suspensionActions.Where(x => x.Action is "note" or "contact" or "str").Select(x => x.Note).ToArray(), evidence, 0m, isSuspended, suspensionActions.Any(x => x.Action == "str"));
        return new AdminInvestorDetail(id, $"INV-{id:0000}", user.FirstName, user.LastName, user.Email, user.CreatedAt, user.PhoneNumber, user.DateOfBirth, user.CountryOfResidence, user.StateOfResidence, user.ResidentialAddress, user.UserType, user.AccountStatus, kyc?.ReviewStatus ?? InvestorKycStatus.Pending, kyc?.ReviewNote, kyc?.ReviewedAt, wallet, holdings.Sum(x => x.Amount), holdings.Count, holdings.Sum(x => x.Returns), holdings, orders, new AdminInvestorKycReview(kyc?.Bvn, kyc?.Nin, documents, checks), suspension);
    }

    private sealed record UploadedEvidence(string Url);

    public async Task<AdminInvestorDetail?> UpdateAsync(string investorId, AdminInvestorMutation mutation, string updatedBy, CancellationToken cancellationToken = default)
    {
        if (!TryParseId(investorId, out var id)) return null;
        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        if (user is null) return null;
        if (mutation.Suspended.HasValue) user.AccountStatus = mutation.Suspended.Value ? InvestorAccountStatus.Suspended : InvestorAccountStatus.Active;
        var kyc = await context.UserKycs.FirstOrDefaultAsync(x => x.UserId == id && !x.IsDeleted, cancellationToken);
        if (!string.IsNullOrWhiteSpace(mutation.KycStatus) && Enum.TryParse<InvestorKycStatus>(mutation.KycStatus, true, out var status))
        {
            kyc ??= new UserKyc { UserId = id };
            kyc.ReviewStatus = status; kyc.ReviewNote = mutation.Note; kyc.ReviewedAt = DateTime.UtcNow;
            if (kyc.Id == 0) context.UserKycs.Add(kyc);
        }
        if (mutation.SuspensionAction is "note" or "contact" or "str")
        {
            if (!string.IsNullOrWhiteSpace(mutation.RequestId) && await context.AdminInvestorSuspensionActions.AnyAsync(x => x.UserId == id && !x.IsDeleted && x.RequestId == mutation.RequestId, cancellationToken))
                return await GetAsync(investorId, cancellationToken);
            var action = new AdminInvestorSuspensionAction { UserId = id, Action = mutation.SuspensionAction, Note = mutation.Note ?? string.Empty, EvidenceJson = mutation.EvidenceJson };
            action.RequestId = mutation.RequestId;
            action.Created(updatedBy);
            context.AdminInvestorSuspensionActions.Add(action);
            kyc ??= new UserKyc { UserId = id };
            kyc.ReviewNote = mutation.Note;
            kyc.ReviewedAt = DateTime.UtcNow;
            if (kyc.Id == 0) context.UserKycs.Add(kyc);
        }
        user.Updated(updatedBy); await context.SaveChangesAsync(cancellationToken); return await GetAsync(investorId, cancellationToken);
    }

    private static bool TryParseId(string value, out int id) => int.TryParse(value.Replace("INV-", "", StringComparison.OrdinalIgnoreCase), out id);
}
