using MVC = Microsoft.AspNetCore.Mvc;

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
        public async Task<string> GenerateLinkCode(string discordId)
        {
            return await services.users.GenerateAuthCode(discordId) ?? "";
        }
        
        [HttpGetBypass("bot/verify")]
        public async Task<dynamic> LinkDiscord(string linkcode)
        {
            try
            {
                await services.users.LinkDiscordAccount(linkcode, safeUserSession.userId);
            }
            catch(Exception e)
            {
                Console.WriteLine($"Something went wrong while linking the account{e.Message.ToString()}");
                return "Something went wrong while trying to link your account";
            }
            return "Successfully linked your account";
        }
    }
}