using Microsoft.AspNetCore.Mvc;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/badges/v2")]
public class BadgesControllerV2 : ControllerBase
{
    // base: https://apidocs.sixteensrc.zip/badges/docs.html#/

    // Gets badge by their awarding game. (except v2?)
    [HttpGet("universes/{universeId:long}/badges")]
    public async Task GetUniverseBadges(long universeId)
    {
        
    }
}