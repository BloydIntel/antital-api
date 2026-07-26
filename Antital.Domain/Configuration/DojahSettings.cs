namespace Antital.Domain.Configuration;

public class DojahSettings
{
    public const string SectionName = "Dojah";

    /// <summary>Flat env alias: Dojah_AppId</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>Flat env alias: Dojah_PublicKey</summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>Flat env alias: Dojah_PrivateKey (Authorization header secret)</summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// EasyOnboard widget id for the JS SDK (<c>config.widget_id</c>).
    /// Flat env alias: Dojah_WidgetId
    /// </summary>
    public string WidgetId { get; set; } = string.Empty;

    /// <summary>
    /// Flat env alias: Dojah_BaseUrl.
    /// Sandbox: https://sandbox.dojah.io — Live: https://api.dojah.io
    /// </summary>
    public string BaseUrl { get; set; } = "https://sandbox.dojah.io";

    /// <summary>When false, KYC provider calls are skipped (local/dev fallback).</summary>
    public bool Enabled { get; set; }
}
