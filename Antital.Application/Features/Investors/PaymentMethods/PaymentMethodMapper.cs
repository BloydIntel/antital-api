using Antital.Application.DTOs.Investors;
using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Application.Features.Investors.PaymentMethods;

internal static class PaymentMethodMapper
{
    public static PaymentMethodItemDto ToItem(InvestorPaymentMethod method) =>
        new(
            method.Id,
            method.Type.ToString(),
            method.Title,
            method.Subtitle,
            method.IsDefault,
            method.IsVerified,
            method.CreatedAt);

    public static string BuildSubtitle(InvestorPaymentMethodType type, string providerName, string last4) =>
        type switch
        {
            InvestorPaymentMethodType.Bank => $"{providerName} • ********{last4}",
            InvestorPaymentMethodType.Card => $"{providerName} ending in {last4}",
            _ => $"{providerName} • {last4}",
        };

    public static bool TryParseType(string type, out InvestorPaymentMethodType parsed)
    {
        if (Enum.TryParse(type, ignoreCase: true, out parsed)
            && Enum.IsDefined(typeof(InvestorPaymentMethodType), parsed))
        {
            return true;
        }

        parsed = default;
        return false;
    }
}
