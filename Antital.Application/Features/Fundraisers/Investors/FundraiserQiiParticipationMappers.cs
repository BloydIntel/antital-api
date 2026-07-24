using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Application.Features.Fundraisers.Investors;

internal static class FundraiserQiiParticipationMappers
{
    public static IReadOnlyList<FundraiserQiiParticipationItemDto> BuildItems(
        IReadOnlyList<InvestorHolding> holdings,
        IReadOnlyList<InvestmentOrder> pendingOrders,
        IReadOnlyDictionary<int, UserInvestmentProfile> profiles)
    {
        var confirmedByUser = holdings
            .GroupBy(h => h.UserId)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    Amount = g.Sum(h => h.InvestedAmount),
                    CommittedAt = g.Max(h => h.InvestedAt),
                    User = g.First().User,
                });

        var pendingByUser = pendingOrders
            .GroupBy(o => o.UserId)
            .Where(g => !confirmedByUser.ContainsKey(g.Key))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.OrderByDescending(o => o.CreatedAt).First();
                    return new
                    {
                        Amount = latest.TotalAmount,
                        CommittedAt = latest.CreatedAt,
                        Currency = latest.Currency,
                        User = latest.User,
                    };
                });

        var items = new List<FundraiserQiiParticipationItemDto>();

        foreach (var (userId, row) in confirmedByUser.OrderByDescending(kv => kv.Value.CommittedAt))
        {
            profiles.TryGetValue(userId, out var profile);
            items.Add(new FundraiserQiiParticipationItemDto(
                userId,
                ResolveInstitution(profile, row.User),
                ResolveType(profile),
                row.Amount,
                "NGN",
                row.CommittedAt,
                "confirmed"));
        }

        foreach (var (userId, row) in pendingByUser.OrderByDescending(kv => kv.Value.CommittedAt))
        {
            profiles.TryGetValue(userId, out var profile);
            items.Add(new FundraiserQiiParticipationItemDto(
                userId,
                ResolveInstitution(profile, row.User),
                ResolveType(profile),
                row.Amount,
                string.IsNullOrWhiteSpace(row.Currency) ? "NGN" : row.Currency,
                row.CommittedAt,
                "pending"));
        }

        return items;
    }

    private static string ResolveInstitution(UserInvestmentProfile? profile, User? user)
    {
        if (!string.IsNullOrWhiteSpace(profile?.CompanyLegalName))
        {
            return profile.CompanyLegalName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(profile?.TradingBrandName))
        {
            return profile.TradingBrandName.Trim();
        }

        if (user == null)
        {
            return "Institutional Investor";
        }

        if (!string.IsNullOrWhiteSpace(user.PreferredName))
        {
            return user.PreferredName.Trim();
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
    }

    private static string ResolveType(UserInvestmentProfile? profile)
    {
        if (profile == null)
        {
            return "Institutional Investor";
        }

        var raw = profile.QiiInstitutionTypes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return "Institutional Investor";
        }

        if (Enum.TryParse<QiiInstitutionType>(raw, ignoreCase: true, out var parsed))
        {
            if (parsed == QiiInstitutionType.OtherRegulatedInstitution
                && !string.IsNullOrWhiteSpace(profile.QiiOtherInstitutionType))
            {
                return profile.QiiOtherInstitutionType.Trim();
            }

            return ToTypeLabel(parsed);
        }

        return raw;
    }

    private static string ToTypeLabel(QiiInstitutionType type) =>
        type switch
        {
            QiiInstitutionType.Bank => "Bank",
            QiiInstitutionType.AssetManagementCompany => "Asset Manager",
            QiiInstitutionType.PensionFundAdministrator => "Pension Fund",
            QiiInstitutionType.InsuranceCompany => "Insurance Company",
            QiiInstitutionType.VentureCapitalOrPrivateEquityFund => "Fund Manager",
            QiiInstitutionType.CorporateFinanceInstitution => "Merchant Bank",
            QiiInstitutionType.OtherRegulatedInstitution => "Other Institution",
            _ => type.ToString(),
        };
}
