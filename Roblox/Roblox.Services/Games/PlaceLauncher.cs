using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Linq;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Roblox.Dto.Games;
using Roblox.Dto.Persistence;
using Roblox.Dto.Users;
using Roblox.Libraries.Assets;
using Roblox.Libraries.FastFlag;
using Roblox.Libraries.RobloxApi;
using Roblox.Logging;
using Roblox.Services.Exceptions;
using Roblox.Models.Assets;
using Roblox.Models.GameServer;
using Roblox.Models.Users;
using Roblox.Services;
using Roblox.Services.App.FeatureFlags;
using JsonSerializer = System.Text.Json.JsonSerializer;
using MultiGetEntry = Roblox.Dto.Assets.MultiGetEntry;
using ServiceProvider = Roblox.Services.ServiceProvider;
using Type = Roblox.Models.Assets.Type;
using System.Text.RegularExpressions;
using InfluxDB.Client.Core.Exceptions;
using Roblox.Exceptions;



public class PlaceLauncherService : ServiceBase
{
    AssetsService assets = new AssetsService();
    GamesService games = new GamesService();
    GameServerService gameServer = new GameServerService();

    public async Task<dynamic> PlaceLauncherAsync(string request, long placeId, bool? isPartyLeader, bool? isTeleport, string? gameId, string? accessCode, string? linkCode, string? privateGameMode)
    {
        switch (request)
        {
            case "RequestGameJob":
                return await RequestGameJob(gameId, placeId);
            case "RequestGame":
                return await RequestGame(placeId);
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
            authenticationTicket = (string)null,
            message = (string)null,
        };
    }
    public async Task<dynamic> RequestGame(long placeId)
    {
        var result = await gameServer.GetServerForPlace(placeId);
        if (result.status != JoinStatus.Joining)
        {
            await Roblox.Metrics.GameMetrics.ReportGameJoinPlaceLauncherReturned(placeId);
            return new
            {
                jobId = (string?)null,
                status = (int)JoinStatus.Loading,
                message = "Server found, loading...",
            };
        }
        return new
        {
            jobId = result.job,
            status = (int)result.status,
            joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={result.job}&placeId={placeId}",
            authenticationUrl = Roblox.Configuration.BaseUrl + "/Login/Negotiate.ashx",
            authenticationTicket = (string?)null,
            message = (string?)null,
        };
    }
}