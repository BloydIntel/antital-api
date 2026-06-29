namespace Antital.Application.Features.Investments.CreateInvestmentOrder;

internal static class InvestmentOrderCalculator
{
    public static (decimal Subtotal, decimal PlatformFee, decimal TotalAmount) Calculate(
        int units,
        decimal sharePrice,
        decimal platformFeePercent)
    {
        var subtotal = units * sharePrice;
        var platformFee = Math.Round(subtotal * platformFeePercent / 100m, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + platformFee;
        return (subtotal, platformFee, total);
    }
}
