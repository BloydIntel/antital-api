namespace Antital.Application.Features.Investments.Paystack;

public static class PaystackReferenceGenerator
{
    public static string CreateForOrder(int orderId) =>
        $"ANT-ORD-{orderId}-{Guid.NewGuid():N}";
}
