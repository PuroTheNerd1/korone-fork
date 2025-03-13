using System.Formats.Asn1;
using Dapper;
using Newtonsoft.Json.Linq;
using Roblox.Dto;
using Roblox.Dto.Games;
using Roblox.Dto.Users;
using Roblox.Libraries;
using Roblox.Models.Assets;
using Roblox.Models.Db;
using Roblox.Services.Exceptions;
using Roblox.Services.Signer;
using Type = Roblox.Models.Assets.Type;

namespace Roblox.Services;

public class GamesService : ServiceBase, IService
{
    private GameServerService gameServer = new();
    private SignService sign = new();
    //ugh
    public readonly Dictionary<long, string> clientVersionMap = new Dictionary<long, string>
    {
        { 2017, "2017L" },
        { 2018, "2018L" },
        { 2020, "2020L" },
        { 2021, "2021M" }
    };
    public async Task<long> GetMaxPlayerCount(long placeId)
    {
        var result = await db.QuerySingleOrDefaultAsync<Dto.Total>(
            "SELECT asset_place.max_player_count AS total FROM asset_place WHERE asset_id = :id LIMIT 1", new
            {
                id = placeId,
            });
        return result?.total ?? 0;
    }
    public async Task<bool> IsFull(string jobId, long placeId)
    {
        var jobPlayers = await gameServer.GetGameServerPlayers(jobId);
        var maxPlayerCount = await GetMaxPlayerCount(placeId);
        if (jobPlayers.Count() >= maxPlayerCount)
        {
            return true;
        }
        return false;
    }
    public async Task<bool> CanCloudEdit(long userId, long universeId)
    {
        var result = await db.QuerySingleOrDefaultAsync<Dto.Total>(
            "SELECT COUNT(*) AS total FROM teamcreate_memberships WHERE universe_id = :id AND user_id = :userId", new
            {
                id = universeId,
                userId,
            });
        return result?.total > 0;
    }

    public async Task<bool> CanManageUniverse(long userId, long universeId)
    {
        var result = await db.QuerySingleOrDefaultAsync<Dto.Total>(
            "SELECT COUNT(*) AS total FROM universe WHERE id = :id AND creator_id = :userId", new
            {
                id = universeId,
                userId,
            });
        return result?.total > 0;
    }

    public async Task<long> GetYear(long placeId)
    {
        var result = await db.QuerySingleOrDefaultAsync<Dto.Year>(
            "SELECT asset_place.year AS year FROM asset_place WHERE asset_id = :id LIMIT 1", new
            {
                id = placeId,
            });
        return result?.year ?? 0;
    }
    public async Task<bool> IsCloudeditEnabled(long universeId)
    {
        bool result = await db.QuerySingleOrDefaultAsync<bool>(
            "SELECT cloudedit FROM universe WHERE id = :id", new
            {
                id = universeId,
            });
        return result;
    }
    public async Task<MultiGetUniverseEntry> SafeGetUniverseInfo(long userId, long universeId)
    {
        var universe = (await MultiGetUniverseInfo(new[] {universeId})).First();
        if (universe is null) 
            throw new RecordNotFoundException("Universe doesn't exist");
        

        if (universe.creatorId != userId) 
            throw new PermissionException(universe.rootPlaceId, userId);
        

        using var assets = ServiceProvider.GetOrCreate<AssetsService>(this);
        var details = await assets.GetAssetCatalogInfo(universe.rootPlaceId);
        // Second condition should almost never happen but just in case
        if (details.moderationStatus != ModerationStatus.ReviewApproved || details.creatorTargetId != userId) {
            throw new PermissionException(universe.rootPlaceId, userId);
        }
        return universe;
    }
    public async Task<long> GetRootPlaceId(long universeId)
    {
        //var details = await MultiGetUniverseInfo(new []{universeId});
        //var arr = details.ToArray();
        var result = await db.QuerySingleOrDefaultAsync<long>(
            "SELECT root_asset_id FROM universe WHERE id = :id LIMIT 1", new
            {
                id = universeId,
            });
        if (result == 0)
            throw new RobloxException(400, 0, "Invalid universe ID");
        return result;
    }

    public async Task<long> GetUniverseId(long placeId)
    {
        var result = await db.QuerySingleOrDefaultAsync<long>(
            "SELECT universe_id FROM universe_asset WHERE asset_id = :id LIMIT 1", new
            {
                id = placeId,
            });
        if (result == 0)
            throw new RobloxException(400, 0, "Invalid place ID");
        return result;
    }
    public async Task<IEnumerable<Dto.Users.MultiGetEntry>> GetTeamcreateMembershipsForUniverse(long universeId)
    {
        UsersService user = new UsersService();
        var result = await db.QueryAsync<dynamic>(
            "SELECT user_id FROM teamcreate_memberships WHERE universe_id = :id", new
            {
                id = universeId,
            });
        var userInfo = await user.MultiGetUsersById(result.Select(r => (long)r.user_id));
        return userInfo;
    }
    public async Task<IEnumerable<MultiGetUniverseEntry>> GetTeamcreateMembershipsForUser(long userId)
    {
        var result = await db.QueryAsync<dynamic>(
            "SELECT universe_id FROM teamcreate_memberships WHERE user_id = :id", new
            {
                id = userId,
            });

        return await MultiGetUniverseInfo(result.Select(r => (long)r.universe_id));
    }
    public async Task SetCloudedit(bool isEnabled, long universeId)
    {
        await db.ExecuteAsync("UPDATE universe SET cloudedit = :isEnabled WHERE id = :universeId",
            new
            {
                isEnabled = isEnabled,
                universeId = universeId,
            });
    }
    public async Task SetForceMorph(long universeId, ForceMorphType type)
    {
        await db.ExecuteAsync("UPDATE universe SET forcemorph_type = :type WHERE id = :id", new
        {
            id = universeId,
            type = (int)type,
        });
    }
    /*
    public async Task<MultiGetUniverseEntry> GetUniverseConfiguration(long universeIds)
    {

    }*/
    public async Task<IEnumerable<MultiGetUniverseEntry>> MultiGetUniverseInfo(IEnumerable<long> universeIds)
    {
        var ids = universeIds.ToArray();
        if (!ids.Any())
            return Array.Empty<MultiGetUniverseEntry>();

        var build = new SqlBuilder();
        var temp = build.AddTemplate(
            @"SELECT
                universe.id,
                universe.root_asset_id as rootPlaceId,
                universe.is_public as isPublic,
                universe.forcemorph_type as universeAvatarType,
                asset.name as sourceName,
                asset.description as sourceDescription,
                asset.name,
                asset.description,
                asset.asset_genre as genre,
                asset.created_at as created,
                asset.updated_at as updated,
                asset_place.max_player_count as maxPlayers,
                asset_place.year as year,
                asset_place.visit_count as visits,
                asset_place.is_vip_enabled as createVipServersAllowed,
                asset.price_robux as price,
                asset.creator_id as creatorId,
                asset.creator_type as creatorType,
                (SELECT COUNT(*) as playing FROM asset_server_player WHERE asset_id = universe.root_asset_id),
                (case when ""asset"".creator_type = 1 then ""user"".username else ""group"".name end) as creatorName
            FROM
                universe
            INNER JOIN
                asset ON asset.id = universe.root_asset_id
            INNER JOIN
                asset_place ON asset_place.asset_id = universe.root_asset_id
            LEFT JOIN
                ""group"" ON ""group"".id = asset.creator_id
            LEFT JOIN
                ""user"" ON ""user"".id = asset.creator_id
            /**where**/
            LIMIT 1000");
        foreach (var id in ids)
        {
            build.OrWhere("universe.id = " + id);
        }

        var result = (await db.QueryAsync<MultiGetUniverseEntry>(temp.RawSql, temp.Parameters)).ToList();
        using var assets = ServiceProvider.GetOrCreate<AssetsService>(this);

        var favorites = await Task.WhenAll(result.Select(c => assets.CountFavorites(c.rootPlaceId)));
        for (var i = 0; i < result.Count; i++)
        {
            result[i].favoritedCount = favorites[i];
        }
        return result;
    }

    public async Task<long> GetTotalVisitsFromUser(long userId)
    {
        AssetsService assets = new AssetsService();
        long totalPlaceVisits = 0;
        var createdPlaces = (await assets.GetCreations(CreatorType.User, userId, Type.Place, 0, 100)).ToArray();
        var placeDetails = (await MultiGetPlaceDetails(createdPlaces
                    .Select(c => c.assetId)))
                .ToArray();
        var universeDetails = await MultiGetUniverseInfo(placeDetails.Select(c => c.universeId));
        foreach (var item in universeDetails)
        {
            totalPlaceVisits += item.visits;
        }
        return totalPlaceVisits;
    }

    public async Task<PlayEntry?> GetOldestPlay(long userId)
    {
        var oldest = await db.QuerySingleOrDefaultAsync<PlayEntry?>(
            "SELECT created_at as createdAt, ended_at as endedAt, asset_id as placeId FROM asset_play_history WHERE user_id = :user_id ORDER BY created_at LIMIT 1", new
            {
                user_id = userId,
            });
        return oldest;
    }

    public async Task<IEnumerable<PlayEntry>> GetRecentGamePlays(long userId, TimeSpan period)
    {
        var date = DateTime.UtcNow.Subtract(period);
        return await db.QueryAsync<PlayEntry>(
            "SELECT created_at as createdAt, ended_at as endedAt, asset_id as placeId FROM asset_play_history WHERE user_id = :user_id AND created_at >= :t", new
            {
                t = date,
                user_id = userId,
            });
    }

    public async Task<IEnumerable<long>> GetRecentGames(long userId, int limit)
    {
        var result = await db.QueryAsync(
            "SELECT asset_play_history.id, asset_id FROM asset_play_history INNER JOIN asset ON asset.id = asset_play_history.asset_id WHERE user_id = :user_id AND asset.moderation_status = :mod_status ORDER BY asset_play_history.id DESC", new
            {
                user_id = userId,
                mod_status = ModerationStatus.ReviewApproved,
            });

        return result.Select(c => (long) c.asset_id).Distinct().Take(limit);
    }

    public static int GetPlayerCount(long placeId)
    {
        /*var query = await db.QuerySingleOrDefaultAsync<Total>(
            "select count(*) as total FROM asset_server_player WHERE asset_server_player.asset_id = :id", new
            {
                id = placeId,
            });
            */
        //return query.total;
        // new code
        int count = 0;
        Dictionary<long, long> playersInGame = GameServerService.CurrentPlayersInGame;
        foreach (var kvp in playersInGame)
        {
            if (kvp.Value == placeId)
            {
                count = count + 1;
            }
        }

        return count;
    }

    public async Task<int> GetVisitCount(long placeId)
    {
        var query = await db.QuerySingleOrDefaultAsync<Total>(
            "select asset_place.visit_count AS total FROM asset_place WHERE asset_place.asset_id = :id", new
            {
                id = placeId,
            });
        return query.total;
    }
    public async Task<IEnumerable<GameListEntry>> GetGamesList(long? contextUserId, string? sortToken, int maxRows, Genre? genre, string? keyword)
    {
        var query = new SqlBuilder();
        var temp = query.AddTemplate(
            @"SELECT
            asset.creator_id as creatorId,
            asset.creator_type as creatorTypeId,
            asset_place.year as year,
            universe_asset.universe_id as universeId,
            asset.name,
            asset.id as placeId,
            asset.description as gameDescription,
            asset.asset_genre as genre,
            (select count(*) as playerCount FROM asset_server_player WHERE asset_server_player.asset_id = asset.id),
            (select count(*) from asset_favorite where asset_id = asset_place.asset_id) as favorite_count,
            (case when asset.creator_type = 1 then ""user"".username else ""group"".name end) as creatorName,
            asset_place.visit_count as visitCount,
            (select count(*) as totalUpVotes from asset_vote where asset_id = asset_place.asset_id and type = :upvote),
            (select count(*) as totalDownVotes from asset_vote where asset_id = asset_place.asset_id and type = :downvote)
            FROM
            asset
            INNER JOIN universe_asset ON universe_asset.asset_id = asset.id
            INNER JOIN asset_place ON asset_place.asset_id = asset.id
            INNER JOIN universe ON universe.id = universe_asset.universe_id
            LEFT JOIN ""group"" ON ""group"".id = asset.creator_id AND asset.creator_type = 2
            LEFT JOIN ""user"" ON ""user"".id = asset.creator_id AND asset.creator_type = 1
            /**where**/
            /**orderby**/
            LIMIT :limit",
            new
            {
            limit = maxRows,
            upvote = AssetVoteType.Upvote,
            downvote = AssetVoteType.Downvote,
            });
        // wheres that apply to all filters
        query.Where("asset.moderation_status = :mod_status", new
        {
            mod_status = ModerationStatus.ReviewApproved,
        });
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query.Where("asset.name ILIKE :keyword", new
            {
                keyword = "%" + keyword + "%",
            });
        }

        if (genre != null && genre != Genre.All && Enum.IsDefined(genre.Value))
        {
            query.Where("asset.asset_genre = :genre", new
            {
                genre = (int) genre,
            });
        }

        var is18Plus = false;
        if (contextUserId != null)
        {
            using var us = ServiceProvider.GetOrCreate<UsersService>();
            is18Plus = await us.Is18Plus(contextUserId.Value);
        }
        if (!is18Plus)
            query.Where("NOT asset.is_18_plus");

        List<long>? sortOrder = null;
        var sortRequired = true;
        switch (sortToken?.ToLower())
        {
            case "recent":
                if (contextUserId is 0 or null)
                    throw new RobloxException(401, 0, "Unauthorized");

                sortOrder = (await GetRecentGames(contextUserId.Value, maxRows)).ToList();
                foreach (var item in sortOrder)
                {
                    query.OrWhere("asset.id = " + item);
                }
                break;
            case "roulette":
                query.OrderBy("RANDOM()");
                sortRequired = false;
                break;
            case "recentlyupdated":
                query.OrderBy("asset.updated_at DESC");
                sortRequired = false;
                break;
            case "recentlycreated":
                query.OrderBy("asset.created_at DESC");
                sortRequired = false;
                break;
            case "mostfavorited":
                // query.Where("");
                query.OrderBy("favorite_count DESC");
                sortRequired = false;
                break;
            default:
                // popular and default are same
                query.OrderBy("playerCount DESC, asset_place.visit_count DESC");
                break;
        }

        var result = await db.QueryAsync<GameListEntry>(temp.RawSql, temp.Parameters);
        // If required, use custom sort
        if (sortOrder != null)
        {
            var newResults = new List<GameListEntry>();
            var oldResult = result.ToList();
            foreach (var id in sortOrder)
            {
                var row = oldResult.FirstOrDefault(c => c.placeId == id);
                if (row != null)
                    newResults.Add(row);
            }

            result = newResults;
        }
        else if (sortRequired)
        {
            // Try to sort by highest player count - should be done by sql but I can't test it right now
            var newResults = result.ToList();
            newResults.Sort((a, b) =>
            {
                return a.playerCount > b.playerCount ? -1 : a.playerCount == b.playerCount ?
                    (a.visitCount > b.visitCount ? -1 : a.visitCount == b.visitCount ? 0 : 1)
                    : 1;
            });
            result = newResults;
        }

        return result;
    }

    public async Task SetPlaceVisibility(long universeId, bool isVisible)
    {
        await db.ExecuteAsync("UPDATE universe SET is_public = :visible WHERE id = :id", new
        {
            id = universeId,
            visible = isVisible,
        });
    }

    public async Task SetRootPlaceId(long universeId, long placeId)
    {
        await db.ExecuteAsync("UPDATE universe SET root_asset_id = :placeId WHERE id = :id", new
        {
            id = universeId,
            placeId,
        });
    }

    public async Task SetYear(long placeId, int year)
    {
        if (year != 2017 && year != 2018 && year != 2019 && year != 2020 && year != 2021)
            throw new ArgumentException("Year can only be 2015, 2016, 2017, 2018, 2019 2020, 2021");

        await db.ExecuteAsync("UPDATE asset_place SET year = :year WHERE asset_id = :id", new
        {
            id = placeId,
            year,
        });
    }
    public async Task SetMaxPlayerCount(long placeId, int maxPlayerCount)
    {
        if (maxPlayerCount < 1)
            throw new RobloxException(400, 0, "Max player count cannot be below 1");
        if (maxPlayerCount > 100)
            throw new RobloxException(400, 0, "Max player count cannot exceed 100");

        await db.ExecuteAsync("UPDATE asset_place SET max_player_count = :max WHERE asset_id = :id", new
        {
            id = placeId,
            max = maxPlayerCount,
        });
    }

    public async Task<IEnumerable<PlaceEntry>> MultiGetPlaceDetails(IEnumerable<long> placeIds)
    {
        var ids = placeIds.Distinct().ToArray();
        if (ids.Length == 0)
            return ArraySegment<PlaceEntry>.Empty;

        var query = new SqlBuilder();
        var temp = query.AddTemplate(
            @"SELECT
                asset.id as universeRootPlaceId,
                asset.creator_id as builderId,
                asset.creator_type as builderType,
                universe_asset.universe_id as universeId,
                asset.name,
                asset.id as placeId,
                asset.description as description,
                asset.asset_genre as genre,
                (select count(*) as playerCount FROM asset_server_player WHERE asset_server_player.asset_id = asset.id),
                (case when ""asset"".creator_type = 1 then ""user"".username else ""group"".name end) as builder,
                asset.created_at as created,
                asset.updated_at as updated,
                asset_place.max_player_count as maxPlayerCount,
                asset_place.year as year,
                asset.moderation_status as moderationStatus
            FROM
                asset
                INNER JOIN universe_asset ON universe_asset.asset_id = asset.id
                INNER JOIN asset_place ON asset_place.asset_id = asset.id
                LEFT JOIN ""group"" ON ""group"".id = asset.creator_id AND asset.creator_type = 2
                LEFT JOIN ""user"" ON ""user"".id = asset.creator_id AND asset.creator_type = 1
            /**where**/
            /**orderby**/
            LIMIT 100");


        foreach (var id in ids)
        {
            query.OrWhere("(asset.asset_type = " + (int) Type.Place + " AND asset.id = " + id + ")");
        }

        return await db.QueryAsync<PlaceEntry>(temp.RawSql, temp.Parameters);
    }
    public async Task<IEnumerable<GamesForCreatorDevelop>> GetGamesForTypeDevelop(CreatorType creatorType, long creatorId, string username, int limit,
        int offset, string? sort, string? accessFilter)
    {
        var qu = await db.QueryAsync<GamesForCreatorEntryDb>(
            @"SELECT u.id, a.name, a.description,
            u.root_asset_id as rootAssetId,
            u.is_public as isPublic,
            ap.visit_count as visitCount,
            a.created_at as created,
            a.updated_at as updated
            FROM universe AS u
            INNER JOIN asset a ON a.id = u.root_asset_id
            INNER JOIN asset_place ap ON ap.asset_id = u.root_asset_id
            WHERE u.creator_type = :type AND u.creator_id = :id
            LIMIT :limit OFFSET :offset",
            new
            {
                type = creatorType,
                id = creatorId,
                limit,
                offset,
            });
        return qu.Select(c => new GamesForCreatorDevelop()
        {
            id = c.id,
            name = c.name,
            description = c.description,
            rootPlaceId = c.rootAssetId,
            isActive = c.isPublic,
            privacyType = c.isPublic ? PrivacyType.Public : PrivacyType.Private,
            creatorType = (int) creatorType,
            creatorTargetId = creatorId,
            creatorName = username,
            created = c.created,
            updated = c.updated,
        });
    }
    public async Task<IEnumerable<GamesForCreatorEntry>> GetGamesForType(CreatorType creatorType, long creatorId, int limit,
        int offset, string? sort, string? accessFilter)
    {
        var qu = await db.QueryAsync<GamesForCreatorEntryDb>(
            @"SELECT u.id, a.name, a.description,
            u.root_asset_id as rootAssetId,
            ap.visit_count as visitCount,
            a.created_at as created,
            a.updated_at as updated
            FROM universe AS u
            INNER JOIN asset a ON a.id = u.root_asset_id
            INNER JOIN asset_place ap ON ap.asset_id = u.root_asset_id
            WHERE u.creator_type = :type AND u.creator_id = :id AND u.is_public = true
            LIMIT :limit OFFSET :offset",
            new
            {
                type = creatorType,
                id = creatorId,
                limit,
                offset,
            });
        return qu.Select(c => new GamesForCreatorEntry()
        {
            id = c.id,
            created = c.created,
            description = c.description,
            name = c.name,
            placeVisits = c.visitCount,
            rootPlaceId = c.rootAssetId,
            rootPlace = new()
            {
                id = c.rootAssetId,
            },
            updated = c.updated,
        });
    }
    public async Task<IEnumerable<UniverseGamePassEntry>> GetGamePassesForUniverse(long universeId, int limit,
        int offset, SortOrder? sort)
    {
        var qu = await db.QueryAsync<UniverseGamePassEntryDb>(
            @"SELECT a.id, a.name,
            a.price_robux as priceRobux,
            a.is_for_sale as isForSale,
            a.sale_count as sales,
            a.created_at as created,
            a.updated_at as updated
            FROM asset AS a
            INNER JOIN asset_gamepass ag ON ag.asset_id = a.id
            WHERE ag.universe_id = :universeId AND a.moderation_status = :acceptedStatus
            LIMIT :limit OFFSET :offset",
            new
            {
                universeId,
                acceptedStatus = ModerationStatus.ReviewApproved,
                limit,
                offset,
            });
        return qu.Select(c => new UniverseGamePassEntry()
        {
            id = c.id,
            name = c.name,
            displayName = c.name,
            productId = c.id,
            price = c.priceRobux,
            isForSale = c.isForSale,
            sales = c.sales,
            updated = c.updated,
            created = c.created
        });
    }
    public async Task<IEnumerable<GamePassDetails>> GetGamePassInfo(long assetId)
    {
        return await db.QueryAsync<GamePassDetails>(
            @"SELECT 
            ag.asset_id as assetId, 
            ag.universe_id as universeId
            FROM asset_gamepass AS ag
            WHERE asset_id = :assetId
            LIMIT 1",
            new { assetId });
    }
    // TODO: gamepass should probably use this too
    public async Task<int> GetUniverseBadgeCount(long universeId)
    {
        var qu = await db.ExecuteScalarAsync<int>(
            @"SELECT COUNT(*)
            FROM asset_badge AS ab
            WHERE ab.universe_id = :universeId",
            new
            { universeId });
        return qu;
    }
    public async Task<IEnumerable<BadgeAssetDetails>> GetBadgesForUniverse(MultiGetUniverseEntry universe, int limit,
        int offset, SortOrder? sort)
    {
        var qu = await db.QueryAsync<BadgeAssetDetailsDb>(
            @"SELECT a.id, a.name, a.description, ab.enabled,
            a.sale_count as awardedCount,
            a.created_at as created,
            a.moderation_status as moderationStatus,
            a.updated_at as updated,
            (
                SELECT COUNT(*) FROM user_transaction AS ut
                                WHERE ut.asset_id = a.id
                                AND ut.created_at >= NOW() - INTERVAL '1 day'
            ) as pastDayAwardedCount,
            (
                SELECT COUNT(*) FROM asset_play_history as aph
                                WHERE aph.asset_id = :rootPlaceId   
                                AND aph.created_at >= NOW() - INTERVAL '1 day'
            ) as pastDayUniverseVisitors
            FROM asset AS a
            INNER JOIN asset_badge ab ON ab.asset_id = a.id
            WHERE ab.universe_id = :universeId
            LIMIT :limit OFFSET :offset",
            new
            {
                universeId = universe.id,
                rootPlaceId = universe.rootPlaceId,
                limit,
                offset,
            });
        return qu.Select(c => new BadgeAssetDetails()
        {
            id = c.id,
            name = c.name,
            description = c.description,
            displayName = c.name,
            displayDescription = c.description,
            enabled = c.enabled && c.moderationStatus == ModerationStatus.ReviewApproved,
            iconImageId = c.id,
            displayIconImageId = c.id,
            moderationStatus = c.moderationStatus,
            created = c.created,
            updated = c.updated,
            statistics = {
                awardedCount = c.awardedCount,
                pastDayAwardedCount = c.pastDayAwardedCount,
                winRatePercentage = Math.Round((decimal)c.pastDayAwardedCount / c.pastDayUniverseVisitors, 1)
            },
            awardingUniverse = {
                id = universe.id,
                name = universe.name,
                rootPlaceId = universe.rootPlaceId
            }
        });
    }
    public async Task<IEnumerable<BadgeDetails>> GetBadgeInfo(long assetId)
    {
        return await db.QueryAsync<BadgeDetails>(
            @"SELECT 
            ab.asset_id as assetId, 
            ab.universe_id as universeId,
            ab.enabled as enabled
            FROM asset_badge AS ab
            WHERE asset_id = :assetId
            LIMIT 1",
            new { assetId });
    }
    // if this src ever gets leaked this is NOT for storing ips, its for matchmaking and for getting the server info
    public async Task<dynamic> GetInfoFromIp(string ip)
    {
        string url = $"http://ip-api.com/json/{ip}";
        HttpClient httpClient = new HttpClient();

        var response = await httpClient.GetStringAsync(url);
        var json = JObject.Parse(response);
        return new
        {
            country = json["country"]!.ToString(),
            countryCode = json["countryCode"]!.ToString(),
            city = json["city"]!.ToString(),
        };
    }
    public async Task<dynamic> GetJoinScript(PlaceEntry placeInfo, UserInfo userInfo, GameServerDb jobInfo,  string characterAppearanceUrl, string clientTicket, string membership, int accountAgeDays, bool generateTeleportJoin, string? cookie)
    {
        var formattedDateTime = DateTime.UtcNow.ToString("M/d/yyyy h:mm:ss tt");

        var joinScript = new
        {
            ClientPort = 0,
            MachineAddress = Configuration.GameServerIp,
            ServerPort = jobInfo.port,
            PingUrl = "",
            PingInterval = 0,
            UserName = userInfo.username,
            SeleniumTestMode = false,
            UserId = userInfo.userId,
            SuperSafeChat = false,
            CharacterAppearance = characterAppearanceUrl,
            ClientTicket = clientTicket,
            NewClientTicket = clientTicket,
            GameChatType = "AllUsers",
            GameId = jobInfo.id.ToString(),
            PlaceId = placeInfo.placeId,
            MeasurementUrl = "",
            WaitingForCharacterGuid = Guid.NewGuid().ToString(),
            BaseUrl = Configuration.BaseUrl,
            ChatStyle = "ClassicAndBubble",
            VendorId = 0,
            ScreenShotInfo = "",
            VideoInfo = "",
            CreatorId = placeInfo.builderId,
            CreatorTypeEnum = "User",
            MembershipType = membership,
            AccountAge = accountAgeDays,
            CookieStoreFirstTimePlayKey = "rbx_evt_ftp",
            CookieStoreFiveMinutePlayKey = "rbx_evt_fmp",
            CookieStoreEnabled = true,
            IsRobloxPlace = placeInfo.builderId == 1,
            GenerateTeleportJoin = generateTeleportJoin,
            IsUnknownOrUnder13 = false,
            SessionId = $"{Guid.NewGuid().ToString()}|{jobInfo.id.ToString()}|0|{Configuration.GameServerIp}|8|{formattedDateTime}|0|null|{cookie}|null|null|null",
            DataCenterId = 0,
            UniverseId = placeInfo.universeId,
            BrowserTrackerId = 0,
            UsePortraitMode = false,
            FollowUserId = 0,
            characterAppearanceId = userInfo.userId,
            /*
            ServerConnections = new List<dynamic>
            {
                new
                {
                    Port = gamseserverPort,
                    Address = Configuration.GameServerIp,
                }
            },
            */
            DisplayName = userInfo.username,
            RobloxLocale = "RobloxLocale",
            GameLocale = "en_us",
            CountryCode = "US"
        };

        return joinScript;
    }

    public dynamic SignJoinScript(long year, dynamic joinScript)
    {

        return year switch
        {
            2015 or 2016 or 2017 => sign.SignJsonResponseForClientFromPrivateKey(joinScript),
            2018 or 2019 or 2020 or 2021 => sign.SignJson2048(joinScript),
            _ => "Fail"
        };
    }
    public async Task<IEnumerable<GameMediaEntry>> GetGameMedia(long placeId)
    {
        return await db.QueryAsync<GameMediaEntry>(
            "SELECT asset_type as assetType, media_asset_id as imageId, media_video_hash as videoHash, media_video_title as videoTitle, is_approved as approved FROM asset_media WHERE asset_id = :id",
            new {id = placeId});
    }
    public async Task<long> GetGameMediaCount(long placeId)
    {
        var result = await db.QuerySingleOrDefaultAsync<Dto.Total>(
            "SELECT COUNT(*) AS total FROM asset_media WHERE asset_id = :id AND is_approved = true", new
            {
                id = placeId,
            });
        return result?.total ?? 0;
    }
    public async Task<CreateUniverseResponse> CreateUniverse(long rootPlaceId)
    {
        return await InTransaction(async _ =>
        {
            var creatorInfo =
                await db.QuerySingleOrDefaultAsync("SELECT creator_id, creator_type FROM asset WHERE id = :id",
                    new {id = rootPlaceId});
            var uni = await InsertAsync("universe", new
            {
                root_asset_id = rootPlaceId,
                is_public = true,
                creator_id = (long) creatorInfo.creator_id,
                creator_type = (int) creatorInfo.creator_type,
            });

            await InsertAsync("universe_asset", new
            {
                asset_id = rootPlaceId,
                universe_id = uni,
            });
            var uni2 = (await MultiGetUniverseInfo(new[] {uni})).FirstOrDefault();
            await InsertAsync("universe_settings", new
            {
                id = uni,
            });
            return new CreateUniverseResponse()
            {
                universeId = uni,
            };
        });
    }

    public bool IsThreadSafe()
    {
        return true;
    }

    public bool IsReusable()
    {
        return false;
    }
}