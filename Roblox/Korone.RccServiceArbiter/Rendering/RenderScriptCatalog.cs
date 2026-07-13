using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Korone.RccServiceArbiter.Configuration;
using Korone.RccServiceArbiter.Rcc;
using Microsoft.Extensions.Options;
using Roblox.Rendering;

namespace Korone.RccServiceArbiter.Rendering;

public sealed class RenderScriptCatalog : IRenderScriptCatalog
{
    private readonly ArbiterOptions _options;
    private readonly IReadOnlyDictionary<string, string> _scripts;

    public RenderScriptCatalog(IOptions<ArbiterOptions> options)
    {
        _options = options.Value;
        _scripts = typeof(RenderScriptCatalog).Assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith("Korone.RccServiceArbiter.RenderScripts.", StringComparison.Ordinal))
            .ToDictionary(name => name, ReadResource, StringComparer.OrdinalIgnoreCase);
    }

    public ScriptExecution Create(RenderRequest request)
    {
        if (_options.Render.DefaultYear >= 2018)
        {
            return new ScriptExecution
            {
                Name = request.Kind.ToString(),
                Script = CreateModernJson(request),
                Arguments = Array.Empty<LuaValue>(),
            };
        }

        var scriptName = ScriptName(request.Kind);
        var script = GetScript(scriptName);
        return new ScriptExecution
        {
            Name = request.Kind.ToString(),
            Script = script,
            Arguments = BuildArguments(request),
        };
    }

    private string CreateModernJson(RenderRequest request)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var assetUrl = request.AssetUrl ?? AssetUrl(request.AssetId);
        var appearanceUrl = request.CharacterAppearanceUrl ?? $"{baseUrl}/v1.1/avatar-fetch?placeId=0&userId={request.UserId}";
        var format = request.Kind == RenderKind.Avatar3D ? "OBJ" : "PNG";
        var type = request.Kind switch
        {
            RenderKind.Avatar or RenderKind.Avatar3D => "Avatar_R15_Action",
            RenderKind.AvatarHeadshot => "Closeup",
            RenderKind.Asset or RenderKind.Model => "Model",
            RenderKind.Texture => request.IsFace ? "Face" : "Image",
            RenderKind.TeeShirt => "TeeShirt",
            RenderKind.Hat => "Hat",
            RenderKind.Head => "Head",
            RenderKind.Mesh => "Mesh",
            RenderKind.MeshPart => "MeshPart",
            RenderKind.Package => "Package",
            RenderKind.BodyPart => "BodyPart",
            RenderKind.Clothing => "Shirt",
            RenderKind.Place => "Place",
            RenderKind.Animation => "AvatarAnimation",
            RenderKind.AnimationSilhouette => "AnimationSilhouette",
            _ => throw new RenderValidationException($"Render kind {request.Kind} is not an RCC JSON thumbnail operation"),
        };
        object?[] arguments = request.Kind switch
        {
            RenderKind.Avatar or RenderKind.Avatar3D => [baseUrl, appearanceUrl, format, request.Kind == RenderKind.Avatar3D ? 352 : request.Width, request.Kind == RenderKind.Avatar3D ? 352 : request.Height, true, 30, 100, 0, 0],
            RenderKind.AvatarHeadshot => [baseUrl, appearanceUrl, "PNG", request.Width, request.Height, true, 40, 60, 0, 0],
            RenderKind.Texture => [request.AssetId ?? 0, baseUrl, "PNG", request.Width, request.Height, true, 0, 0, 0, 0],
            RenderKind.Head => [assetUrl, "PNG", request.Width, request.Height, baseUrl, 420, true, 0, 0, 0, 0],
            RenderKind.Package => [request.AssetUrls ?? string.Empty, baseUrl, "PNG", request.Width, request.Height, AssetUrl(1785197), string.Empty, true, 0, 0, 0, 0],
            RenderKind.BodyPart => [assetUrl, baseUrl, "PNG", request.Width, request.Height, AssetUrl(1785197), string.Empty],
            RenderKind.Clothing => [assetUrl, "PNG", request.Width, request.Height, baseUrl, 1785197, true, 0, 0, 0, 0],
            RenderKind.Place => [assetUrl, "PNG", request.Width, request.Height, baseUrl, request.AssetId ?? 0, baseUrl, 1],
            RenderKind.Animation => [request.CharacterAppearanceUrl, baseUrl, "PNG", request.Width, request.Height, request.AnimationUrl],
            RenderKind.AnimationSilhouette => [assetUrl, baseUrl, request.Width, request.Height],
            _ => [assetUrl, "PNG", request.Width, request.Height, baseUrl, true, 0, 0, 0, 0],
        };
        var template = JsonNode.Parse(GetScript(ModernTemplateName(request.Kind)))?.AsObject()
            ?? throw new RenderExecutionException($"Modern render template for {request.Kind} is invalid");
        var settings = template["Settings"]?.AsObject()
            ?? throw new RenderExecutionException($"Modern render template for {request.Kind} has no Settings object");
        settings["Type"] = type;
        settings["Arguments"] = JsonSerializer.SerializeToNode(arguments);
        return template.ToJsonString();
    }

    private static string ModernTemplateName(RenderKind kind) => kind switch
    {
        RenderKind.Avatar or RenderKind.Avatar3D => "Avatar.json",
        RenderKind.AvatarHeadshot => "Closeup.json",
        RenderKind.Asset or RenderKind.Model => "Model.json",
        RenderKind.Texture => "Image.json",
        RenderKind.TeeShirt => "Image.json",
        RenderKind.Hat => "Hat.json",
        RenderKind.Head => "Head.json",
        RenderKind.Mesh => "Mesh.json",
        RenderKind.MeshPart => "MeshPart.json",
        RenderKind.Package => "Package.json",
        RenderKind.BodyPart => "BodyPart.json",
        RenderKind.Clothing => "Clothing.json",
        RenderKind.Place => "Place.json",
        RenderKind.Animation => "AvatarAnimation.json",
        RenderKind.AnimationSilhouette => "AnimationSilhouette.json",
        _ => throw new RenderValidationException($"Render kind {kind} has no modern JSON template"),
    };

    private IReadOnlyList<LuaValue> BuildArguments(RenderRequest request)
    {
        var assetUrl = request.AssetUrl ?? AssetUrl(request.AssetId);
        var appearance = request.CharacterAppearanceUrl ??
                         $"{_options.BaseUrl.TrimEnd('/')}/v1.1/avatar-fetch?userId={request.UserId}";
        var common = new[] { String(assetUrl), String("PNG"), Number(request.Width), Number(request.Height), String(_options.BaseUrl) };
        return request.Kind switch
        {
            RenderKind.Avatar => [String(appearance), String(_options.BaseUrl), String("PNG"), Number(request.Width), Number(request.Height)],
            RenderKind.AvatarHeadshot => [String(_options.BaseUrl), String(appearance), String("PNG"), Number(request.Width), Number(request.Height), Bool(true), Number(30), Number(100), Number(0), Number(0)],
            RenderKind.Head or RenderKind.Clothing => [.. common, Number(420)],
            RenderKind.Place => [.. common, Number(request.AssetId ?? 0)],
            RenderKind.Package => [String(request.AssetUrls ?? string.Empty), String(_options.BaseUrl), String("PNG"), Number(request.Width), Number(request.Height), String(AssetUrl(1785197)), String(string.Empty)],
            RenderKind.Animation => [String(request.CharacterAppearanceUrl ?? string.Empty), String(request.AnimationUrl ?? string.Empty), String("PNG"), Number(request.Width), Number(request.Height), String(_options.BaseUrl)],
            _ => common,
        };
    }

    private static string ScriptName(RenderKind kind) => kind switch
    {
        RenderKind.Avatar or RenderKind.Avatar3D => "AvatarScript.lua",
        RenderKind.AvatarHeadshot => "AvatarHeadShotScript.lua",
        RenderKind.Texture => "FaceScript.lua",
        RenderKind.Hat => "HatScript.lua",
        RenderKind.Head => "HeadScript.lua",
        RenderKind.Mesh => "MeshScript.lua",
        RenderKind.MeshPart => "MeshPartScript.lua",
        RenderKind.Model or RenderKind.Asset or RenderKind.Animation or RenderKind.AnimationSilhouette => "ModelScript.lua",
        RenderKind.Package or RenderKind.BodyPart => "PackageScript.lua",
        RenderKind.Clothing => "ShirtScript.lua",
        RenderKind.Place => "PlaceScript.lua",
        RenderKind.TeeShirt => "FaceScript.lua",
        _ => throw new RenderValidationException($"Render kind {kind} is not an RCC thumbnail operation"),
    };

    private string AssetUrl(long? assetId) => $"{_options.BaseUrl.TrimEnd('/')}/asset/?id={assetId ?? 0}";
    private string GetScript(string fileName) => _scripts.FirstOrDefault(pair => pair.Key.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)).Value
        ?? throw new RenderExecutionException($"Embedded render script {fileName} was not found");
    private static LuaValue String(string value) => new() { Type = LuaType.LUA_TSTRING, Value = value };
    private static LuaValue Number(long value) => new() { Type = LuaType.LUA_TNUMBER, Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture) };
    private static LuaValue Bool(bool value) => new() { Type = LuaType.LUA_TBOOLEAN, Value = value ? "true" : "false" };
    private static string ReadResource(string name)
    {
        using var stream = typeof(RenderScriptCatalog).Assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
