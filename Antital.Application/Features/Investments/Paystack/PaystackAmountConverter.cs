namespace Antital.Application.Features.Investments.Paystack;

public static class PaystackAmountConverter
{
    public static int ToKobo(decimal nairaAmount) =>
        (int)Math.Round(nairaAmount * 100m, 0, MidpointRounding.AwayFromZero);

    public static decimal FromKobo(int kobo) => kobo / 100m;
}
