using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Roblox.Models.Studio;
namespace Roblox.Website.Controllers;
[ApiController]
[Route("/v1")]
public class DevelopStudio : ControllerBase
{
    [HttpGetBypass("gametemplates")]
    public dynamic StudioTemplates()
    {
        var Templates = new
        {
            gameTemplateType = "Generic",
            hasTutorials = false,
            universe = new Universe
            {
                id = 221,
                name = "Baseplate",
                description = null,
                isArchived = false,
                rootPlaceId = 4430,
                isActive = true,
                privacyType = "Public",
                creatorType = "User",
                creatorTargetId = 3,
                creatorName = "shikataganai",
                created = DateTime.Parse("2013-11-01T08:47:14.07Z"),
                updated = DateTime.Parse("2023-05-02T22:03:01.107Z")
            }
        };
        var data = new { data = new[] { Templates } };
        string json = JsonConvert.SerializeObject(data);
        return json; 
    }
}