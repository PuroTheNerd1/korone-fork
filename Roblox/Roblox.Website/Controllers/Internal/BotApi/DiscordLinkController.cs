using MVC = Microsoft.AspNetCore.Mvc;
using Roblox.Libraries.DiscordApi;
namespace Roblox.Website.Controllers
{
    /* 
        This needs to be improved because this is buggy and unstable
    */
    [MVC.ApiController]
    [MVC.Route("/")]
    public class DiscordLink: ControllerBase
    {
        [BotAuthorization]
        [HttpGetBypass("bot/generatecode")]
        public async Task<string> GenerateLinkCode()
        {
            return $"Head to https://www.{Configuration.ShortBaseUrl}/bot/verify to link your account";
        }
        
        [HttpGetBypass("bot/verify")]
        public async Task<dynamic> LinkDiscord(string? code)
        {
            if (await services.users.IsUserLinked(safeUserSession.userId))
            {
                return "You have already linked your discord account to Pekora";
            }
            // if there isnt a code we will redirect it to the oauth link to get the code
            if (code == null)
            {
                return Redirect("https://discord.com/oauth2/authorize?client_id=1359582890232516618&response_type=code&redirect_uri=https%3A%2F%2Fwww.pekora.zip%2Fbot%2Fverify&scope=identify+guilds.members.read+guilds.join");
            }
            DiscordApi discordOAuth = new(code, false, $"https://www.{Configuration.ShortBaseUrl}/bot/verify");
            var userInfo = await discordOAuth.GetUserInfo();
            if (userInfo == null)
            {
                return "Invalid Discord Account";
            }
            await services.users.LinkDiscordAccount(userInfo.Id.ToString(), safeUserSession.userId);
            // just incase
            await services.discordBotApi.AddGuildMember(Configuration.DiscordGuildId, userInfo.Id.ToString(), discordOAuth.accessToken);
            return "You have linked your account to Pekora";
        }
    }
}