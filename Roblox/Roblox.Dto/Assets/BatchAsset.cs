namespace Roblox.Dto.Assets;

public class BatchAssetRequest
{
    public long? AssetId { get; set; }
    public string AssetType { get; set; }
    public long? RequestId { get; set; }
}