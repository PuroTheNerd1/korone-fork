using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Roblox.Dto.Games;
using Roblox.Exceptions;
using Roblox.Models;
using Roblox.Models.Assets;
using Roblox.Models.Studio;
using Roblox.Services.Exceptions;
namespace Roblox.Website.Controllers;
[ApiController]
[Route("")]
public class UniverseV1 : ControllerBase
{

    [HttpGet("universes/get-info")]
    public async Task<dynamic> GetUniverseInfo(long universeId)
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] { universeId })).FirstOrDefault();
        return new
        {
            Name = uni.name,
            Description = uni.description,
            RootPlace = uni.rootPlaceId,
            StudioAccessToApisAllowed = false,
            CurrentUserHasEditPermissions = uni.creatorId == safeUserSession.userId,
            UniverseAvatarType = uni.universeAvatarType,
        };
    }

    [HttpGetBypass("universes/get-universe-places")]
    public async Task<dynamic> GetPlaces(long universeId)
    {
        var place = await services.games.GetRootPlaceId(universeId);
        var placeInfo = await services.assets.GetAssetCatalogInfo(place);
        return new
        {
            FinalPage = true,
            RootPlace = place,
            Places = new List<dynamic>
            {
                new
                {
                    PlaceId = place,
                    Name = placeInfo.name,
                }
            },
            PageSize = 50
        };
    }

    [HttpGetBypass("badges/list-badges-for-place/json")]
    public dynamic GetGameBadges()
    {
        return new
        {
            FinalPage = true,
            GameBadges = new List<dynamic>(),
            PageSize = 50
        };
    }

    [HttpGetBypass("developerproducts/list")]
    public dynamic GetDeveloperProducts()
    {
        return new
        {
            FinalPage = true,
            DeveloperProducts = new List<dynamic>(),
            PageSize = 50
        };
    }

    [HttpGetBypass("universes/get-aliases")]
    public dynamic GetAliases()
    {
        return new
        {
            FinalPage = true,
            Aliases = new List<string>(),
            PageSize = 50
        };
    }

    [HttpGet("v1/gametemplates")]
    public dynamic StudioTemplates()
    {
        var Templates = new
        {
            gameTemplateType = "Generic",
            hasTutorials = false,
            universe = new Universe
            {
                id = 1,
                name = "Starter place",
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

    [HttpGet("v1/search/universes")]
    public async Task<RobloxCollectionPaginated<GamesForCreatorDevelop>> GetUserCreatedGames()
    {
        int offset = int.Parse("0");
        var result =
            (await services.games.GetGamesForTypeDevelop(CreatorType.User, safeUserSession.userId, safeUserSession.username, 50, offset, null, null)).ToList();
        return new RobloxCollectionPaginated<GamesForCreatorDevelop>()
        {
            data = result
        };
    }
    [HttpGet("v1/user/universes")]
    public async Task<RobloxCollectionPaginated<GamesForCreatorDevelop>> GetUserCreatedGames(string? sortOrder, string? accessFilter, int limit, string? cursor = null)
    {
        if (limit is > 100 or < 1) limit = 10;
        int offset = int.Parse(cursor ?? "0");
        var result =
            (await services.games.GetGamesForTypeDevelop(CreatorType.User, safeUserSession.userId, safeUserSession.username, limit, offset, sortOrder ?? "asc", accessFilter ?? "All")).ToList();
        return new RobloxCollectionPaginated<GamesForCreatorDevelop>()
        {
            nextPageCursor = result.Count >= limit ? (offset+limit).ToString(): null,
            previousPageCursor = offset >= limit ? (offset-limit).ToString() : null,
            data = result
        };
    }

    [HttpGet("v2/universes/{universeId}/places")]
    [HttpGet("v1/universes/{universeId}/places")]
    public async Task<dynamic> GetUniverseAttachedPlaces(long universeId)
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] {universeId})).FirstOrDefault();
        return new
        {
            previousPageCursor = (string?)null,
            nextPageCursor = (string?)null,
            data = new List<object>
            {
                new
                {
                    maxPlayerCount = uni.maxPlayers,
                    socialSlotType = "Automatic",
                    allowCopying = false,
                    currentSavedVersion = 1,
                    allowedGearTypes = (string?)null,
                    maxPlayersAllowed = 0,
                    id = uni.rootPlaceId,
                    universeId = universeId,
                    name = uni.name,
                    description = uni.description,
                    isRootPlace = true
                }
            }
        };
    }
    /*
    [HttpGetBypass("teamtest/{placeId}/runninggames")]
    [HttpGet("v1/teamtest/places/{placeId}/runninggames")]
    public dynamic GetTeamTestRunningGames(long placeId)
    {
        return new
        {
            previousPageCursor = (string?)null,
            nextPageCursor = (string?)null,
            data = new List<object>()
        };
    }
    */
    [HttpGet("v1/places/{placeId}/teamcreate/active_session/members")]
    public async Task<dynamic> GetTeamCreateMembers(long placeId)
    {
        List<dynamic> players = new List<dynamic>();
        var startIndex = 0;
        var limit = 1;
        var offset = startIndex;
        var servers = (await services.gameServer.GetGameServers(placeId, offset, limit, 3)).ToList();

        foreach (var server in servers)
        {
            var gameServerPlayers = server.players.Select(player => new
            {
                id = player.userId,
                name = player.username,
                displayName = player.username
            }).ToList();

            players.AddRange(gameServerPlayers);
        }

        return new
        {
            data = players
        };
    }

    [HttpGet("v1/user/groups/canmanage")]
    public dynamic CanManageGroup()
    {
        return new
        {
            data = new List<object>()
        };
    }

    [HttpGet("v2/universes/{universeId}/permissions")]
    public async Task<dynamic> CanManageV2(long universeId)
    {
        bool canManage = await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        if (!canManage)
            throw new RobloxException(403, 0, "The user is not authorized to perform this action.");
        return new
        {
            data = new List<object>()
        };
    }

    [HttpGet("v1/universes/{universeId}/permissions")]
    public async Task<dynamic> CanManage(long universeId)
    {
        bool canManage = await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        return new
        {
            canManage,
            canCloudEdit = canManage
        };
    }

    [HttpGet("v1/universes/{universeId}/teamcreate")]
    public async Task<dynamic> TeamCreateSettings(long universeId)
    {
        return new
        {
            isEnabled = await services.games.IsCloudeditEnabled(universeId),
        };
    }

    [HttpGet("v1/universes/{universeId}")]
    public async Task<dynamic> UniverseInfo(long universeId)
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] {universeId})).FirstOrDefault();
        var assetInfo = (await services.assets.MultiGetAssetDeveloperDetails(new[] {uni.rootPlaceId})).First();
        return new
        {
            id = universeId,
            name = uni.name,
            description = uni.description,
            isArchived = false,
            rootPlaceId = uni.rootPlaceId,
            isActive = assetInfo.moderationStatus != ModerationStatus.Declined,
            privacyType = uni.isPublic ? PrivacyType.Public : PrivacyType.Private,
            creatorType = assetInfo.creator.type,
            creatorTargetId = uni.creatorId,
            creatorName = uni.creatorName,
            created = uni.created,
            updated = uni.updated
        };
    }

    [HttpGet("v1/universes/{universeId}/icon")]
    public dynamic GetUniverseIcon(long universeId)
    {
        return new
        {
            imageId = (int?)null,
            isApproved = true
        };
    }

    [HttpPatch("v1/universes/{universeId}/configuration")]
    [HttpPost("v2/universes/{universeId}/configuration")]
    [HttpPostBypass("v2/universes/{universeId}/configuration")]
    public async Task<dynamic> SetUniverseConfiguration(long universeId, [FromBody] UniverseConfiguration configuration)
    {
        if (!await services.games.CanManageUniverse(safeUserSession.userId, universeId))
        {
            throw new ForbiddenException(0, "You are not authorized to configure this universe.");
        }
        await services.games.SetPlaceVisibility(universeId, configuration.privacyType == PrivacyType.Public);
        var uni = (await services.games.MultiGetUniverseInfo(new[] { universeId })).FirstOrDefault();
        List<string> playableDevices = new List<string>
        {
            "Computer",
            "Phone",
            "Tablet",
            "Console",
            "VR"
        };

        return new UniverseConfiguration
        {
            allowPrivateServers = false,
            privateServerPrice = 0,
            id = universeId,
            name = uni.name,
            universeAvatarType = uni.universeAvatarType,
            universeScaleType = "AllScales",
            universeAnimationType = "Standard",
            universeCollisionType = "Outerbox",
            universeBodyType = "Standard",
            universeJointPositioningType = "ArtistIntent",
            isArchived = false,
            isFriendsOnly = false,
            genre = uni.genre,
            playableDevices = playableDevices,
            permissions = new
            {
                IsThirdPartyTeleportAllowed = true,
                IsThirdPartyAssetAllowed = true,
                IsThirdPartyPurchaseAllowed = true,
            },
            isForSale = false,
            price = 0,
            isStudioAccessToApisAllowed = true,
            privacyType = PrivacyType.Public,
        };
    }

    [HttpGet("v2/universes/{universeId}/configuration")]
    [HttpGet("v1/universes/{universeId}/configuration")]
    public async Task<dynamic> GetUniverseConfiguration(long universeId)
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] {universeId})).FirstOrDefault();
        List<string> playableDevices = new List<string>
        {
            "Computer",
            "Phone",
            "Tablet",
            "Console",
            "VR"
        };

        return new UniverseConfiguration
        {
            allowPrivateServers = false,
            privateServerPrice = 0,
            id = universeId,
            name = uni.name,
            universeAvatarType = uni.universeAvatarType,
            universeScaleType = "AllScales",
            universeAnimationType = "Standard",
            universeCollisionType = "Outerbox",
            universeBodyType = "Standard",
            universeJointPositioningType = "ArtistIntent",
            isArchived = false,
            isFriendsOnly = false,
            genre = uni.genre,
            playableDevices = playableDevices,
            permissions = new
            {
                IsThirdPartyTeleportAllowed = true,
                IsThirdPartyAssetAllowed = true,
                IsThirdPartyPurchaseAllowed = true,
            },
            isForSale = false,
            price = 0,
            isStudioAccessToApisAllowed = true,
            privacyType = PrivacyType.Public,
        };
    }
}