namespace Roblox.Dto.Assets;

public class BatchAssetRequest
{
    public long? assetId { get; set; }
    public string assetType { get; set; }
    public long? requestId { get; set; }
}