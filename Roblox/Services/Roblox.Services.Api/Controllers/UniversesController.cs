using Microsoft.AspNetCore.Mvc;
using Roblox.Services.Exceptions;
using Roblox.Web.Infrastructure.Controllers;
using Roblox.Web.Infrastructure.Metadata;

namespace Roblox.Services.Api.Controllers;

[ApiController]
[Route("/")]
public class UniversesController : RobloxControllerBase
{
    [AllowRobloxAnonymous]
    [HttpGet("universes/get-universe-containing-place")]
    public async Task<dynamic> GetUniverseContainingPlace(long placeid)
    {
        return new
        {
            UniverseId = await services.games.GetUniverseId(placeid),
        };
    }

    [RequireRccRequest]
    [HttpGet("v1.1/game-start-info")]
    public async Task<dynamic> GameStartInfo(long universeId)
    {
        var universe = await services.games.GetUniverseInfo(universeId);
        return new
        {
            gameAvatarType = universe.universeAvatarType,
            allowCustomAnimations = "True",
            universeAvatarCollisionType = "OuterBox",
            universeAvatarBodyType = "Standard",
            jointPositioningType = "ArtistIntent",
            universeAvatarMinScales = new
            {
                height = 0.9,
                width = 0.7,
                head = 0.95,
                depth = 0.0,
                proportion = 0.0,
                bodyType = 0.0,
            },
            universeAvatarMaxScales = new
            {
                height = 1.05,
                width = 1.0,
                head = 1.0,
                depth = 0.0,
                proportion = 1.0,
                bodyType = 1.0,
            },
            universeAvatarAssetOverrides = new List<object>(),
        };
    }
    
    [RequireRccRequest]
    [HttpPost("game/load-place-info")]
    public async Task<dynamic> LoadPlaceInfo()
    {
        var details = await services.assets.GetAssetCatalogInfo(currentPlaceId);
        return new
        {
            CreatorId =  details.creatorTargetId,
            CreatorType = "User",
            PlaceVersion = details.id,
            GameId = currentPlaceId,
            IsRobloxPlace = details.creatorTargetId == 1
        };
    }
    
    [AllowRobloxAnonymous]
    [HttpGet("developerproducts/list")]
    public async Task<dynamic> GetDeveloperProducts(long page, long? placeId, long? universeId)
    {
        if (page is < 1 or > 5)
        {
            page = 1;
        }

        universeId = universeId switch
        {
            null when placeId is not null => await services.games.GetUniverseId(placeId.Value),
            null => throw new RobloxException(RobloxException.BadRequest, 0,
                "You must provide a valid placeId or universeId."),
            _ => universeId!
        };

        var products = (await services.games.GetDeveloperProducts(universeId.Value, 5, 5 * (page - 1))).ToList();
        return new
        {
            FinalPage = products.Count < 5 || page == 5,
            DeveloperProducts = products.Select(c => new
            {
                ProductId = c.id,
                DeveloperProductId = c.iconImageAssetId,
                Name = c.name,
                Description = c.description,
                IconImageAssetId = c.iconImageAssetId,
                displayName = c.name,
                displayDescription = c.description,
                displayIcon = (int?)null,
                PriceInRobux = c.priceInRobux,
            }),
            PageSize = products.Count
        };
    }

    [RequireRobloxSession]
    [HttpGet("universes/get-info")]
    public async Task<dynamic> GetUniverseInfo(long universeId)
    {
        var universe = (await services.games.MultiGetUniverseInfo([universeId])).FirstOrDefault();
        if (universe == null)
        {
            throw new RecordNotFoundException();
        }

        return new
        {
            Name = universe.name,
            Description = universe.description,
            RootPlace = universe.rootPlaceId,
            StudioAccessToApisAllowed = true,
            CurrentUserHasEditPermissions = universe.creatorId == safeUserSession.userId,
            UniverseAvatarType = universe.universeAvatarType,
        };
    }

    [RequireRobloxSession]
    [HttpGet("universes/get-universe-places")]
    public async Task<dynamic> GetUniversePlaces(long universeId)
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        var rootPlace = await services.games.GetRootPlaceId(universeId);
        var places = (await services.games.GetUniversePlaces(universeId)).ToList();
        return new
        {
            FinalPage = true,
            RootPlace = rootPlace,
            Places = places.Select(placeInfo => new
            {
                PlaceId = placeInfo.placeId,
                Name = placeInfo.name,
            }),
            PageSize = places.Count,
        };
    }

    [AllowRobloxAnonymous]
    [HttpGet("universes/get-aliases")]
    public dynamic GetAliases()
    {
        return new
        {
            FinalPage = true,
            Aliases = new List<string>(),
            PageSize = 50,
        };
    }
}
