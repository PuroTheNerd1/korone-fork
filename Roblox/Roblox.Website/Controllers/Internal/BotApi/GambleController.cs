using MVC = Microsoft.AspNetCore.Mvc;

namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class GambleBot: ControllerBase
    {
        [BotAuthorization]
        [HttpGetBypass("bot/coinflip")]
        public async Task<dynamic> CoinFlip(string discordid)
        {
            Random random = new Random();
            double chance = random.NextDouble();
            if (chance < 0.4)
            {
                return "Heads";
            }
            else
            {
                return "Tails";
            }
        }
    }
}