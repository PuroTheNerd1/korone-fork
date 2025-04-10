using Newtonsoft.Json;
namespace Roblox.Libraries.DiscordApi;
public class DiscordTokenResponse
{
    [JsonProperty("access_token")]
    public string accessToken { get; set; }

    [JsonProperty("token_type")]
    public string tokenType { get; set; }

    [JsonProperty("expires_in")]
    public int expiresIn { get; set; }

    [JsonProperty("refresh_token")]
    public string refreshToken { get; set; }

    [JsonProperty("scope")]
    public string scope { get; set; }
}