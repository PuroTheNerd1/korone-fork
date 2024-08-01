
using MVC = Microsoft.AspNetCore.Mvc;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class StudioScripts : ControllerBase
    {
        public string visitScript = System.IO.File.ReadAllText(@"C:\ProjectX\services\Roblox\StudioScripts\visit.txt");
        [HttpGetBypass("game/visit.ashx")]
        public async Task<dynamic> VisitStudio(int IsPlaySolo, long UserID, long universeId)
        {
            int membership;
            var membership2 = await services.users.GetUserMembership(UserID);
            if (membership2 == null)
            {
                membership = 0;
            }
            else
            {
                membership = (int)membership2!.membershipType;
            }
            string finalScript = visitScript.Replace
                ("roblox.com", $"{Configuration.BaseUrl.Replace("https://www.", "")}").Replace            
                ("%membership%", $"{membership}").Replace
                ("%userId%", $"{UserID}").Replace
                ("%universeId%", $"{universeId}");
            return services.sign.SignStringResponseForClientFromPrivateKey(finalScript, true);
        }
    }
}
