using System.Diagnostics;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Dapper;
using Roblox.Dto.Games;
using Roblox.Libraries.EasyJwt;
using Roblox.Libraries.Password;
using Roblox.Models.Assets;
using Roblox.Models.Economy;
using Roblox.Models.GameServer;
using Roblox.Services.Exceptions;
using Roblox.Logging;
using System.Collections.Concurrent;
using Roblox.Dto.Users;
using StackExchange.Redis;

namespace Roblox.Services;



public class GameServerService : ServiceBase
{
    public class ArbiterHttpClient : HttpClient
    {

        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(100);

        public ArbiterHttpClient()
        {
            this.BaseAddress = new Uri($"https://arbiter.{Configuration.ShortBaseUrl}/");
            this.DefaultRequestHeaders.Add("PJX-ArbiterAUTH", Configuration.ArbiterAuthorization);
        }

        private async Task<HttpResponseMessage> PostLimitedAsync(string url, HttpContent content)
        {
            await _semaphore.WaitAsync();
            try
            {
                return await base.PostAsync(url, content);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> StartGameServer(StartGameServerRequest request)
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var result = await PostLimitedAsync("start-game-server", content);
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> EvictPlayer(EvictPlayerRequest request)
        {
            var jsonRequest = $"{{ \"gameId\": \"{request.gameId}\", \"userId\": {request.userId}, \"messageVersionId\": {request.messageVersionId} }}";
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

            var result = await PostLimitedAsync("evict-player", content);
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> KillGameServer(KillGameServerRequest request)
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var result = await PostLimitedAsync("kill-game-server", content);
            return result.IsSuccessStatusCode;
        }

        public async Task<bool> SetFilteringEnabled(SetFilteringEnabledRequest request)
        {
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var result = await PostLimitedAsync("set-filtering-enabled", content);
            return result.IsSuccessStatusCode;
        }
        public static SetFilteringEnabledRequest CreateFilteringEnabled(Guid jobId, bool isEnabled)
        {
            return new SetFilteringEnabledRequest
            {
                jobId = jobId,
                isEnabled = isEnabled
            };
        }
        public static EvictPlayerRequest CreateEvictPlayerRequest(Guid jobId, long userId)
        {
            return new EvictPlayerRequest
            {
                gameId = jobId,
                userId = userId,
                messageVersionId = 0
            };
        }
        public static StartGameServerRequest CreateGameServerRequest(PlaceEntry placeInfo, int rccPort, int networkServerPort, int proxyPort, Guid jobId, int matchmaking)
        {
            return new StartGameServerRequest
            {
                jobId = jobId,
                placeId = placeInfo.placeId,
                universeId = placeInfo.universeId,
                maxPlayerCount = placeInfo.maxPlayerCount,
                gameServerPort = networkServerPort,
                rccPort = rccPort,
                proxyPort = proxyPort,
                creatorId = placeInfo.builderId,
                placeVersion = 1,
                matchmakingContextId = matchmaking,
                year = placeInfo.year,
            };
        }
        public static KillGameServerRequest CreateKillGameServerRequest(Guid jobId)
        {
            return new KillGameServerRequest
            {
                jobId = jobId,
            };
        }
        public class SetFilteringEnabledRequest
        {
            public Guid jobId { get; set; }
            public bool isEnabled { get; set; }
        }
        public class EvictPlayerRequest
        {
            public Guid gameId { get; set; }
            public long userId { get; set; }
            public int messageVersionId { get; set; }
        }
        public class StartGameServerRequest
        {
            public Guid jobId { get; set; }
            public long placeId { get; set; }
            public long universeId { get; set; }
            public int maxPlayerCount { get; set; }
            public long gameServerPort { get; set; }
            public long rccPort { get; set; }
            public long proxyPort { get; set; }
            public long creatorId { get; set; }
            public long placeVersion { get; set; }
            public int matchmakingContextId { get; set; }
            public long year { get; set; }
        }

        public class KillGameServerRequest
        {
            public Guid jobId { get; set; }
        }
    }

    private static ArbiterHttpClient arbiterClient = new ArbiterHttpClient();
    private static string jwtKey { get; set; } = string.Empty;
    private static EasyJwt jwt { get; } = new();
    private static Random random = new Random();
    private static readonly TimeSpan LiveServerTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan PlayerPresenceTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan PortReservationTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan JoinReservationTtl = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan LoadingServerStartupTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ReadyServerHeartbeatTimeout = TimeSpan.FromMinutes(5);

    private sealed class LiveGameServerRecord
    {
        public Guid id { get; set; }
        public long assetId { get; set; }
        public long port { get; set; }
        public DateTime updatedAt { get; set; }
        public ServerStatus status { get; set; }
        public int type { get; set; }
        public long ping { get; set; } = -1;
        public long fps { get; set; } = -1;
    }

    private static string ServerKey(Guid jobId) => $"gameserver:v1:{jobId:N}";
    private static string PlaceServersKey(long placeId, int? matchmaking) => $"gameserver:v1:place:{placeId}:type:{matchmaking ?? 1}";
    private static string PlayersKey(Guid jobId) => $"gameserver:v1:players:{jobId:N}";
    private static string PlayerNamesKey(Guid jobId) => $"gameserver:v1:playernames:{jobId:N}";
    private static string UserJobKey(long userId) => $"gameserver:v1:user:{userId}";
    private static string UserPlaceKey(long userId) => $"gameserver:v1:userplace:{userId}";
    private static string UserPlayHistoryKey(long userId) => $"gameserver:v1:userplayhistory:{userId}";
    private static string ActiveUsersKey() => "gameserver:v1:activeusers";
    private static string PortKey(int port) => $"gameserver:v1:port:{port}";
    private static string ReservationsKey(Guid jobId) => $"gameserver:v1:reservations:{jobId:N}";

    private const string ReserveSlotLua = @"
local now = tonumber(ARGV[1])
local cutoff = tonumber(ARGV[2])
local maxPlayers = tonumber(ARGV[3])
local ttlSeconds = tonumber(ARGV[4])
local member = ARGV[5]
redis.call('ZREMRANGEBYSCORE', KEYS[2], '-inf', cutoff)
local active = redis.call('SCARD', KEYS[1])
local reserved = redis.call('ZCARD', KEYS[2])
if active + reserved >= maxPlayers then
    return 0
end
redis.call('ZADD', KEYS[2], now, member)
redis.call('EXPIRE', KEYS[2], ttlSeconds)
return 1";

    private static GameServerDb ToGameServerDb(LiveGameServerRecord record)
    {
        return new GameServerDb
        {
            id = record.id,
            assetId = record.assetId,
            port = record.port,
            updatedAt = record.updatedAt,
            status = record.status,
            type = record.type,
        };
    }

    private async Task<LiveGameServerRecord?> GetLiveServerRecord(Guid jobId)
    {
        var raw = await redis.StringGetAsync(ServerKey(jobId));
        if (raw == null)
            return null;

        return JsonSerializer.Deserialize<LiveGameServerRecord>(raw);
    }

    private async Task SaveLiveServerRecord(LiveGameServerRecord record)
    {
        await redis.StringSetAsync(ServerKey(record.id), JsonSerializer.Serialize(record), LiveServerTtl);
        await redis.SetAddAsync(PlaceServersKey(record.assetId, record.type), record.id.ToString("N"), LiveServerTtl);
    }

    private async Task DeleteLiveServerIndexes(LiveGameServerRecord record)
    {
        await redis.SetRemoveAsync(PlaceServersKey(record.assetId, record.type), record.id.ToString("N"));
    }

    private async Task<int> GetLivePlayerCount(Guid jobId)
    {
        return (await redis.SetMembersAsync(PlayersKey(jobId))).Length;
    }

    private async Task<bool> ReserveJoinSlot(Guid jobId, long userId, int maxPlayers)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var cutoff = now - (long)JoinReservationTtl.TotalMilliseconds;
        var result = await redis.ScriptEvaluateAsync(
            ReserveSlotLua,
            new RedisKey[] { PlayersKey(jobId), ReservationsKey(jobId) },
            new RedisValue[]
            {
                now,
                cutoff,
                maxPlayers,
                Math.Max(1, (long)JoinReservationTtl.TotalSeconds),
                userId.ToString(),
            });
        return (long)result == 1;
    }

    private static bool IsLiveServerStale(GameServerDb server)
    {
        var maxAge = server.status == ServerStatus.Loading
            ? LoadingServerStartupTimeout
            : ReadyServerHeartbeatTimeout;
        return server.updatedAt.Add(maxAge) < DateTime.UtcNow;
    }

    public static ConcurrentDictionary<long, long> currentPlayersInGame = new ConcurrentDictionary<long, long>() { }; // userid, placeid
    public static void Configure(string newJwtKey)
    {
        jwtKey = newJwtKey;
    }

    public async Task OnPlayerJoin(long userId, long placeId, Guid serverId, string? username = null)
    {
        currentPlayersInGame.AddOrUpdate(userId, placeId, (key, oldValue) => placeId);

        await redis.SetAddAsync(PlayersKey(serverId), userId.ToString(), PlayerPresenceTtl);
        await redis.SetAddAsync(ActiveUsersKey(), userId.ToString(), PlayerPresenceTtl);
        if (!string.IsNullOrWhiteSpace(username))
            await redis.StringSetAsync($"{PlayerNamesKey(serverId)}:{userId}", username, PlayerPresenceTtl);
        await redis.StringSetAsync(UserJobKey(userId), serverId.ToString(), PlayerPresenceTtl);
        await redis.StringSetAsync(UserPlaceKey(userId), placeId.ToString(), PlayerPresenceTtl);
        await Roblox.Cache.DistributedCache.redis.GetDatabase(0).SortedSetRemoveAsync(ReservationsKey(serverId), userId.ToString());

        var playHistoryId = await InsertAsync("asset_play_history", new
        {
            asset_id = placeId,
            user_id = userId,
        });
        await redis.StringSetAsync(UserPlayHistoryKey(userId), playHistoryId.ToString(), PlayerPresenceTtl);
        await db.ExecuteAsync("UPDATE asset_place SET visit_count = visit_count + 1 WHERE asset_id = :id", new
        {
            id = placeId,
        });

        // give ticket to creator
        await InTransaction(async _ =>
        {
            using var assets = ServiceProvider.GetOrCreate<AssetsService>(this);
            var placeDetails = await assets.GetAssetCatalogInfo(placeId);
            using var ec = ServiceProvider.GetOrCreate<EconomyService>(this);
            using var cooldown = ServiceProvider.GetOrCreate<CooldownService>(this);
            // Per 100 users there is a 1 day cooldown to earn tickets from visits
            if (await cooldown.TryIncrementBucketCooldown("TicketCreatorPlaceVisit:" + placeId, 100, TimeSpan.FromDays(1)))
            {
                if (placeDetails.creatorType == CreatorType.User)
                {
                    /* 
                        Homestead = 6
                        Bricksmith = 7
                    */
                    using var accountService = ServiceProvider.GetOrCreate<AccountInformationService>(this);
                    var badges = await accountService.GetUserBadges(placeDetails.creatorTargetId);
                    using var games = ServiceProvider.GetOrCreate<GamesService>(this);
                    switch (await games.GetTotalVisitsFromUser(placeDetails.creatorTargetId))
                    {
                        case 100:
                            if (badges.Any(b => b.id == 6))
                            {
                                return 0;
                            }
                            await db.ExecuteAsync("INSERT INTO user_badge (user_id, badge_id) VALUES (:user_id, :badge_id)", new
                            {
                                user_id = placeDetails.creatorTargetId,
                                badge_id = 6,
                            });
                            break;
                        case 1000:
                            if (badges.Any(b => b.id == 7))
                            {
                                return 0;
                            }
                            await db.ExecuteAsync("INSERT INTO user_badge (user_id, badge_id) VALUES (:user_id, :badge_id)", new
                            {
                                user_id = placeDetails.creatorTargetId,
                                badge_id = 7,
                            });
                            break;
                        default:
                            break;
                    }
                }
            }


            return 0;
        });
    }

    public async Task OnPlayerLeave(long userId, long placeId, Guid serverId)
    {
        currentPlayersInGame.TryRemove(userId, out _);

        await redis.SetRemoveAsync(PlayersKey(serverId), userId.ToString());
        await redis.SetRemoveAsync(ActiveUsersKey(), userId.ToString());
        await redis.KeyDeleteAsync($"{PlayerNamesKey(serverId)}:{userId}");
        await redis.KeyDeleteAsync(UserJobKey(userId));
        await redis.KeyDeleteAsync(UserPlaceKey(userId));
        var cachedPlayHistoryId = await redis.StringGetAsync(UserPlayHistoryKey(userId));
        await redis.KeyDeleteAsync(UserPlayHistoryKey(userId));

        AssetPlayEntry? latestSession = null;
        if (long.TryParse(cachedPlayHistoryId, out var playHistoryId))
        {
            latestSession = await db.QuerySingleOrDefaultAsync<AssetPlayEntry>(
                "SELECT id, created_at as createdAt FROM asset_play_history WHERE id = :id AND ended_at IS NULL",
                new { id = playHistoryId });
        }

        if (latestSession == null)
        {
            latestSession = await db.QuerySingleOrDefaultAsync<AssetPlayEntry>(
                "SELECT id, created_at as createdAt FROM asset_play_history WHERE user_id = :user_id AND asset_id = :asset_id AND ended_at IS NULL ORDER BY asset_play_history.id DESC LIMIT 1",
                new
                {
                    user_id = userId,
                    asset_id = placeId,
                });
        }
        if (latestSession != null)
        {
            await db.ExecuteAsync("UPDATE asset_play_history SET ended_at = now() WHERE id = :id", new
            {
                id = latestSession.id,
            });

            if (latestSession.createdAt.Year != DateTime.UtcNow.Year) return;

            var playTimeMinutes = (long)Math.Truncate((DateTime.UtcNow - latestSession.createdAt).TotalMinutes);
            var earnedTickets = Math.Min(playTimeMinutes * 10, 60); // temp cap, might reduce in the future?
            // cap is 10k tickets per 12 hours (about 1k robux)
            const long maxEarningsPerPeriod = 10000;
            using (var ec = ServiceProvider.GetOrCreate<EconomyService>(this))
            {
                var earningsToday =
                    await ec.CountTransactionEarningsOfType(userId, PurchaseType.PlayingGame, null, TimeSpan.FromHours(12));

                if (earningsToday >= maxEarningsPerPeriod)
                    return;
            }

            await InTransaction(async _ =>
            {
                using var ec = ServiceProvider.GetOrCreate<EconomyService>(this);
                await ec.IncrementCurrency(CreatorType.User, userId, CurrencyType.Tickets, earnedTickets);
                await InsertAsync("user_transaction", new
                {
                    amount = earnedTickets,
                    currency_type = CurrencyType.Tickets,
                    user_id_one = userId,
                    user_id_two = 1,
                    type = PurchaseType.PlayingGame,
                    // store id of the game they played as well
                    asset_id = placeId,
                });

                return 0;
            });
        }
    }
    public async Task KickPlayer(long userId)
    {
        Guid jobId = await GetJobIdByUserId(userId);
        await arbiterClient.EvictPlayer(ArbiterHttpClient.CreateEvictPlayerRequest(jobId, userId));
    }
    public async Task KickPlayer(long userId, Guid jobId)
    {
        await arbiterClient.EvictPlayer(ArbiterHttpClient.CreateEvictPlayerRequest(jobId, userId));
    }

    private async Task TryKillGameServerAsync(Guid serverId)
    {
        try
        {
            await arbiterClient.KillGameServer(ArbiterHttpClient.CreateKillGameServerRequest(serverId));
        }
        catch (Exception ex)
        {
            Writer.Info(LogGroup.GameServerJoin, "Error sending kill request for server {0}: {1}\n{2}", serverId, ex.Message, ex.StackTrace);
        }
    }

    public async Task ShutDownServerAsync(Guid serverId, bool waitForArbiter = true)
    {
        await DeleteGameServer(serverId);
        if (waitForArbiter)
            await TryKillGameServerAsync(serverId);
        else
            _ = TryKillGameServerAsync(serverId);
    }


    public static void RemoveAllPlayersFromPlaceId(long placeId)
    {
        List<long> playersToRemove = currentPlayersInGame.Where(kvp => kvp.Value == placeId).Select(kvp => kvp.Key).ToList();

        foreach (var userId in playersToRemove)
        {
            currentPlayersInGame.TryRemove(userId, out _);
        }
    }

    public static long GetUserPlaceId(long userId) // get user game is in
    {
        bool isInGame = currentPlayersInGame.ContainsKey(userId);
        if (!isInGame)
            return 0;

        return currentPlayersInGame[userId];
    }

    public async Task<long> GetUserPlaceIdAsync(long userId)
    {
        var placeId = await redis.StringGetAsync(UserPlaceKey(userId));
        return long.TryParse(placeId, out var parsed) ? parsed : GetUserPlaceId(userId);
    }

    public async Task<IReadOnlyDictionary<long, long>> GetPlayerCountsByPlaceIds(IEnumerable<long> placeIds, int type = 1)
    {
        var result = new Dictionary<long, long>();
        foreach (var placeId in placeIds.Distinct())
        {
            long count = 0;
            foreach (var server in await GetGameServersForPlace(placeId, type))
            {
                count += await GetLivePlayerCount(server.id);
            }
            result[placeId] = count;
        }

        return result;
    }

    public async Task<long> GetTotalActivePlayerCount()
    {
        return (await redis.SetMembersAsync(ActiveUsersKey())).LongLength;
    }

    public async Task<long[]> GetActiveUserIds()
    {
        return (await redis.SetMembersAsync(ActiveUsersKey()))
            .Select(v => long.TryParse(v, out var userId) ? userId : 0)
            .Where(v => v != 0)
            .ToArray();
    }

    public async Task<DateTime> GetLastServerPing(string serverId)
    {
        return Guid.TryParse(serverId, out var jobId) && await GetLiveServerRecord(jobId) is { } record
            ? record.updatedAt
            : DateTime.MinValue;
    }
    public async Task<long> GetServerStat(Guid serverId)
    {
        var record = await GetLiveServerRecord(serverId);
        return record?.ping ?? -1;
    }

    public async Task SetServerStats(string serverId, long ping, long fps)
    {
        if (!Guid.TryParse(serverId, out var jobId))
            return;

        var record = await GetLiveServerRecord(jobId);
        if (record == null)
            return;

        record.ping = ping;
        record.fps = fps;
        record.updatedAt = DateTime.UtcNow;
        await SaveLiveServerRecord(record);
    }
    public async Task SetServerPing(Guid serverId)
    {
        var record = await GetLiveServerRecord(serverId);
        if (record == null)
            return;

        record.updatedAt = DateTime.UtcNow;
        record.status = ServerStatus.Ready;
        await SaveLiveServerRecord(record);
    }

    public async Task DeleteGameServer(Guid serverId)
    {
        var record = await GetLiveServerRecord(serverId);
        if (record != null)
        {
            await DeleteLiveServerIndexes(record);
            await redis.KeyDeleteAsync(PortKey((int)record.port));
        }

        foreach (var userIdRaw in await redis.SetMembersAsync(PlayersKey(serverId)))
        {
            if (long.TryParse(userIdRaw, out var userId))
            {
                await redis.KeyDeleteAsync(UserJobKey(userId));
                await redis.KeyDeleteAsync(UserPlaceKey(userId));
                await redis.KeyDeleteAsync($"{PlayerNamesKey(serverId)}:{userId}");
                await redis.SetRemoveAsync(ActiveUsersKey(), userId.ToString());
            }
        }

        await redis.KeyDeleteAsync(ServerKey(serverId));
        await redis.KeyDeleteAsync(PlayersKey(serverId));
        await redis.KeyDeleteAsync(ReservationsKey(serverId));
    }


   
    public async Task<Guid> GetJobIdByUserId(long userId)
    {
        var result = await redis.StringGetAsync(UserJobKey(userId));
        return Guid.TryParse(result, out var jobId)
            ? jobId
            : throw new RecordNotFoundException("User not found in a job");
    }
    public async Task<GameServerDb> GetGameServer(Guid jobId)
    {
        var record = await GetLiveServerRecord(jobId);
        return record == null ? null! : ToGameServerDb(record);
    }

    public async Task<bool> IsPortTaken(int port)
    {
        return await redis.StringGetAsync(PortKey(port)) != null;
    }
    public async Task<IEnumerable<GameServerDb>> GetGameServersForPlace(long placeId, int? matchmaking = 1)
    {
        var ids = await redis.SetMembersAsync(PlaceServersKey(placeId, matchmaking));
        var servers = new List<GameServerDb>();
        foreach (var id in ids)
        {
            if (!Guid.TryParse(id, out var jobId))
                continue;

            var record = await GetLiveServerRecord(jobId);
            if (record == null)
            {
                await redis.SetRemoveAsync(PlaceServersKey(placeId, matchmaking), id);
                continue;
            }

            servers.Add(ToGameServerDb(record));
        }

        var counts = new Dictionary<Guid, int>();
        foreach (var server in servers)
            counts[server.id] = await GetLivePlayerCount(server.id);

        return servers.OrderBy(s => counts[s.id]).ThenBy(s => s.updatedAt);
    }

    public async Task<GameServerGetOrCreateResponse> GetServerForPlace(PlaceEntry placeInfo, int matchmaking, long? userId = null)
    {
        var gameServers = await GetGameServersForPlace(placeInfo.placeId, matchmaking);
        Writer.Info(LogGroup.GameServerJoin, "GetServerForPlace placeId={0} matchmaking={1} candidateCount={2}", placeInfo.placeId, matchmaking, gameServers.Count());

        foreach (var server in gameServers)
        {
            var currentPlayerCount = await GetLivePlayerCount(server.id);
            var age = DateTime.UtcNow - server.updatedAt;
            Writer.Info(LogGroup.GameServerJoin, "Evaluating live server jobId={0} placeId={1} status={2} ageSeconds={3:F1} players={4}/{5}", server.id, placeInfo.placeId, server.status, age.TotalSeconds, currentPlayerCount, placeInfo.maxPlayerCount);

            if (IsLiveServerStale(server))
            {
                Writer.Info(LogGroup.GameServerJoin, "Removing stale live server jobId={0} placeId={1} status={2} updatedAt={3:O}", server.id, placeInfo.placeId, server.status, server.updatedAt);
                await ShutDownServerAsync(server.id, waitForArbiter: false);
                continue;
            }

            if (currentPlayerCount >= placeInfo.maxPlayerCount)
            {
                continue;
            }

            if (userId.HasValue && !await ReserveJoinSlot(server.id, userId.Value, placeInfo.maxPlayerCount))
            {
                Writer.Info(LogGroup.GameServerJoin, "Join reservation failed jobId={0} placeId={1} userId={2}", server.id, placeInfo.placeId, userId.Value);
                continue;
            }

            Writer.Info(LogGroup.GameServerJoin, "Returning existing live server jobId={0} placeId={1} status={2}", server.id, placeInfo.placeId, server.status);
            return new GameServerGetOrCreateResponse()
            {
                job = server.id,
                ip = Configuration.GameServerIp,
                port = server.port,
                status = server.status == ServerStatus.Ready ? JoinStatus.Joining : JoinStatus.Loading
            };
        }

        // We need to create a lock to prevent multiple requests from creating the same game server
        using var serverCreationLock = await Cache.redLock.CreateLockAsync($"CreateGameServerV1:{placeInfo.placeId}:{matchmaking}", TimeSpan.FromSeconds(10));
        if (!serverCreationLock.IsAcquired)
        {
            Writer.Info(LogGroup.GameServerJoin, "Server creation lock busy placeId={0} matchmaking={1}", placeInfo.placeId, matchmaking);
            return new GameServerGetOrCreateResponse
            {
                status = JoinStatus.Loading,
            };
        }
        Writer.Info(LogGroup.GameServerJoin, "Acquired server creation lock placeId={0} matchmaking={1}", placeInfo.placeId, matchmaking);

        var afterLockServers = await GetGameServersForPlace(placeInfo.placeId, matchmaking);
        foreach (var server in afterLockServers)
        {
            if (IsLiveServerStale(server))
            {
                Writer.Info(LogGroup.GameServerJoin, "Removing stale live server after lock jobId={0} placeId={1} status={2} updatedAt={3:O}", server.id, placeInfo.placeId, server.status, server.updatedAt);
                await ShutDownServerAsync(server.id, waitForArbiter: false);
                continue;
            }

            if (await GetLivePlayerCount(server.id) >= placeInfo.maxPlayerCount)
                continue;

            if (userId.HasValue && !await ReserveJoinSlot(server.id, userId.Value, placeInfo.maxPlayerCount))
            {
                Writer.Info(LogGroup.GameServerJoin, "Join reservation failed after lock jobId={0} placeId={1} userId={2}", server.id, placeInfo.placeId, userId.Value);
                continue;
            }

            Writer.Info(LogGroup.GameServerJoin, "Returning existing live server after lock jobId={0} placeId={1} status={2}", server.id, placeInfo.placeId, server.status);
            return new GameServerGetOrCreateResponse
            {
                job = server.id,
                ip = Configuration.GameServerIp,
                port = server.port,
                status = server.status == ServerStatus.Ready ? JoinStatus.Joining : JoinStatus.Loading
            };
        }

        int mainRCCPort = random.Next(30000, 40000);
        int networkServerPort = random.Next(50000, 60000);
        int proxyPort = 0;
        do
        {
            await Task.Delay(100);
            proxyPort = random.Next(30000, 40000);
            if (await redis.StringSetIfNotExistsAsync(PortKey(proxyPort), "1", PortReservationTtl))
                break;
        } while (true);

        Guid jobId = Guid.NewGuid();


        if (await StartGameServer(placeInfo, mainRCCPort, networkServerPort, proxyPort, jobId, matchmaking))
        {
            var record = new LiveGameServerRecord
            {
                port = proxyPort,
                id = jobId,
                assetId = placeInfo.placeId,
                updatedAt = DateTime.UtcNow,
                status = ServerStatus.Loading,
                type = matchmaking
            };
            await SaveLiveServerRecord(record);
            if (userId.HasValue)
                await ReserveJoinSlot(jobId, userId.Value, placeInfo.maxPlayerCount);
        }
        else
        {
            await redis.KeyDeleteAsync(PortKey(proxyPort));
            return new GameServerGetOrCreateResponse()
            {
                status = JoinStatus.Error
            };
        }

        return new GameServerGetOrCreateResponse()
        {
            job = jobId,
            ip = Configuration.GameServerIp,
            port = proxyPort,
            status = JoinStatus.Waiting
        };
    }


    public async Task<bool> StartGameServer(PlaceEntry placeInfo, int RCCPort, int networkServerPort, int proxyPort, Guid jobId, int matchmaking)
    {
        var request = ArbiterHttpClient.CreateGameServerRequest(placeInfo, RCCPort, networkServerPort, proxyPort, jobId, matchmaking);
        Writer.Info(LogGroup.GameServerJoin, "Starting arbiter game server jobId={0} placeId={1} rccPort={2} networkPort={3} proxyPort={4} matchmaking={5}", jobId, placeInfo.placeId, RCCPort, networkServerPort, proxyPort, matchmaking);
        try
        {
            var started = await arbiterClient.StartGameServer(request);
            Writer.Info(LogGroup.GameServerJoin, "Arbiter start result jobId={0} placeId={1} started={2}", jobId, placeInfo.placeId, started);
            return started;
        }
        catch (Exception ex)
        {
            Writer.Info(LogGroup.GameServerJoin, "Arbiter start failed jobId={0} placeId={1} error={2}\n{3}", jobId, placeInfo.placeId, ex.Message, ex.StackTrace);
            return false;
        }
    }

    public async Task<IEnumerable<GameServerPlayer>> GetGameServerPlayers(Guid serverId)
    {
        var userIds = (await redis.SetMembersAsync(PlayersKey(serverId)))
            .Select(v => long.TryParse(v, out var userId) ? userId : 0)
            .Where(v => v != 0)
            .ToArray();
        if (userIds.Length == 0)
            return Array.Empty<GameServerPlayer>();

        var nameKeys = userIds.Select(userId => $"{PlayerNamesKey(serverId)}:{userId}").ToArray();
        var cachedNames = await redis.StringGetManyAsync(nameKeys);
        var missingNames = userIds
            .Where(userId => !cachedNames.TryGetValue($"{PlayerNamesKey(serverId)}:{userId}", out var name) || string.IsNullOrWhiteSpace(name))
            .ToArray();

        var dbNames = new Dictionary<long, string>();
        if (missingNames.Length != 0)
        {
            await using var connection = await Database.OpenConnectionAsync("GameServer.GetGameServerPlayers.Open");
            var rows = await Database.QueryTimedAsync<GameServerPlayer>(
                connection,
                "GameServer.GetGameServerPlayers.Names",
                "SELECT id as userId, username FROM \"user\" WHERE id = ANY(:ids)",
                new { ids = missingNames });
            dbNames = rows.ToDictionary(row => row.userId, row => row.username);
        }

        return userIds.Select(userId =>
        {
            var key = $"{PlayerNamesKey(serverId)}:{userId}";
            var hasCached = cachedNames.TryGetValue(key, out var cachedName) && !string.IsNullOrWhiteSpace(cachedName);
            return new GameServerPlayer
            {
                userId = userId,
                username = hasCached ? cachedName! : dbNames.GetValueOrDefault(userId, userId.ToString()),
            };
        });
    }

    public async Task<IEnumerable<GameServerEntryWithPlayers>> GetGameServers(long placeId, int offset, int limit, int type = 1)
    {
        var result = (await GetGameServersForPlace(placeId, type))
            .Skip(offset)
            .Take(limit)
            .Select(server => new GameServerEntryWithPlayers
            {
                id = server.id,
                assetId = server.assetId,
            })
            .ToList();

        foreach (var server in result)
        {
            server.players = await GetGameServerPlayers(server.id);
        }
        return result;
    }


}
