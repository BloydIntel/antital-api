namespace Antital.Application.Features.Investments.Paystack;

public static class PaystackReferenceGenerator
{
    public static string CreateForOrder(int orderId) =>
        $"ANT-ORD-{orderId}-{Guid.NewGuid():N}";

    public static string CreateForApplicationFee(int userId) =>
        $"ANT-FEE-{userId}-{Guid.NewGuid():N}";

    public static bool TryParseApplicationFeeUserId(string? reference, out int userId)
    {
        userId = 0;
        if (string.IsNullOrWhiteSpace(reference))
        {
            return false;
        }

        var parts = reference.Split('-', StringSplitOptions.RemoveEmptyEntries);
        // ANT-FEE-{userId}-{guid}
        if (parts.Length < 4
            || !string.Equals(parts[0], "ANT", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(parts[1], "FEE", StringComparison.OrdinalIgnoreCase)
            || !int.TryParse(parts[2], out userId)
            || userId <= 0)
        {
            return false;
        }

        return true;
    }

    public static bool IsApplicationFeeReference(string? reference) =>
        !string.IsNullOrWhiteSpace(reference)
        && reference.StartsWith("ANT-FEE-", StringComparison.OrdinalIgnoreCase);
}
