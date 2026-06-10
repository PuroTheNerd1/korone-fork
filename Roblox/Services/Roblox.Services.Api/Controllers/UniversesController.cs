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

    [RequireRobloxSession]
    [HttpGet("universes/get-info")]
    public async Task<dynamic> GetUniverseInfo(long universeId)
    {
        var universe = (await services.games.MultiGetUniverseInfo(new[] { universeId })).FirstOrDefault();
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
