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
        return Content(json, "application/json");
    }
    [HttpGet("user/universes")]
    public async Task<RobloxCollectionPaginated<GamesForCreatorEntry>> GetUserCreatedGames(string? sortOrder, string? accessFilter, int limit, string? cursor = null)
    {
        if (limit is > 100 or < 1) limit = 10;
        int offset = int.Parse(cursor ?? "0");
        var result =
            (await services.games.GetGamesForType(CreatorType.User, userSession.userId, limit, offset, sortOrder ?? "asc", accessFilter ?? "All")).ToList();
        return new RobloxCollectionPaginated<GamesForCreatorEntry>()
        {
            nextPageCursor = result.Count >= limit ? (offset+limit).ToString(): null,
            previousPageCursor = offset >= limit ? (offset-limit).ToString() : null,
            data = result,
        };
    }
    [HttpGet("universes/{universeId}/permissions")]
    public async Task<dynamic> CanManage(long universeId)
    {
        var place = await services.games.GetRootPlaceId(universeId);
        bool canManage = await services.assets.CanUserModifyItem(userSession.userId, place);
        return new
        {
            canManage,
            canCloudEdit = canManage
        };
    }
    [HttpGet("universes/{universeId}")]    
    public async Task<dynamic> UniverseInfo(long universeId)
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] {universeId})).FirstOrDefault();
        return new
        {
            id = universeId,
            name = uni.name,
            description = uni.description,
            isArchived = false,
            rootPlaceId = uni.rootPlaceId,
            isActive = true,
            privacyType = "Public",
            creatorType = "User",
            creatorTargetId = uni.creatorId,
            creatorName = uni.creatorName,
            created = uni.created,
            updated = uni.updated
        };
    }
    [HttpGet("universes/{universeId}/configuration")]    
    public async Task<dynamic> UniverseConfiguration(long universeId)
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] {universeId})).FirstOrDefault();
        List<string> playableDevices = new List<string> 
        { 
            "Computer", 
            "Phone", 
            "Tablet", 
            "VR" 
        };
        return new
        {
            allowPrivateServers = false,
            privateServerPrice = 0,
            id = universeId,
            name = uni.name,
            universeAvatarType = "MorphToR6",
            universeScaleType = "AllScales",
            universeAnimationType = "PlayerChoice",
            universeCollisionType = "OuterBox",
            universeBodyType = "Standard",
            universeJointPositioningType = "ArtistIntent",
            isArchived = false,
            isFriendsOnly = false,
            genre = "All",
            playableDevices,
            isForSale = false,
            price = 0,
            isStudioAccessToApisAllowed = true,
            privacyType = "Public",
        };
    }
    [HttpPostBypass("places/{placeId}")]
    public async Task<dynamic> UpdatePlace([Required, FromBody] UpdatePlace request, long placeId)
    {
        
    }
}