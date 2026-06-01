using Antital.Domain.Enums;
using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

public class MediaAsset : TrackableEntity
{
    public int OfferingId { get; set; }
    public MediaAssetType AssetType { get; set; }
    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public virtual InvestmentOffering Offering { get; set; } = null!;
}
