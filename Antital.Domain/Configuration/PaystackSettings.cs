namespace Antital.Domain.Configuration;

public class PaystackSettings
{
    public const string SectionName = "Paystack";

    public string SecretKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public decimal PlatformFeePercent { get; set; } = 2.5m;
    public int OrderExpiryMinutes { get; set; } = 30;
}
