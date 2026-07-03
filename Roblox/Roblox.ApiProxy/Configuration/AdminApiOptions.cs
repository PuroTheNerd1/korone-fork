namespace Roblox.ApiProxy.Configuration;

public sealed class AdminApiOptions
{
    public const string SectionName = "AdminApi";

    public string PublicBaseUrl { get; set; } = "https://admin.pekora.zip/v1/";

    public string[] CorsAllowedOrigins { get; set; } =
    [
        "https://www.pekora.zip",
        "https://pekora.zip",
        "http://localhost:3000",
        "http://localhost:5200",
    ];
}
