using System.Text.Json;
using Korone.RccServiceArbiter.Configuration;
using Korone.RccServiceArbiter.Rendering;
using Microsoft.Extensions.Options;
using Roblox.Rendering;
using Xunit;

namespace Korone.RccServiceArbiter.Tests;

public sealed class RenderScriptCatalogTests
{
    [Theory]
    [InlineData(RenderKind.Avatar, "Avatar_R15_Action", "PNG")]
    [InlineData(RenderKind.Avatar3D, "Avatar_R15_Action", "OBJ")]
    [InlineData(RenderKind.AvatarHeadshot, "Closeup", "PNG")]
    [InlineData(RenderKind.MeshPart, "MeshPart", "PNG")]
    [InlineData(RenderKind.Animation, "AvatarAnimation", "PNG")]
    public void ModernRcc_UsesJsonThumbnailPayload(RenderKind kind, string expectedType, string expectedFormat)
    {
        var catalog = CreateCatalog();
        var execution = catalog.Create(new RenderRequest
        {
            Kind = kind, AssetId = 123, UserId = 456, Width = 640, Height = 360,
            CharacterAppearanceUrl = "https://example.test/avatar", AnimationUrl = "https://example.test/animation",
        });
        using var document = JsonDocument.Parse(execution.Script);
        Assert.Equal("Thumbnail", document.RootElement.GetProperty("Mode").GetString());
        var settings = document.RootElement.GetProperty("Settings");
        Assert.Equal(expectedType, settings.GetProperty("Type").GetString());
        Assert.Contains(settings.GetProperty("Arguments").EnumerateArray(), value => value.ValueKind == JsonValueKind.String && value.GetString() == expectedFormat);
        Assert.Empty(execution.Arguments);
    }

    private static RenderScriptCatalog CreateCatalog() => new(Options.Create(new ArbiterOptions
    { BaseUrl = "https://example.test", Render = new ArbiterRenderOptions { DefaultYear = 2020 } }));
}
