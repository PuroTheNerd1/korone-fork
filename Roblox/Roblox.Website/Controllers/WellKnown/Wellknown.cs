using MVC = Microsoft.AspNetCore.Mvc;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class WellKnown: ControllerBase
    {
        [HttpGetBypass(".well-known/discord")]
        public string WellKnownDiscord()
        {
            return "No link for u";
        }
    }
}
