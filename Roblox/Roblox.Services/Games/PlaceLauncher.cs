
using InfluxDB.Client.Core.Exceptions;
using Roblox;
using Roblox.Dto.Games;
using Roblox.Models.Games;
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

    public async Task<PlaceLaunchResponse> PlaceLauncherAsync(PlaceLaunchRequest plRequest)
    {
        switch (plRequest.request)
        {
            case "RequestGameJob":
                if (plRequest.gameId == null)
                    throw new BadRequestException("Game Id is missing");
                return await RequestGameJob(plRequest.gameId, plRequest.placeId);
            case "RequestGame":
                return await RequestGame(plRequest.placeId, plRequest.userId, plRequest.cookie, plRequest.special, plRequest.username);
            case "CloudEdit":
                return await RequestCloudEdit(plRequest.placeId, plRequest.userId, plRequest.username);
            case "RequestPrivateGame":
                break;
        }
        //default
        return new PlaceLaunchResponse()
        {
            status = (int)JoinStatus.Error,
            message = "An error occured while starting the game."
        };
    }

    public async Task<PlaceLaunchResponse> RequestGameJob(string gameId, long placeId)
    {
        GamesService games = new GamesService();
        if (await games.IsFull(gameId, placeId))
        {
            return new PlaceLaunchResponse()
            {
                jobId = gameId,
                status = (int)JoinStatus.GameFull,
                message = "The game is full."
            };
        }
        return new PlaceLaunchResponse()
        {
            jobId = gameId,
            status = (int)JoinStatus.Joining,
            joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={gameId}&placeId={placeId}",
            authenticationUrl = $"{Roblox.Configuration.BaseUrl}/Login/Negotiate.ashx",
            authenticationTicket = "hi",
            message = $"Joining {gameId}",
        };
    }

    public async Task<PlaceLaunchResponse> RequestGame(long placeId, long userId, string cookie, bool? Special = false, string? username = null)
    {
        GamesService games = new GamesService();
        GameServerService gameServer = new GameServerService();
        UsersService users = new UsersService();
        SignService sign = new SignService();
        var result = await gameServer.GetServerForPlace(placeId, (int)MatchmakingContextId.Default);
        dynamic? joinScript = null;
        string finalTicket;
        if (Special.HasValue && (bool)Special)
        {
            var jobPlayers = await gameServer.GetGameServerPlayers(result.job);
            PlaceEntry uni = (await games.MultiGetPlaceDetails(new[] { placeId })).First();
            string membership;

            DateTime currentUtcDateTime = DateTime.UtcNow;
            string formattedDateTime = currentUtcDateTime.ToString("M/d/yyyy h:mm:ss tt");
            var userInfo = await users.GetUserById((long)userId);
            var membership2 = await users.GetUserMembership((long)userId);
            var accountAgeDays = DateTime.UtcNow.Subtract(userInfo.created).Days;
            if (membership2 == null)
            {
                membership = "None";
            }
            else
            {
                membership = (int)membership2!.membershipType == 4 ? "Premium" : (int)membership2!.membershipType == 3 ? "OutrageousBuildersClub" : (int)membership2.membershipType == 2 ? "TurboBuildersClub" : (int)membership2.membershipType == 1 ? "BuildersClub" : "None";
            }
            string characterAppearanceUrl = $"{Configuration.BaseUrl}/v1/avatar-fetch?userId={userId}&placeId={placeId}";
            finalTicket = sign.GenerateClientTicketV4((long)userId, userInfo.username, characterAppearanceUrl, membership, result.job, formattedDateTime, accountAgeDays, placeId);
            joinScript = await games.GetJoinScript(uni.year, userInfo.username, (long)userId, result.job, placeId, uni.universeId, uni.builderId, characterAppearanceUrl, finalTicket, membership, accountAgeDays, true, cookie);
        }

        if (result.status == JoinStatus.Joining)
        {
            await Roblox.Metrics.GameMetrics.ReportGameJoinPlaceLauncherReturned(placeId);

            return new PlaceLaunchResponse()
            {
                jobId = result.job,
                status = (int)result.status,
                joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={result.job}&placeId={placeId}",
                authenticationUrl = Roblox.Configuration.BaseUrl + "/Login/Negotiate.ashx",
                authenticationTicket = cookie,
                message = $"Server found ({result.job})",
                joinScript = (Special ?? false) ? joinScript ?? "" : ""
            };
        }
        return new PlaceLaunchResponse()
        {
            jobId = (string?)null,
            status = (int)JoinStatus.Loading,
            message = "Server found, loading...",
        };
    }
    public async Task<PlaceLaunchResponse> RequestCloudEdit(long placeId, long userId, string username)
    {
        if (userId != 3 && userId != 16 && userId != 3434 && userId != 52)
        {
            throw new BadRequestException("You are not allowed to join this game.");
        }
        GamesService games = new GamesService();
        GameServerService gameServer = new GameServerService();
        UsersService users = new UsersService();
        SignService sign = new SignService();
        string finalTicket;
        dynamic settings;
        string characterAppearanceUrl = $"{Configuration.BaseUrl}/v1.1/avatar-fetch?userId={userId}&placeId={placeId}";
        var result = await gameServer.GetServerForPlace(placeId, (int)MatchmakingContextId.CloudEdit);
        if (result.status == JoinStatus.Joining)
        {
            PlaceEntry uni = (await games.MultiGetPlaceDetails(new[] { placeId })).First();
            long year = await games.GetYear(placeId);
            string membership;
            var membership2 = await users.GetUserMembership((long)userId);
            DateTime currentUtcDateTime = DateTime.UtcNow;
            string formattedDateTime = currentUtcDateTime.ToString("M/d/yyyy h:mm:ss tt");
            var userInfo = await users.GetUserById((long)userId);
            var accountAgeDays = DateTime.UtcNow.Subtract(userInfo.created).Days;
            if (membership2 == null)
            {
                membership = "None";
            }
            else
            {
                membership = (int)membership2!.membershipType == 4 ? "Premium" : (int)membership2!.membershipType == 3 ? "OutrageousBuildersClub" : (int)membership2.membershipType == 2 ? "TurboBuildersClub" : (int)membership2.membershipType == 1 ? "BuildersClub" : "None";
            }
            switch (uni.year)
            {
                case 2017:
                    finalTicket = sign.GenerateClientTicketV1(userId, username, result.job, characterAppearanceUrl);
                    break;
                case 2018:
                case 2019:
                    finalTicket = sign.GenerateClientTicketV2(userId, username, result.job, characterAppearanceUrl);
                    break;
                case 2020:
                    characterAppearanceUrl = $"http://www.pekora.zip/v1/avatar-fetch?userId={placeId}&placeId={placeId}";
                    finalTicket = sign.GenerateClientTicketV4(userId, username, characterAppearanceUrl, membership, result.job, formattedDateTime, accountAgeDays, placeId);
                    break;
                case 2021:
                    characterAppearanceUrl = $"http://www.pekora.zip/v1/avatar-fetch?userId={placeId}&placeId={placeId}";
                    finalTicket = sign.GenerateClientTicketV4(userId, username, characterAppearanceUrl, membership, result.job, formattedDateTime, accountAgeDays, placeId);
                    break;
                default:
                    throw new InvalidOperationException($"This year does not exist: {uni.year}");
            }
            settings = await games.GetJoinScript(year, username, (long)userId, result.job, placeId, uni.universeId, uni.builderId, characterAppearanceUrl, finalTicket, membership, accountAgeDays, true, null);
            return new PlaceLaunchResponse()
            {
                jobId = result.job,
                status = (int)result.status,
                joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={result.job}&placeId={placeId}",
                authenticationUrl = Roblox.Configuration.BaseUrl + "/Login/Negotiate.ashx",
                settings = settings,
                authenticationTicket = "hi",
                message = $"Joining cloudedit session ({result.job})",
            };
        }
        return new PlaceLaunchResponse()
        {
            jobId = (string?)null,
            status = (int)JoinStatus.Loading,
            message = "Server found, loading...",
        };
    }
}