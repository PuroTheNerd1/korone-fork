using System.Collections;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Roblox.Dto.Assets;
using Roblox.Dto.Games;
using Roblox.Exceptions;
using Roblox.Models;
using Roblox.Models.Assets;
using Roblox.Models.Studio;
using Roblox.Services.Exceptions;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/")]
public class UniverseV1 : ControllerBase 
{
    [HttpGetBypass("toolbox-service/v1/{type}")]
    public async Task<dynamic> GetToolBoxService([FromRoute] string type, [FromQuery] string sortType, [FromQuery] int limit = 30, [FromQuery] string? cursor = null, [FromQuery] string? keyword = null)
    {
        CatalogSearchRequest request = new CatalogSearchRequest
        {
            keyword = keyword,
            category = type,
            subcategory = type,
            sortType = sortType,
            limit = limit,
            cursor = cursor
        };
        var searchResults = await services.assets.SearchCatalog(request);
        return new
        {
            totalResults = searchResults.data!.Count(),
            filteredKeyword	= searchResults.keyword,
            searchDebugInfo = (string?)null,
            spellCheckerResult	= new
            {
                correctionState = 0,
                correctedQuery = (string?)null,
                userQuery = (string?)null,
            },
            queryFacets = new
            {
                appliedFacets = new List<object>(),
                availableFacets = new List<object>(),
            },
            imageSearchStatus = (string?)null,
            previousPageCursor = searchResults.previousPageCursor,
            nextPageCursor = searchResults.nextPageCursor,
            data = searchResults.data!.Select(c => new
            {
                id = c.id,
                name = (string?)null,
                searchResultSource = "LexicalWithSort"
            })
        };
    }
    [HttpPostBypass("toolbox-service/v1/items/details")]
    public async Task<dynamic> GetToolBoxServiceDetails([FromBody] WebsiteModels.Catalog.MultiGetRequest request)
    {
	    var multiGetResults = await services.assets.MultiGetInfoById(request.items.Select(c => c.id));
        return new
        {
            data = multiGetResults.Select(c =>
            {
                return new
                {
                    asset = new
                    {
                        audioDetails = (string?)null,
                        id = c.id,
                        name = c.name,
                        typeId = (int)c.assetType,
                        assetSubTypes = new List<int>(),
                        assetGenres	= c.genres,
                        isEndorsed = false, 
                        description	= c.description,
                        duration = 0,
                        hasScripts = c.assetType == Models.Assets.Type.Model || c.assetType == Models.Assets.Type.Plugin,
                        createdUtc = c.createdAt,
                        updatedUtc = c.updatedAt,
                        creatingUniverseId = (string?)null,
                        isAssetHashApproved	= c.moderationStatus == ModerationStatus.ReviewApproved,
                        // TODO: Asset privacy options
                        visibilityStatus = c.moderationStatus == ModerationStatus.ReviewApproved,
                        socialLinks = new List<object>(),
                    },
                    creator = new
                    {
                        id = c.creatorTargetId,
                        name = c.creatorName,
                        type = (int)c.creatorType,
                        isVerifiedCreator = false,
                        latestGroupUpdaterUserId = (string?)null,
                        latestGroupUpdaterUserName = (string?)null,
                    },
                    // TODO: Votes
                    voting = new
                    {
                        showVotes = false,
                        upVotes = 0,
                        downVotes = 0,
                        canVote = false,
                        userVote = (string?)null,
                        hasVoted = false,
                        voteCount = 0,
                        upVotePercent = 0,
                    },
                    fiatProduct	= new
                    {
                        currencyCode = "USD",
                        quantity = new
                        {
                            significand	= 0,
                            exponent = 0,
                        },
                        published = true,
                        purchasable	= true,
                    }
                };
            })
        };
    }
    [HttpGetBypass("universes/get-universe-containing-place")]
    public async Task<dynamic> GetUniverse(long placeid)
    {
        return new
        {
            UniverseId = await services.games.GetUniverseId(placeid)
        };
    }

    [HttpGetBypass("universes/get-info")]
    public async Task<dynamic> GetUniverseInfo(long universeId) 
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] { universeId })).FirstOrDefault();
        if (uni == null)
            throw new RecordNotFoundException();
        return new 
        {
            Name = uni.name,
            Description = uni.description,
            RootPlace = uni.rootPlaceId,
            StudioAccessToApisAllowed = true,
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

    // TODO: is this an actual api?
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
    
    Dictionary<string, long> getStarterPlaces = new Dictionary<string, long> 
    {
        { "Baseplate", 36573 },
        { "Flat Terrain", 36574 },
        { "Starting Place", 36568 },
        { "Western", 36569 },
        { "Suburban", 36570 },
        { "Team/FFA Arena", 36571 },
        { "Capture The Flag", 36572 },
        //{ "Control Points", 36575 },
        { "City", 36576 },
        { "Castle", 36577 },
        { "Village", 36585 },
        { "Obby", 36578 },
        { "Combat", 36579 },
        { "Racing", 36580 },
        { "Pirate Island", 36581 },
        { "Line Runner", 36582 },
        //{ "Infinite Runner", 36583 },
        //{ "Free For All", 36584 },
        //{ "Team Deathmatch", 36590 }
    };

    [HttpGet("v1/gametemplates")]
    public async Task<dynamic> StudioTemplates() 
    {
        // ArrayList templates = new ArrayList();
        // int i = 1;
        // foreach (var place in getStarterPlaces) {
        //     templates.Add(new {
        //         gameTemplateType = "Generic",
        //         hasTutorials = false,
        //         universe = new Universe {
        //             id = i,
        //             name = place.Key,
        //             description = "skibidi",
        //             isArchived = false,
        //             rootPlaceId = place.Value,
        //             isActive = true,
        //             privacyType = "Public",
        //             creatorType = "User",
        //             creatorTargetId = 1,
        //             creatorName = "ROBLOX",
        //             created = DateTime.Parse("2013-11-01T08:47:14.07Z"),
        //             updated = DateTime.Parse("2023-05-02T22:03:01.107Z")
        //         }
        //     });
        //     i++;
        // }
        
        var templates = await services.games.MultiGetPlaceDetails(getStarterPlaces.Values.ToList()); //await services.games.MultiGetUniverseInfo(getStarterPlaces.Values.ToList());
        return new 
        {
            data = templates.Select(c => 
            {
                return new
                {
                    gameTemplateType = "Generic",
                    hasTutorials = false,
                    universe = new Universe 
                    {
                        id = c.universeId,
                        name = c.name,
                        description = c.description ?? "skbidii",
                        isArchived = false,
                        rootPlaceId = c.universeRootPlaceId,
                        isActive = true,
                        privacyType = "Public",
                        creatorType = "User",
                        creatorTargetId = c.builderId,
                        creatorName = c.builder,
                        created = c.created,
                        updated = c.updated
                    }
                };
            })
        };
    }

    [HttpGetBypass("v1/universes/multiget")]
    public async Task<dynamic> MultiGetUniverseInfo([FromQuery] List<long> ids) 
    {
        var universes = await services.games.MultiGetUniverseInfo(ids);
        return new 
        {
            data = universes.Select(c => 
            {
                return new 
                {
                    id = c.id,
                    name = c.name,
                    description = c.description,
                    isArchived = false,
                    rootPlaceId = c.rootPlaceId,
                    isActive = true,
                    privacyType = c.isPublic ? "Public" : "Private",
                    creatorType = c.creatorType,
                    creatorTargetId = c.creatorId,
                    creatorName = c.creatorName,
                    created = c.created,
                    updated = c.updated
                };
            })
        };
    }

    [HttpGet("v1/search/universes")]
    public async Task<dynamic> SearchUniverse(string q) 
    {
        int offset = int.Parse("0");
        if (q.Contains("Team")) 
        {
            var result = await services.games.GetTeamcreateMembershipsForUser(safeUserSession.userId);
            return new 
            {
                previousPageCursor = (string?)null,
                nextPageCursor = (string?)null,
                data = result.Select(c => 
                {
                    return new 
                    {
                        id = c.id,
                        name = c.name,
                        description = c.description,
                        isArchived = false,
                        rootPlaceId = c.rootPlaceId,
                        isActive = c.isPublic,
                        privacyType = c.isPublic ? PrivacyType.Public : PrivacyType.Private,
                        creatorType = c.creator.type,
                        creatorTargetId = c.creatorId,
                        creatorName = c.creatorName,
                        created = c.created,
                        updated = c.updated
                    };
                })
            };
        }
        else 
        {
            var result =
                (await services.games.GetGamesForTypeDevelop(CreatorType.User, safeUserSession.userId,
                    safeUserSession.username, 50, offset, null, null)).ToList();
            return new RobloxCollectionPaginated<GamesForCreatorDevelop>() 
            {
                data = result
            };
        }
    }

    [HttpGet("v1/user/universes")]
    public async Task<RobloxCollectionPaginated<GamesForCreatorDevelop>> GetUserCreatedGames(string? sortOrder, string? accessFilter, int limit, string? cursor = null) 
    {
        if (limit is > 100 or < 1) limit = 10;
        int offset = int.Parse(cursor ?? "0");
        var result =
            (await services.games.GetGamesForTypeDevelop(CreatorType.User, safeUserSession.userId,
                safeUserSession.username, limit, offset, sortOrder ?? "asc", accessFilter ?? "All")).ToList();
        return new RobloxCollectionPaginated<GamesForCreatorDevelop>() {
            nextPageCursor = result.Count >= limit ? (offset + limit).ToString() : null,
            previousPageCursor = offset >= limit ? (offset - limit).ToString() : null,
            data = result
        };
    }

    [HttpGet("v2/universes/{universeId}/places")]
    [HttpGet("v1/universes/{universeId}/places")]
    public async Task<dynamic> GetUniverseAttachedPlaces(long universeId) 
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] { universeId })).FirstOrDefault();
        if (uni == null)
            throw new RecordNotFoundException();
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

    [HttpGetBypass("v1/user/teamcreate/memberships")]
    public async Task<dynamic> GetMembershipsForCurrentUser() 
    {
        var memberships = await services.games.GetTeamcreateMembershipsForUser(safeUserSession.userId);
        return new 
        {
            previousPageCursor = (string?)null,
            nextPageCursor = (string?)null,
            data = memberships.Select(c => 
            {
                return new 
                {
                    id = c.id,
                    name = c.name,
                    description = c.description,
                    isArchived = false,
                    rootPlaceId = c.rootPlaceId,
                    isActive = c.isPublic,
                    privacyType = c.isPublic ? PrivacyType.Public : PrivacyType.Private,
                    creatorType = c.creator.type,
                    creatorTargetId = c.creatorId,
                    creatorName = c.creatorName,
                    created = c.created,
                    updated = c.updated
                };
            })
        };
    }

    [HttpGetBypass("v1/universes/{universeId}/teamcreate/memberships")]
    public async Task<dynamic> GetMembershipsForUniverse(long universeId) 
    {
        var memberships = await services.games.GetTeamcreateMembershipsForUniverse(universeId);
        return new 
        {
            previousPageCursor = (string?)null,
            nextPageCursor = (string?)null,
            data = memberships.Select(c => 
            {
                return new 
                {
                    buildersClubMembershipType = "None",
                    userId = c.id,
                    username = c.name,
                    displayName = c.name,
                };
            })
        };
    }

    /*
    [HttpGetBypass("teamtest/{placeId}/runninggames")]
    [HttpGet("v1/teamtest/places/{placeId}/runninggames")]
    public dynamic GetTeamTestRunningGames(long placeId)
    {
        return new
        {
            FinalPage = true,
            RunningGames = new List<dynamic>(),
            PageSize = 50
        };
    }
    */
    [HttpGetBypass("universes/{universeId}/listcloudeditors")]
    public async Task<dynamic> GetCloudEditors(long universeId) 
    {
        var editors = await services.games.GetTeamcreateMembershipsForUniverse(universeId);
        return new 
        {
            finalPage = true,
            users = editors.Select(c => 
            {
                return new 
                {
                    userId = c.id,
                    isAdmin = false,
                };
            })
        };
    }

    [HttpGet("v1/places/{placeId}/teamcreate/active_session/members")]
    public async Task<dynamic> GetTeamCreateMembers(long placeId) {
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

    [HttpGetBypass("v1/universes/{universeId}/context-permission")]
    [HttpGetBypass("v1/universes/{universeId}/permissions")]
    public async Task<dynamic> CanManage(long universeId) 
    {
        bool canManage = await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        bool canCloudEdit = await services.games.CanCloudEdit(safeUserSession.userId, universeId) ? canManage : false;
        return new 
        {
            canManage,
            canCloudEdit
        };
    }

    [HttpPatch("v1/universes/{universeId}/teamcreate")]
    public async Task<dynamic> SetTeamCreateSettings([FromBody] TeamCreateSettings request, long universeId) 
    {
        if (!await services.games.CanManageUniverse(safeUserSession.userId, universeId)) 
            throw new ForbiddenException(0, "You are not authorized to configure this universe.");
        

        await services.games.SetCloudedit(request.isEnabled, universeId);
        return Content("{}", "application/json");
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
        var uni = (await services.games.MultiGetUniverseInfo(new[] { universeId })).FirstOrDefault();
        if (uni == null)
            throw new RecordNotFoundException();
        var assetInfo = (await services.assets.MultiGetAssetDeveloperDetails(new[] { uni.rootPlaceId })).First();
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

    [HttpPatchBypass("v1/universes/{universeId}/configuration")]
    [HttpPatchBypass("v2/universes/{universeId}/configuration")]
    public async Task<dynamic> SetUniverseConfiguration([FromRoute] long universeId, [FromBody] UpdateUniverseConfiguration configuration) 
    {
        List<string> playableDevices = new List<string> {
            "Computer",
            "Phone",
            "Tablet",
            "Console",
            "VR"
        };
        if (!await services.games.CanManageUniverse(safeUserSession.userId, universeId)) 
            throw new ForbiddenException(0, "You are not authorized to configure this universe.");
        

        //await services.games.SetPlaceVisibility(universeId, configuration.privacyType == PrivacyType.Public);
        var uni = (await services.games.MultiGetUniverseInfo(new[] { universeId })).FirstOrDefault();
        if (uni == null)
            throw new RecordNotFoundException();
        await services.games.SetForceMorph(universeId, configuration.universeAvatarType == "PlayerChoice" ? ForceMorphType.PlayerChoice : configuration.universeAvatarType == "MorphToR6" ? ForceMorphType.MorphToR6 : ForceMorphType.MorphToR15);
        return new 
        {
            allowPrivateServers = false,
            privateServerPrice = 0,
            id = universeId,
            name = uni.name,
            universeAvatarType = uni.universeAvatarType,
            universeScaleType = "AllScales",
            universeAnimationType = "Standard",
            universeCollisionType = R15CollisionType.OuterBox.ToString(),
            universeBodyType = "Standard",
            universeJointPositioningType = "ArtistIntent",
            universeAvatarMinScales = new 
            {
                height = 0,
                width = 0,
                head = 0,
                depth = 0,
                proportion = 0,
                bodyType = 0,
            },
            universeAvatarMaxScales = new 
            {
                height = 1,
                width = 1,
                head = 1,
                depth = 1,
                proportion = 1,
                bodyType = 1,
            },
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
            studioAccessToApisAllowed = true,
            isStudioAccessToApisAllowed = true,
            privacyType = PrivacyType.Public,
        };
    }

    [HttpGet("v2/universes/{universeId}/configuration")]
    [HttpGet("v1/universes/{universeId}/configuration")]
    public async Task<dynamic> GetUniverseConfiguration(long universeId) 
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] { universeId })).FirstOrDefault();
        List<string> playableDevices = new List<string> 
        {
            "Computer",
            "Phone",
            "Tablet",
            "Console",
            "VR"
        };
        if (uni == null)
            throw new RecordNotFoundException();
        return new  
        {
            allowPrivateServers = false,
            privateServerPrice = 0,
            id = universeId,
            name = uni.name,
            universeAvatarType = uni.universeAvatarType,
            universeScaleType = "AllScales",
            universeAnimationType = "Standard",
            universeCollisionType = R15CollisionType.OuterBox.ToString(),
            universeBodyType = "Standard",
            universeJointPositioningType = "ArtistIntent",
            universeAvatarMinScales = new 
            {
                height = 0,
                width = 0,
                head = 0,
                depth = 0,
                proportion = 0,
                bodyType = 0,
            },
            universeAvatarMaxScales = new 
            {
                height = 1,
                width = 1,
                head = 1,
                depth = 1,
                proportion = 1,
                bodyType = 1,
            },
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
            studioAccessToApisAllowed = true,
            isStudioAccessToApisAllowed = true,
            privacyType = PrivacyType.Public,
        };
    }
}