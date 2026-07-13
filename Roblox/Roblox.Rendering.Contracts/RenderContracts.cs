using System.Text.Json.Serialization;

// Shared wire contracts only. Keep this assembly independent of clients and hosts.

namespace Roblox.Rendering;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RenderKind
{
    Avatar,
    AvatarHeadshot,
    Avatar3D,
    Asset,
    Texture,
    TeeShirt,
    Hat,
    Head,
    Mesh,
    MeshPart,
    Model,
    Package,
    BodyPart,
    Clothing,
    Place,
    Animation,
    AnimationSilhouette,
    PlaceConversion,
    HatConversion,
}

public sealed class RenderRequest
{
    public RenderKind Kind { get; set; }
    public long? AssetId { get; set; }
    public long? UserId { get; set; }
    public int? AssetTypeId { get; set; }
    public long? ContentId { get; set; }
    public int Width { get; set; } = 420;
    public int Height { get; set; } = 420;
    public bool IsFace { get; set; }
    public string? AssetUrl { get; set; }
    public string? AssetUrls { get; set; }
    public string? CharacterAppearanceUrl { get; set; }
    public string? AnimationUrl { get; set; }
    public string? InputData { get; set; }
    public AvatarData? Avatar { get; set; }
}

public sealed class RenderResult
{
    public Guid JobId { get; set; }
    public string ContentType { get; set; } = "image/png";
    public string Data { get; set; } = string.Empty;
    public IReadOnlyList<string> DependencyUrls { get; set; } = Array.Empty<string>();
}

public sealed class RenderErrorResponse
{
    public IReadOnlyList<RenderError> Errors { get; set; } = Array.Empty<RenderError>();
}

public sealed class RenderError
{
    public int Code { get; set; }
    public string Message { get; set; } = string.Empty;
}
