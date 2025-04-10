using System.Data.Common;
using System.Diagnostics;
using System.Net;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Roblox.Logging;
using System.Dynamic;
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace Roblox.Libraries.DiscordApi;

public class DiscordApi
{
    private class DiscordHttpClient : HttpClient
    {
        public DiscordHttpClient(string authorization) : base(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            this.BaseAddress = new Uri("https://discord.com/api/");
            DefaultRequestHeaders.Add("Authorization", "Bearer " + authorization );
        }
        public void ChangeAuthorizationToken(string token)
        {
            DefaultRequestHeaders.Remove("Authorization");
            DefaultRequestHeaders.Add("Authorization", "Bearer " + token );
        }
    }
    private DiscordHttpClient discordClient;
    public string accessToken { get; set; }
    public string refreshToken { get; set; }
    public int expiresIn { get; set; }
    public DiscordApi(string codeoOrToken, bool isToken)
    {
        discordClient = new(isToken ? codeoOrToken : Configuration.DiscordOAuthToken);
        // Only authorize when we have a proper token
        if (isToken)
        {
            try
            {
                var discordToken = RequestAccessToken(codeoOrToken, false).Result;
                accessToken =  discordToken.accessToken;
                refreshToken = discordToken.refreshToken;
                expiresIn = discordToken.expiresIn;
                discordClient.ChangeAuthorizationToken(accessToken);
            }
            catch (Exception e)
            {
                Writer.Info(LogGroup.DiscordApi, e.Message);
                throw;
            }
        }
        else 
        {
            accessToken = codeoOrToken;
        }
    }

    public async Task<DiscordMember?> GetGuildMember(ulong guildId)
    {
        var result = await discordClient.GetAsync($"users/@me/guilds/{guildId}/member");
        if (result.IsSuccessStatusCode)
        {
            string body = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DiscordMember>(body);
        }
        else
        {
            Writer.Info(LogGroup.DiscordApi, "GetGuildMember failed with {0}", result.StatusCode);
            return null;
        }
    }

    public async Task<DiscordUser?> GetUserInfo()
    {
        var result = await discordClient.GetAsync("users/@me");
        if (result.IsSuccessStatusCode)
        {
            string body = await result.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DiscordUser>(body);
        }
        else
        {
            Writer.Info(LogGroup.DiscordApi, "GetUserInfo failed with {0}", result.StatusCode);
            return null;
        }
    }
    // we should have a better way to check if the OAuth token is valid
    public async Task<bool> IsValid()
    {
        return await GetUserInfo() != null;
    }
    private async Task<DiscordTokenResponse> RequestAccessToken(string codeOrToken, bool useRefreshToken)
    {
        Dictionary<string, string> data = new Dictionary<string, string>
        {
            {"client_id", Configuration.DiscordClientId.ToString()},
            {"client_secret", Configuration.DiscordClientSecret.ToString()},
            {"grant_type", "authorization_code"},
            {"redirect_uri", $"https://www.{Configuration.ShortBaseUrl}/api/callback"},
            {"scope", "identify guilds"},
            {useRefreshToken ? "refresh_token" : "code", codeOrToken}
        };
        var content = new FormUrlEncodedContent(data);

        var response = await discordClient.PostAsync("oauth2/token", content);
        string body = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            return JsonConvert.DeserializeObject<DiscordTokenResponse>(body) ?? throw new Exception($"Unknown error: {body}");
        }
        
        throw new Exception($"An error occured while requesting access token body: {body}");
    }

}