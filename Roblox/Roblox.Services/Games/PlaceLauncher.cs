
using Roblox.Models.GameServer;

using Roblox.Services;
public class PlaceLauncherService : ServiceBase
{
    public enum MatchmakingContextId
    {
        Default = 1,
        Xbox,
        CloudEdit,
        CloudEditTest,
    }

    public async Task<dynamic> PlaceLauncherAsync(string request, long placeId, bool? isPartyLeader, bool? isTeleport, string? gameId, string? accessCode, string? linkCode, string? privateGameMode)
    {
        switch (request)
        {
            case "RequestGameJob":
                return await RequestGameJob(gameId, placeId);
            case "RequestGame":
                return await RequestGame(placeId, (int)MatchmakingContextId.Default);
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
    public async Task<dynamic> RequestGame(long placeId, int matchmaking)
    {
        GamesService games = new GamesService();
        GameServerService gameServer = new GameServerService();
        var result = await gameServer.GetServerForPlace(placeId, matchmaking);
        if (result.status == JoinStatus.Joining)
        {
            await Roblox.Metrics.GameMetrics.ReportGameJoinPlaceLauncherReturned(placeId);
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
            jobId = (string?)null,
            status = (int)JoinStatus.Loading,
            message = "Server found, loading...",
        };
    }
}