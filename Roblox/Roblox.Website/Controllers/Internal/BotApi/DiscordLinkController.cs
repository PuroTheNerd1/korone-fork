using MVC = Microsoft.AspNetCore.Mvc;

namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class DiscordLink: ControllerBase
    {
        private bool IsBot()
        {
            var botKey = Request.Headers.ContainsKey("PJX-BOTAUTH") ? Request.Headers["PJX-BOTAUTH"].ToString() : null;
            var isBot = botKey == "ljbHjhLvOwPGasmd1qBoa4qkkbcqa1tT39BImr5SvZFbqQXi133GruGL2O2U06906ezZ8pmwEAv33SM5KmWk";
            return isBot;
        }
        [HttpGetBypass("bot/generatecode")]
        public async Task<dynamic> GenerateLinkCode(string discordId)
        {
            if(!IsBot())
            {
                return BadRequest();
            }
            string authCode = await services.users.GenerateAuthCode(discordId);
            return authCode;
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