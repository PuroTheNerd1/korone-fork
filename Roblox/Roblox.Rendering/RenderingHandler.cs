using NetVips;

namespace Roblox.Rendering;

public static class RenderingHandler
{
    public static System.Collections.Concurrent.ConcurrentDictionary<long, string> allowedPlaceForRender { get; } = new();

    public static void Configure(string baseUrl, string authorization = "") => RenderHttpClient.Configure(baseUrl, authorization);

    private static async Task<string> SendAsync(RenderRequest request, CancellationToken? cancellationToken = null)
    {
        var result = await RenderHttpClient.SendAsync(request, cancellationToken ?? CancellationToken.None);
        return result.ContentType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(result.Data))
            : result.Data;
    }

    public static Task<string> RequestHatThumbnail(long assetId) => SendAsync(new RenderRequest { Kind = RenderKind.Hat, AssetId = assetId });
    public static Task<string> RequestMeshThumbnail(long assetId) => SendAsync(new RenderRequest { Kind = RenderKind.Mesh, AssetId = assetId });
    public static Task<string> RequestMeshPartThumbnail(long assetId) => SendAsync(new RenderRequest { Kind = RenderKind.MeshPart, AssetId = assetId });
    public static Task<string> RequestModelThumbnail(long assetId) => SendAsync(new RenderRequest { Kind = RenderKind.Model, AssetId = assetId });
    public static Task<string> RequestImageThumbnail(long assetId, bool isFace = false) => SendAsync(new RenderRequest { Kind = RenderKind.Texture, AssetId = assetId, IsFace = isFace });
    public static Task<string> RequestClothingRender(long assetId) => SendAsync(new RenderRequest { Kind = RenderKind.Clothing, AssetId = assetId });
    public static Task<string> RequestTeeShirtRender(long assetId) => SendAsync(new RenderRequest { Kind = RenderKind.TeeShirt, AssetId = assetId });
    public static Task<string> RequestHeadRender(long assetId) => SendAsync(new RenderRequest { Kind = RenderKind.Head, AssetId = assetId });
    public static Task<string> RequestAnimationSilhouetteRender(long assetId) => SendAsync(new RenderRequest { Kind = RenderKind.AnimationSilhouette, AssetId = assetId });
    public static Task<string> RequestAnimationRender(string characterAppearanceUrl, string animationUrl) => SendAsync(new RenderRequest
    { Kind = RenderKind.Animation, CharacterAppearanceUrl = characterAppearanceUrl, AnimationUrl = animationUrl });
    public static Task<string> RequestPackageRender(string assetUrls) => SendAsync(new RenderRequest { Kind = RenderKind.Package, AssetUrls = assetUrls });
    public static Task<string> RequestBodyPartRender(string assetUrl) => SendAsync(new RenderRequest { Kind = RenderKind.BodyPart, AssetUrl = assetUrl });
    public static Task<string> RequestPlayerThumbnail(long userId, AvatarRigType avatarRigType, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest { Kind = RenderKind.Avatar, UserId = userId, AvatarRigType = avatarRigType, Width = 840, Height = 840 }, cancellationToken);

    public static Task<string> RequestPlayerThumbnail(long userId, CancellationToken? cancellationToken = null) =>
        RequestPlayerThumbnail(userId, AvatarRigType.R15, cancellationToken);
    public static Task<string> RequestPlayerThumbnail3D(long userId, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest { Kind = RenderKind.Avatar3D, UserId = userId, Width = 352, Height = 352 }, cancellationToken);
    public static Task<string> RequestHeadshotThumbnail(long userId, CancellationToken? cancellationToken = null) =>
        SendAsync(new RenderRequest { Kind = RenderKind.AvatarHeadshot, UserId = userId, Width = 720, Height = 720 }, cancellationToken);

    public static async Task<string> RequestPlaceRender(long assetId, int x, int y)
    {
        allowedPlaceForRender.TryAdd(assetId, string.Empty);
        return await SendAsync(new RenderRequest { Kind = RenderKind.Place, AssetId = assetId, Width = x, Height = y });
    }

    public static Task<TReturn> ResizeImage<TReturn, TImageType>(TImageType inputImage, int width, int height)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Image dimensions must be positive");
        byte[] source = inputImage switch
        {
            string value => Convert.FromBase64String(value),
            byte[] value => value,
            Stream stream when stream.CanRead => ReadStream(stream),
            _ => throw new ArgumentException("Unsupported image input type", nameof(inputImage)),
        };
        using var image = Image.ThumbnailBuffer(source, width, height: height, size: Enums.Size.Force);
        var output = image.PngsaveBuffer();
        object result = typeof(TReturn) == typeof(string) ? Convert.ToBase64String(output)
            : typeof(TReturn) == typeof(byte[]) ? output
            : typeof(TReturn) == typeof(MemoryStream) || typeof(TReturn) == typeof(Stream) ? new MemoryStream(output, writable: false)
            : throw new ArgumentException("Unsupported image return type", nameof(TReturn));
        return Task.FromResult((TReturn)result);
    }

    private static byte[] ReadStream(Stream stream)
    {
        if (stream is MemoryStream memory) return memory.ToArray();
        using var copy = new MemoryStream(); stream.CopyTo(copy); return copy.ToArray();
    }
}
