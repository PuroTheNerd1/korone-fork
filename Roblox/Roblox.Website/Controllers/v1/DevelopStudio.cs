using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Roblox.Dto.Games;
using Roblox.Models;
using Roblox.Models.Assets;
using Roblox.Models.Studio;
namespace Roblox.Website.Controllers;
[ApiController]
[Route("/v1")]
public class DevelopStudio : ControllerBase
{
    [HttpGet("gametemplates")]
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
    [HttpGet("user/universes")]
    public async Task<RobloxCollectionPaginated<GamesForCreatorEntry>> GetUserCreatedGames(long userId,
        string? sortOrder, string? accessFilter, int limit, string? cursor = null)
    {
        if (limit is > 100 or < 1) limit = 10;
        int offset = int.Parse(cursor ?? "0");
        var result =
            (await services.games.GetGamesForType(CreatorType.User, userId, limit, offset, sortOrder ?? "asc", accessFilter ?? "All")).ToList();
        return new RobloxCollectionPaginated<GamesForCreatorEntry>()
        {
            nextPageCursor = result.Count >= limit ? (offset+limit).ToString(): null,
            previousPageCursor = offset >= limit ? (offset-limit).ToString() : null,
            data = result,
        };
    }
}