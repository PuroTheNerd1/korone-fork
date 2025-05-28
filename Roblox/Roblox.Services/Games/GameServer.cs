using System.Diagnostics;
using System.Net.Http.Headers;
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

namespace Roblox.Services;



public class GameServerService : ServiceBase
{
    public class ArbiterHttpClient : HttpClient
    {
        
        public ArbiterHttpClient()
        {
            this.BaseAddress = new Uri($"https://arbiter.{Configuration.ShortBaseUrl}/");
            this.DefaultRequestHeaders.Add("PJX-ArbiterAUTH", Configuration.ArbiterAuthorization);
        }
        public async Task<bool> StartGameServer(StartGameServerRequest request)
        {
            var result = await this.PostAsync("start-game-server", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
            return result.IsSuccessStatusCode;
        }
        public async Task<bool> EvictPlayer(EvictPlayerRequest request)
        {
            /*
                This is temporary because the JSON doesnt format well
            */
            var jsonRequest = $"{{ \"gameId\": \"{request.gameId}\", \"userId\": {request.userId}, \"messageVersionId\": {request.messageVersionId} }}";
            var result = await this.PostAsync("evict-player", new StringContent(jsonRequest, Encoding.UTF8, "application/json"));
            return result.IsSuccessStatusCode;
        }
        public async Task<bool> KillGameServer(KillGameServerRequest request)
        {
            var result = await this.PostAsync("kill-game-server", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"));
            return result.IsSuccessStatusCode;
        }
        public static EvictPlayerRequest CreateEvictPlayerRequest(string jobId, long userId)
        {
            return new EvictPlayerRequest
            {
                gameId = jobId,
                userId = userId,
                messageVersionId = 0
            };
        }
        public static StartGameServerRequest CreateGameServerRequest(PlaceEntry placeInfo, int rccPort, int networkServerPort, int proxyPort, string jobId, MatchmakingContext matchmaking)
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
                matchmakingContextId = (int)matchmaking,
                year = placeInfo.year,
            };
        }
        public static KillGameServerRequest CreateKillGameServerRequest(string jobId)
        {
            return new KillGameServerRequest
            {
                jobId = jobId,
            };
        }
        public class EvictPlayerRequest
        {
            public string gameId { get; set; }
            public long userId { get; set; }
            public int messageVersionId { get; set; }
        }
        public class StartGameServerRequest
        {
            public string jobId { get; set; }
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
            public string jobId { get; set; }
        }
    }

    private static ArbiterHttpClient arbiterClient = new ArbiterHttpClient();
    private static GamesService games = new GamesService();
    private static string jwtKey { get; set; } = string.Empty;
    private static Random RandomComponent = new Random();
    public static Dictionary<long, long> currentPlayersInGame = new Dictionary<long, long>() { }; // userid, placeid
    public static Dictionary<string, int> unreadyGameServers = new Dictionary<string, int>(); // Process, network server port
    public static void Configure(string newJwtKey)
    {
        jwtKey = "hello world 12345";
    }

    public async Task OnPlayerJoin(long userId, long placeId, string serverId)
    {
        lock (currentPlayersInGame)
        {
            currentPlayersInGame.Remove(userId);
            currentPlayersInGame.Add(userId, placeId);
        }

        await db.ExecuteAsync(
            "INSERT INTO asset_server_player (asset_id, user_id, server_id) VALUES (:asset_id, :user_id, :server_id::uuid)",
            new
            {
                asset_id = placeId,
                user_id = userId,
                server_id = serverId,
            });
        await InsertAsync("asset_play_history", new
        {
            asset_id = placeId,
            user_id = userId,
        });
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
            if (placeDetails.creatorType == CreatorType.Group)
            {
                await InsertAsync("user_transaction", new
                {
                    amount = 10,
                    currency_type = CurrencyType.Tickets,
                    user_id_one = (long?)null,
                    user_id_two = userId,
                    group_id_one = placeDetails.creatorTargetId,
                    type = PurchaseType.PlaceVisit,
                    // store id of the game as well
                    asset_id = placeId,
                });
            }
            else
            {
                if(placeDetails.creatorTargetId == userId)
                {
                    return 0;
                }
                await ec.IncrementCurrency(CreatorType.User, placeDetails.creatorTargetId, CurrencyType.Tickets, 10);
                await InsertAsync("user_transaction", new
                {
                    amount = 10,
                    currency_type = CurrencyType.Tickets,
                    user_id_one = placeDetails.creatorTargetId,
                    user_id_two = userId,
                    type = PurchaseType.PlaceVisit,
                    // store id of the game as well
                    asset_id = placeId,
                });
                /* 
                    Homestead = 6
                    Bricksmith = 7
                */
                using var accountService = ServiceProvider.GetOrCreate<AccountInformationService>(this);
                var badges = await accountService.GetUserBadges(placeDetails.creatorTargetId);
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

            return 0;
        });
    }

    public async Task OnPlayerLeave(long userId, long placeId, string serverId)
    {
        if (!currentPlayersInGame.ContainsKey(userId)) return;
        lock (currentPlayersInGame)
        {
            currentPlayersInGame.Remove(userId);
        }

        await db.ExecuteAsync(
            "DELETE FROM asset_server_player WHERE user_id = :user_id AND server_id = :server_id::uuid", new
            {
                server_id = serverId,
                user_id = userId,
            });
        Console.WriteLine("deleted from db line 195 onplayerleave");
        var latestSession = await db.QuerySingleOrDefaultAsync<AssetPlayEntry>(
            "SELECT id, created_at as createdAt FROM asset_play_history WHERE user_id = :user_id AND asset_id = :asset_id AND ended_at IS NULL ORDER BY asset_play_history.id DESC LIMIT 1",
            new
            {
                user_id = userId,
                asset_id = placeId,
            });
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
        string jobId = await GetJobIdByUserId(userId);
        if (jobId == null) return;
        await arbiterClient.EvictPlayer(ArbiterHttpClient.CreateEvictPlayerRequest(jobId, userId));
    }


    public async Task ShutDownServerAsync(string serverId)
    {
        if(await arbiterClient.KillGameServer(ArbiterHttpClient.CreateKillGameServerRequest(serverId)))
            Console.WriteLine($"GameServer {serverId} was successfully closed!");
        await db.ExecuteAsync("DELETE FROM asset_server_player WHERE server_id = :id::uuid", new {id = serverId});
        await db.ExecuteAsync("DELETE FROM asset_server WHERE id = :id::uuid", new {id = serverId});
        //Console.WriteLine($"GameServer {placeJobId} (place {placeId}) was successfully closed!");
    }

    public static void RemoveAllPlayersFromPlaceId(long placeId)
    {
        List<long> playersToRemove = currentPlayersInGame.Where(kvp => kvp.Value == placeId).Select(kvp => kvp.Key).ToList();

        foreach (var userId in playersToRemove)
        {
            currentPlayersInGame.Remove(userId);
        }
    }

    public static long GetUserPlaceId(long userId) // get user game is in
    {
        bool isInGame = currentPlayersInGame.ContainsKey(userId);
        if (!isInGame)
            return 0;

        return currentPlayersInGame[userId];
    }

    public async Task<DateTime> GetLastServerPing(string serverId)
    {
        var result = await db.QuerySingleOrDefaultAsync("SELECT updated_at FROM asset_server WHERE id = :id::uuid", new
        {
            id = serverId,
        });

        return (DateTime) result.updated_at;
    }
    public async Task<long> GetServerStat(string serverId)
    {
        var result = await db.QuerySingleOrDefaultAsync<long>("SELECT ping FROM asset_server WHERE id = :id::uuid", new
        {
            id = serverId,
        });

        if (result == 0)
            return -1;

        return result;
    }

    public async Task SetServerPing(string serverId, long ping)
    {
        await db.ExecuteAsync("UPDATE asset_server SET updated_at = :u, status = :stat, ping = :ping WHERE id = :id::uuid", new
        {
            u = DateTime.UtcNow,
            stat = (int)ServerStatus.Ready,
            ping = ping, 
            id = serverId,
        });
    }

    public async Task DeleteGameServer(string serverId)
    {
        await db.ExecuteAsync("DELETE FROM asset_server_player WHERE server_id = :id::uuid", new {id = serverId});
        await db.ExecuteAsync("DELETE FROM asset_server WHERE id = :id::uuid", new {id = serverId});
    }

    public async Task<string> GetJobIdByUserId(long userId)
    {
        var result = await db.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT server_id FROM asset_server_player WHERE user_id = :userId",
            new { userId }
        );

        return result.ToString() ?? throw new RecordNotFoundException();
    }
    public async Task<GameServer> GetGameServer(string jobId)
    {
        return await db.QueryFirstOrDefaultAsync<GameServer>(
            "SELECT id, asset_id as assetId, port, updated_at as updatedAt, status, type  FROM asset_server WHERE id = :id::uuid",
            new
            {
                id = Guid.Parse(jobId),
            });
    }

    public async Task<bool> IsPortTaken(int port)
    {
        int result = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT port FROM asset_server WHERE port = :gsport",
            new
            {
                gsport = port,
            });
        return result != 0;
    }
    public async Task<IEnumerable<GameServer>> GetGameServersForPlace(long placeId, MatchmakingContext? matchmaking = MatchmakingContext.Default)
    {
        return await db.QueryAsync<GameServer>(
            "SELECT id, asset_id as assetId, port, updated_at as updatedAt, status, type FROM asset_server WHERE asset_id = :assetid AND type = :type",
            new
            {
                assetid = placeId,
                type = matchmaking,
            });
    }

    public async Task<GameServerGetOrCreateResponse> GetServerForPlace(PlaceEntry placeInfo, MatchmakingContext matchmaking)
    {
        // Get all gamservers for the place, if there are any
        var gameServers = await GetGameServersForPlace(placeInfo.placeId, matchmaking);
        
        if (gameServers != null)
        {
            foreach (var server in gameServers)
            {
                if (gameServers == null)
                    break;
                string jobid = server.id.ToString();
                var currentPlayerCount = await GetGameServerPlayers(jobid);

                // if the server is full continue the search for a good one
                if (currentPlayerCount.Count() >= placeInfo.maxPlayerCount)
                {
                    continue;
                }

                // if the server is older than 5 minutes then shutdown the server
                if (server.updatedAt.AddMinutes(5) < DateTime.UtcNow)
                {
                    await ShutDownServerAsync(jobid);
                    continue;
                }


                // we found a server to join or.... its loading depending
                return new GameServerGetOrCreateResponse()
                {
                    job = jobid,
                    ip = Configuration.GameServerIp,
                    port = server.port,
                    status = server.status == ServerStatus.Ready ? JoinStatus.Joining : JoinStatus.Loading
                };
            }
        }

        int mainRCCPort = RandomComponent.Next(30000, 40000);
        int networkServerPort =  RandomComponent.Next(50000, 60000);;
        int proxyPort = 0;
        do
        {
            proxyPort = RandomComponent.Next(7000, 8000);
            if (!await IsPortTaken(proxyPort))
                break;
            
        } while (true);

        string jobId = Guid.NewGuid().ToString();
        // await using var serverCreationLock = await Cache.redLock.CreateLockAsync("CreateGameServerV1", TimeSpan.FromSeconds(33));
        // if (!serverCreationLock.IsAcquired)
        //     return new GameServerGetOrCreateResponse
        //     {
        //         status = JoinStatus.Loading,
        //     };
       _ = Task.Run(async () => await StartGameServer(placeInfo, mainRCCPort, networkServerPort, proxyPort, jobId, matchmaking));
        await db.ExecuteAsync(
            "INSERT INTO asset_server (id, asset_id, ip, port, server_connection, type) VALUES (:id::uuid, :asset_id, :ip, :port, :server_connection, :type)",
        new
        {
            id = jobId,
            asset_id = placeInfo.placeId,
            ip = Configuration.GameServerIp,
            port = proxyPort,
            server_connection = $"{Configuration.GameServerIp}:{proxyPort}",
            type = matchmaking
        });
        unreadyGameServers.Add(jobId, 0);
        
        while (unreadyGameServers.ContainsKey(jobId))
        {
            await Task.Delay(500);
        }
        return new GameServerGetOrCreateResponse()
        {
            job = jobId,
            ip = Configuration.GameServerIp,
            port = proxyPort,
            status = JoinStatus.Joining
        };
    }


    public async Task<string> StartGameServer(PlaceEntry placeInfo, int RCCPort, int networkServerPort, int proxyPort, string jobId, MatchmakingContext matchmaking)
    {
        Console.WriteLine("Starting Gameserver");
        var request = ArbiterHttpClient.CreateGameServerRequest(placeInfo, RCCPort, networkServerPort, proxyPort, jobId, matchmaking);
        _ = Task.Run(async () => await arbiterClient.StartGameServer(request));
        return "OK";
    }

    [Obsolete]
    public async Task DeleteOldGameServers()
    {
        // first part, do game servers
        var serversToDelete = (await db.QueryAsync<GameServerEntry>("SELECT id::text, asset_id as assetId FROM asset_server WHERE updated_at <= :t", new
        {
            t = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(2)),
        })).ToList();
        Console.WriteLine("[info] there are {0} bad servers", serversToDelete.Count);
        foreach (var server in serversToDelete)
        {
            var players = await GetGameServerPlayers(server.id);
            foreach (var player in players)
            {
                await OnPlayerLeave(player.userId, server.assetId, server.id);
            }
            Console.WriteLine("[info] deleting server {0}", server.id);
            await db.ExecuteAsync("DELETE FROM asset_server_player WHERE server_id = :id::uuid", new
            {
                id = server.id,
            });
            //Console.WriteLine("deleted from db line 706 deleteoldgameservers");
            await db.ExecuteAsync("DELETE FROM asset_server WHERE id = :id::uuid", new
            {
                id = server.id,
            });
        }
        // second part, do game server players
        // this is so ugly jeez
        var orphanedPlayers =
            await db.QueryAsync(
                "SELECT s.id, p.server_id FROM asset_server_player p LEFT JOIN asset_server s ON s.id = p.server_id WHERE s.id IS NULL");
        foreach (var deadbeatDad in orphanedPlayers.Select(c => ((Guid) c.server_id).ToString()).Distinct())
        {
            Console.WriteLine("[info] deleting all orphans for serverId = {0}",deadbeatDad);
            await db.ExecuteAsync("DELETE FROM asset_server_player WHERE server_id = :id::uuid", new
            {
                id = deadbeatDad,
            });
            Console.WriteLine("deleted from db line 724 DeleteOldGameServers");
        }
    }

    public async Task<IEnumerable<GameServerPlayer>> GetGameServerPlayers(string serverId)
    {
        return await db.QueryAsync<GameServerPlayer>(
            "SELECT user_id as userId, u.username FROM asset_server_player INNER JOIN \"user\" u ON u.id = asset_server_player.user_id WHERE server_id = :id::uuid", new
            {
               id = serverId,
            });
    }

    public async Task<IEnumerable<GameServerEntryWithPlayers>> GetGameServers(long placeId, int offset, int limit, MatchmakingContext type = MatchmakingContext.Default)
    {
        var result = (await db.QueryAsync<GameServerEntryWithPlayers>("SELECT id::text, asset_id as assetId FROM asset_server WHERE asset_id = :id AND type = :type LIMIT :limit OFFSET :offset", new
        {
            id = placeId,
            type,
            limit,
            offset,
        })).ToList();

        foreach (var server in result)
        {
            server.players = await GetGameServerPlayers(server.id);
        }
        return result;
    }

    public async Task<IEnumerable<GameServerEntry>> GetGamesUserIsPlaying(long userId)
    {
       return await db.QueryAsync<GameServerEntry>(
            "SELECT s.id::text, s.asset_id as assetId FROM asset_server_player p INNER JOIN asset_server s ON s.id = p.server_id WHERE p.user_id = :id",
            new
            {
                id = userId,
            });
    }

}