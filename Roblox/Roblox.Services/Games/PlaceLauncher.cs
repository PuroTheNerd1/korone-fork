
using Roblox;
using Roblox.Dto.Games;
using Roblox.Models.GameServer;
using Roblox.Services;
using Roblox.Services.Signer;
namespace Roblox.Services.PlaceLauncher;
public class PlaceLauncherService : ServiceBase
{
    public enum MatchmakingContextId
    {
        Default = 1,
        Xbox,
        CloudEdit,
        CloudEditTest,
    }

    public async Task<dynamic> PlaceLauncherAsync(string request, long placeId, bool? isPartyLeader, bool? isTeleport, string? gameId, string? accessCode, string? linkCode, string? privateGameMode, string? username = null, long? userId = null,  bool? special = false)
    {
        switch (request)
        {
            case "RequestGameJob":
                return await RequestGameJob(gameId, placeId);
            case "RequestGame":
                return await RequestGame(placeId, (int)MatchmakingContextId.Default, special, username, userId);
            case "CloudEdit":
                return await RequestGame(placeId, (int)MatchmakingContextId.CloudEdit);
            case "RequestPrivateGame":
                break;
        }
        //default 
        return new
        {
            status = (int)JoinStatus.Error,
            message = "An error occured while starting the game."  
        };
    }

    public async Task<dynamic> RequestGameJob(string gameId, long placeId)
    {
        GamesService games = new GamesService();
        if (await games.IsFull(gameId, placeId))
        {
            return new
            {
                status = (int)JoinStatus.GameFull,
                message = "Game is full",
            };
        }

        return new
        {
            jobId = gameId,
            status = (int)JoinStatus.Joining,
            joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={gameId}&placeId={placeId}",
            authenticationUrl = $"{Roblox.Configuration.BaseUrl}/Login/Negotiate.ashx",
            authenticationTicket = "hi",
            message = (string)null,
        };
    }
    public async Task<dynamic> RequestGame(long placeId, int matchmaking, bool? Special = false, string? username = null, long? userId = null)
    {
        GamesService games = new GamesService();
        GameServerService gameServer = new GameServerService();
        UsersService users = new UsersService();
        SignService sign = new SignService();
        var result = await gameServer.GetServerForPlace(placeId, matchmaking);
        dynamic joinScript = null;
        string finalTicket;
        if ((bool)Special)
        {
            var jobPlayers = await gameServer.GetGameServerPlayers(result.job);
            PlaceEntry uni = (await games.MultiGetPlaceDetails(new[] { placeId })).First();
            long year = await games.GetYear(placeId);
            string membership;
            var membership2 = await users.GetUserMembership((long)userId);
            DateTime currentUtcDateTime = DateTime.UtcNow;
            string formattedDateTime = currentUtcDateTime.ToString("M/d/yyyy h:mm:ss tt");
            var userInfo = await users.GetUserById((long)userId);
            string characterAppearanceUrl = $"http://www.projex.zip/v1/avatar-fetch?userId={placeId}&placeId={placeId}";

            Console.WriteLine(username);
            var accountAgeDays = DateTime.UtcNow.Subtract(userInfo.created).Days;
            if (membership2 == null)
            {
                membership = "None";
            }
            else
            {
                membership = (int)membership2!.membershipType == 3 ? "OutrageousBuildersClub" : (int)membership2.membershipType == 2 ? "TurboBuildersClub" : (int)membership2.membershipType == 1 ? "BuildersClub" : "None";
            }
            characterAppearanceUrl = $"http://www.projex.zip/v1/avatar-fetch?userId={userId}&placeId={placeId}";
            finalTicket = sign.GenerateClientTicketV4((long)userId, username, characterAppearanceUrl, membership, result.job, formattedDateTime, accountAgeDays, placeId);
            joinScript = new
            {
                ClientPort = 0,
                MachineAddress = "85.215.186.154",
                ServerConnections = new List<dynamic>
                {
                    new
                    {
                        Port = GameServerService.currentGameServerPorts[result.job], 
                        Address = "85.215.186.154", 
                    }
                },
                ServerPort = GameServerService.currentGameServerPorts[result.job], 
                PingUrl = "", 
                PingInterval = 120, 
                UserName = username, 
                DisplayName = username,
                SeleniumTestMode = false, 
                UserId = userId, 
                ClientTicket = finalTicket, 
                SuperSafeChat = false, 
                PlaceId = placeId, 
                MeasurementUrl = "",
                WaitingForCharacterGuid = Guid.NewGuid().ToString(),
                BaseUrl = Configuration.BaseUrl, 
                ChatStyle = "ClassicAndBubble", 
                VendorId = 0,
                ScreenShotInfo = "",
                VideoInfo = "",
                CreatorId = uni.builderId,
                CreatorTypeEnum = "User",  
                MembershipType = membership, 
                AccountAge = accountAgeDays, 
                CookieStoreFirstTimePlayKey = "rbx_evt_ftp",
                CookieStoreFiveMinutePlayKey = "rbx_evt_fmp",
                CookieStoreEnabled = true,
                IsRobloxPlace = false,
                UniverseId = uni.universeId,
                GenerateTeleportJoin = false,
                UsUnknownOrUnder13 = false,
                SessionId = $"{Guid.NewGuid().ToString()}|{result.job}|0|85.215.186.154|8|{formattedDateTime}|0|null|a|null|null|null",
                DataCenterId = 0,
                FollowUserId = 0,
                BrowserTrackerId = 0,
                UsePortraitMode = false,
                CharacterAppearance = $"http://www.projex.zip/v1/avatar-fetch?userId={placeId}&placeId={placeId}",
                GameId = result.job,     
                RobloxLocale = "RobloxLocale",
                GameLocale = "en_us",
                CountryCode = "US",
                characterAppearanceId = userId,
            };
        }



        if (result.status == JoinStatus.Joining)
        {
            await Roblox.Metrics.GameMetrics.ReportGameJoinPlaceLauncherReturned(placeId);
            if ((bool)!Special)
            {
                return new
                {
                    jobId = result.job,
                    status = (int)result.status,
                    joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={result.job}&placeId={placeId}",
                    authenticationUrl = Roblox.Configuration.BaseUrl + "/Login/Negotiate.ashx",
                    authenticationTicket = "hi",
                    message = (string?)null,
                };
            }
            return new
            {
                jobId = result.job,
                status = (int)result.status,
                joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={result.job}&placeId={placeId}",
                authenticationUrl = Roblox.Configuration.BaseUrl + "/Login/Negotiate.ashx",
                authenticationTicket = "hi",
                message = (string?)null,
                joinScript
            };
        }
        return new
        {
            jobId = (string?)null,
            status = (int)JoinStatus.Loading,
            message = "Server found, loading...",
        };
    }
}