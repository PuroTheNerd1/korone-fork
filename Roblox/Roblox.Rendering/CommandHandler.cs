namespace Roblox.Rendering;

public static class CommandHandler
{
    public static void Configure(string baseUrl, string authorization) => RenderHttpClient.Configure(baseUrl, authorization);

    private static async Task<Stream> SendAsync(RenderRequest request, CancellationToken? cancellationToken)
    {
        var result = await RenderHttpClient.SendAsync(request, cancellationToken ?? CancellationToken.None);
        return new MemoryStream(Convert.FromBase64String(result.Data), writable: false);
    }

    public static Task<Stream> RequestPlayerThumbnail(AvatarData data, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest
        {
            Kind = RenderKind.Avatar,
            UserId = data.userId,
            Avatar = data,
            AvatarRigType = string.Equals(data.playerAvatarType, "R6", StringComparison.OrdinalIgnoreCase)
                ? AvatarRigType.R6
                : AvatarRigType.R15,
        }, cancellationToken);

    public static Task<Stream> RequestPlayerHeadshot(AvatarData data, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest { Kind = RenderKind.AvatarHeadshot, UserId = data.userId, Avatar = data }, cancellationToken);

    public static Task<Stream> RequestTextureThumbnail(long assetId, int assetTypeId, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest { Kind = RenderKind.Texture, AssetId = assetId, AssetTypeId = assetTypeId }, cancellationToken);

    public static Task<Stream> RequestAssetThumbnail(long assetId, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest { Kind = RenderKind.Asset, AssetId = assetId }, cancellationToken);

    public static Task<Stream> RequestAssetMesh(long assetId, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest { Kind = RenderKind.Mesh, AssetId = assetId }, cancellationToken);

    public static Task<Stream> RequestPlaceConversion(string base64EncodedPlace, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest { Kind = RenderKind.PlaceConversion, InputData = base64EncodedPlace }, cancellationToken);

    public static Task<Stream> RequestHatConversion(string base64EncodedHat, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest { Kind = RenderKind.HatConversion, InputData = base64EncodedHat }, cancellationToken);

    public static Task<Stream> RequestAssetGame(long assetId, int x, int y, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest { Kind = RenderKind.Place, AssetId = assetId, Width = x, Height = y }, cancellationToken);

    public static Task<Stream> RequestAssetTeeShirt(long assetId, long contentId, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest { Kind = RenderKind.TeeShirt, AssetId = assetId, ContentId = contentId }, cancellationToken);
}
