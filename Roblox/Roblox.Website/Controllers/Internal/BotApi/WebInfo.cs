using MVC = Microsoft.AspNetCore.Mvc;

using Dapper;
using Npgsql;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class WebInfo: ControllerBase
    {
        private NpgsqlConnection db => services.assets.db;
        private bool IsBot()
        {
            var botKey = Request.Headers.ContainsKey("PJX-BOTAUTH") ? Request.Headers["PJX-BOTAUTH"].ToString() : null;
            var isBot = botKey == "ljbHjhLvOwPGasmd1qBoa4qkkbcqa1tT39BImr5SvZFbqQXi133GruGL2O2U06906ezZ8pmwEAv33SM5KmWk";
            return isBot;
        }
        [HttpGetBypass("bot/status")]
        public async Task<dynamic> GetWebInfo()
        {
            if(!IsBot())
                return "Yes";
            var t = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(60));
            var OnlineCountQuery = await db.QuerySingleOrDefaultAsync("SELECT COUNT(*) as total FROM \"user\" WHERE online_at >= :t", new
            {
                t,
            });
            var IngameQuery = await db.QuerySingleOrDefaultAsync("SELECT COUNT(*) as total FROM asset_server_player", new
            {
                t,
            });

            long OnlineCount = OnlineCountQuery?.total ?? 0;
            long Ingame = IngameQuery?.total ?? 0;
            
            return new 
            {
                Online = OnlineCount,
                Ingame = Ingame
            };
        }
    }
}