using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Games;
using Roblox.Exceptions;
using Roblox.Logging;
using Roblox.Models;
using Roblox.Models.Assets;
using Roblox.Models.Db;
using Roblox.Services.Exceptions;
using Type = Roblox.Models.Assets.Type;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/games/v1")]
public class GamesControllerV1 : ControllerBase
{
    [HttpGet("games")]
    public async Task<dynamic> MultiGetUniverseInfo(string universeIds)
    {
        var sp = universeIds.Split(",").Select(long.Parse);
        var result = await services.games.MultiGetUniverseInfo(sp);
        return new
        {
            data = result,
        };
    }

    [HttpGet("games/sorts")]
    public async Task<dynamic> GetGameSorts(string? gameSortsContext)
    {
        var sorts = new Dictionary<string, dynamic>()
        {
            {
                "popular", new
                {
                    token = "popular",
                    name = "popular",
                    displayName = "popular",
                }
            },
            {
                "roulette", new
                {
                    token = "roulette",
                    name = "roulette",
                    displayName = "roulette",
                }
            },
            {
                "recent", new
                {
                    token = "recent",
                    name = "Recent",
                    displayName = "Recent",
                }
            },
            {
                "mostFavorited", new
                {
                    token = "mostFavorited",
                    name = "Most Favorited",
                    displayName = "Most Favorited",
                }
            },
            {
                "recentlyUpdated", new
                {
                    token = "recentlyUpdated",
                    name = "Recently Updated",
                    displayName = "Recently Updated",
                }
            },
            {
                "recentlyCreated", new
                {
                    token = "recentlyCreated",
                    name = "Recently Created",
                    displayName = "Recently Created",
                }
            },
        };

        var results = new List<dynamic>();
        if (gameSortsContext == "HomeSorts")
        {
            if (userSession == null)
                throw new ForbiddenException();
            // we need to check if player actually has anything recent before showing recent sort
            var recent = await services.games.GetRecentGames(userSession.userId, 1);
            if (recent.Any())
            {
                results.Add(sorts["recent"]);
                results.Add(sorts["roulette"]);
                results.Add(sorts["popular"]);
                // results.Add(sorts["mostFavorited"]);
            }
        }
        else
        {
            results.Add(sorts["popular"]);
            results.Add(sorts["roulette"]);
            results.Add(sorts["mostFavorited"]);
            results.Add(sorts["recentlyUpdated"]);
            // results.Add(sorts["recentlyCreated"]);
        }

        return new
        {
            sorts = results.Select(c => new
            {
                c.token,
                c.name,
                c.displayName,
                getSetTypeId = 0,
                gameSetTargetId = 0,
                timeOptionsAvailable = false,
                genreOptionsAvailable = false,
                numberOfRows = 1,
                numberOfGames = 0,
                isDefaultSort = true,
                contextUniverseId = (long?) null,
                contextCountryRegionId = (int?) null,
                tokenExpiryInSeconds = 86400,
            }),
        };
    }

    [HttpGet("games/list")]
    public async Task<dynamic> GetGamesList(string? sortToken, int maxRows = 10, Genre? genre = null, string? keyword = null)
    {
        // if (!await services.cooldown.TryIncrementBucketCooldown($"GetGames:{safeUserSession.userId}", 3, TimeSpan.FromSeconds(5)))
        // {
        //     Console.WriteLine($"GetGames:{safeUserSession.userId}");
        //     throw new TooManyRequestsException(0, "Too many attempts. Try again in a few seconds.");
        // }
        if (maxRows is > 100 or < 1) maxRows = 10;
        var result = await services.games.GetGamesList(userSession?.userId, sortToken, maxRows, genre, keyword);
        return new
        {
            games = result,
        };
    }

    private static Regex numberRegex { get; } = new("([0-9]+)");
    
    [HttpGet("games/multiget-playability-status")]
    public dynamic MultiGetPlayabilityStatus()
    {
        var ids = HttpContext.Request.QueryString.Value;
        return numberRegex.Matches(ids).Select(c => long.Parse(c.Value)).Distinct().Select(c => new
        {
            playabilityStatus = "Playable",
            isPlayable = true,
            universeId = c,
        });
    }

    [HttpGet("games/{universeId:long}/social-links/list")]
    public dynamic GetSocialLinks()
    {
        return new
        {
            data = new List<int>(),
        };
    }

    [HttpGet("games/recommendations/game/{universeId:long}")]
    public async Task<dynamic> GetRecommendedGames(long universeId, int maxRows = 6)
    {
        if (maxRows is > 100 or < 1) maxRows = 10;
        // todo: actually add recommendeds
        var result = await services.games.GetGamesList(userSession.userId, "popular", maxRows, null, null);
        return new
        {
            games = result,
        };
    }

    [HttpGet("games/multiget-place-details")]
    public async Task<IEnumerable<PlaceEntry>> MultiGetPlaceDetails(string placeIds)
    {
        return await services.games.MultiGetPlaceDetails(placeIds.Split(",").Select(long.Parse));
    }

    [HttpGet("games/votes")]
    public async Task<dynamic> GetGameVotes(string universeIds)
    {
        var ids = universeIds.Split(",").Select(long.Parse).Distinct().ToList();
        if (ids.Count is < 1 or > 100)
            throw new RobloxException(400, 0, "BadRequest");
        var uni = await services.games.MultiGetUniverseInfo(ids);

        var result = new List<dynamic>();
        foreach (var item in uni)
        {
            var votes = await services.assets.GetVoteForAsset(item.rootPlaceId);
            result.Add(new
            {
                id = item.id,
                upVotes = votes.upVotes,
                downVotes = votes.downVotes,
            });
        }

        return new
        {
            data = result,
        };
    }

    [HttpPatch("games/{universeId:long}/user-votes")]
    public async Task VoteOnUniverse(long universeId, [Required, FromBody] VoteRequest request)
    {
        var uni = (await services.games.MultiGetUniverseInfo(new[] {universeId})).FirstOrDefault();
        await services.assets.VoteOnAsset(uni.rootPlaceId, safeUserSession.userId, request.vote);
    }

    [HttpGet("users/{userId:long}/count")]
    public async Task<dynamic> GetUserGameCount(long userId) {
        var localUserId = safeUserSession.userId;
        await using var placeCuntLock =
            await Services.Cache.redLock.CreateLockAsync("GetPlaceCountV1:UserId:" + localUserId,
                TimeSpan.FromSeconds(3));
        if (!placeCuntLock.IsAcquired)
        {
            Writer.Info(LogGroup.AbuseDetection, "GetPlaceCount API could not acquire placeCuntLock");
            throw new TooManyRequestsException(0, "Too many attempts. Try again in a few seconds.");
        }
        
        var uniCount = await services.games.GetUserPlaceCount(userId);
        return new {
            universeCount = uniCount,
        };
    }
    
    [HttpGet("games/{universeId:long}/game-passes")]
    public async Task<RobloxCollectionPaginated<UniverseGamePassEntry>> GetUniverseGamePasses(long universeId, long? unfiltered = 0, SortOrder? sortOrder = SortOrder.Asc, int? limit = 10, string? cursor = null) {
        if (limit is > 100 or < 1) limit = 10;
        int offset = int.Parse(cursor ?? "0");
        List<UniverseGamePassEntry> result;
        if (unfiltered == 1) {
            result = (await services.games.GetGamePassesForUniverseModStatus(universeId, limit ?? 10, offset,
                userSession?.userId, sortOrder ?? SortOrder.Asc)).ToList();
        }
        else {
            result = (await services.games.GetGamePassesForUniverse(universeId, limit ?? 10, offset,
                userSession?.userId, sortOrder ?? SortOrder.Asc)).ToList();
        }
        
        return new RobloxCollectionPaginated<UniverseGamePassEntry>()
        {
            nextPageCursor = result.Count >= limit ? (offset+limit).ToString(): null,
            previousPageCursor = offset >= limit ? (offset-limit).ToString() : null,
            data = result,
        };
    }
    
    [HttpGet("games/game-passes/{assetId:long}")]
    public async Task<GamePassDetails> GetGamePassInfo(long assetId) {
        var asset = await services.assets.GetAssetCatalogInfo(assetId);
        if (asset is null) {
            throw new BadRequestException(0, "Asset does not exist");
        }
        if (asset.assetType != Type.GamePass) {
            throw new BadRequestException(0, "Asset is not a Game Pass");
        }

        var passInfo = (await services.games.GetGamePassInfo(assetId)).FirstOrDefault();
        if (passInfo is null) {
            // not sure how this would ever happen but just in case
            throw new RecordNotFoundException();
        }
        
        return passInfo;
    }
}