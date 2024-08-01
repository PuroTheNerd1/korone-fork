
using MVC = Microsoft.AspNetCore.Mvc;
using Roblox.Website.Controllers.Internal;
using Microsoft.AspNetCore.Mvc;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class Telementry : ControllerBase
    {
        [HttpGetBypass("client/pbe")]
        [HttpGetBypass("mobile/pbe")]
        public OkResult PBE()
        {
            return Ok();
        }
    }
}