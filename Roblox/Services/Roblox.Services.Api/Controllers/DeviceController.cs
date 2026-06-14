using Microsoft.AspNetCore.Mvc;
using Roblox.Web.Infrastructure.Controllers;

namespace Roblox.Services.Api.Controllers;

[ApiController]
[Route("[controller]/[action]")]
public class DeviceController : RobloxControllerBase
{
    [HttpGet]
    [HttpPost]
    public ActionResult<dynamic> Initialize()
    {
        return new
        {
            browserTrackerId = 1234567890,
            appDeviceIdentifier = (string?)null,
        };
    }
}