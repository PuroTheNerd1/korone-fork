using System.Data.Common;
using System.Diagnostics;
using System.Net;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Roblox.Logging;
using System.Dynamic;
using Newtonsoft.Json.Linq;
// ReSharper disable InconsistentNaming
#pragma warning disable IDE1006 // Naming Styles
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace Roblox.Libraries.DiscordApi;


public class DiscordBotApi
{
    private class DiscordHttpClient : HttpClient
    {
        public DiscordHttpClient(string authorization) : base(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All })
        {
            this.BaseAddress = new Uri("https://discord.com/api/v10/");
            DefaultRequestHeaders.Add("Authorization", "Bot " + authorization );
        }
        public void ChangeAuthorizationToken(string token)
        {
            DefaultRequestHeaders.Remove("Authorization");
            DefaultRequestHeaders.Add("Authorization", "Bot " + token );
        }
    }
    private DiscordHttpClient discordClient;
    public DiscordBotApi (string token)
    {
        discordClient = new(token);
    }

    public async Task AddGuildMember(string guildId, string discordId)
    {
        var result = await discordClient.PutAsync($"guilds/${guildId}/members/${discordId}", null);
        if (result.IsSuccessStatusCode)
        {
            Writer.Info(LogGroup.DiscordApi, "Succcessfully added {0} to Pekora", discordId);
        }
        else
        {
            Writer.Info(LogGroup.DiscordApi, "Failed to add {0} to pekora status: {1}", discordId, result.StatusCode);
        }
    }

}