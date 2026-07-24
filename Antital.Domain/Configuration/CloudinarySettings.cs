namespace Antital.Domain.Configuration;

public class CloudinarySettings
{
    public const string SectionName = "Cloudinary";

    /// <summary>Flat env alias: Cloudinary_Cloud_Name</summary>
    public string CloudName { get; set; } = string.Empty;

    /// <summary>Flat env alias: Cloudinary_API_Key</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Flat env alias: Cloudinary_API_Secret</summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>Flat env alias: Cloudinary_FolderName</summary>
    public string FolderName { get; set; } = "antital";
}
