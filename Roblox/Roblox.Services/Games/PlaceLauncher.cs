
using Roblox;
using Roblox.Dto.Games;
using Roblox.Dto.Users;
using Roblox.Models.Assets;
using Roblox.Models.Games;
using Roblox.Models.GameServer;
using Roblox.Services;
using Roblox.Services.Exceptions;
using Roblox.Services.Signer;
namespace Roblox.Services.PlaceLauncher;
public class PlaceLauncherService : ServiceBase
{

    private static GamesService games = new GamesService();
    private static GameServerService gameServer = new GameServerService();
    private static UsersService users = new UsersService();
    private static SignService sign = new SignService();


    public async Task<PlaceLaunchResponse> PlaceLauncherAsync(PlaceLaunchRequest request)
    {
        if (request.username == null || request.userId == null || request.cookie == null)
            throw new ArgumentNullException("One of the arguments are missing");

        PlaceEntry placeInfo = (await games.MultiGetPlaceDetails(new[] { request.placeId })).First();

        await IsUserAllowedToJoin((long)request.userId, placeInfo);

        switch (request.request)
        {
            case "RequestGameJob":
                return await RequestGameJob(request.gameId, request.placeId);
            case "RequestGame":
                return await RequestGame(placeInfo, (long)request.userId, request.cookie, request.special, request.username);
            case "CloudEdit":
                return await RequestCloudEdit(placeInfo, (long)request.userId, request.username);
            case "RequestPrivateGame":
                break;
        }
        //default
        throw new PlaceLauncherException(JoinStatus.Error, "An error occured while starting the game.");
    }
    private async Task IsUserAllowedToJoin(long userId, PlaceEntry placeInfo)
    {
        // Let's check if the user is allowed to join the game
        if (!await games.CanUserJoinUniverse(userId, placeInfo.builderId, placeInfo.universeId))
            throw new PlaceLauncherException(JoinStatus.Unauthorized, "You are not allowed to join this game.");
        
        // Check if the place is approved
        if (!placeInfo.IsApproved())
            throw new PlaceLauncherException(JoinStatus.Restricted, "This place is not available for play.");
    }
    public async Task<PlaceLaunchResponse> RequestGameJob(string gameId, long placeId)
    {
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
            joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={gameId}",
            authenticationUrl = $"{Roblox.Configuration.BaseUrl}/Login/Negotiate.ashx",
            authenticationTicket = "hi",
            message = $"Joining {gameId}",
        };
    }

    public async Task<PlaceLaunchResponse> RequestGame(PlaceEntry placeInfo, long userId, string cookie, bool? Special = false, string? username = null)
    {
        dynamic? joinScript = null;

        var result = await gameServer.GetServerForPlace(placeInfo, MatchmakingContext.Default);
        
        if (Special.HasValue && (bool)Special)
        {
            string membership = await users.GetUserMemberShipAsString(userId);
            var userInfo = await users.GetUserById((long)userId);

            string clientTicket = sign.GenerateClientTicket(placeInfo.year, userId, username, userInfo.characterAppearanceUrl, membership, result.job, userInfo.accountAgeDays, placeInfo.placeId);
            joinScript = await games.GetJoinScript(placeInfo, userInfo, result, userInfo.characterAppearanceUrl, clientTicket, membership, userInfo.accountAgeDays, true, cookie);

        }

        if (result.status == JoinStatus.Joining)
        {
            return new PlaceLaunchResponse()
            {
                jobId = result.job,
                status = (int)result.status,
                joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={result.job}",
                authenticationUrl = Roblox.Configuration.BaseUrl + "/Login/Negotiate.ashx",
                authenticationTicket = cookie,
                message = $"Server found ({result.job})",
                joinScript = (Special ?? false) ? joinScript ?? "" : ""
            };
        }
        return new PlaceLaunchResponse()
        {
            status = (int)JoinStatus.Loading,
            message = "Server found, loading...",
        };
    }
    public async Task<PlaceLaunchResponse> RequestCloudEdit(PlaceEntry placeInfo, long userId, string username)
    {
        // Block 2017 due to authentication issues
        if (placeInfo.year == 2017)
            throw new PlaceLauncherException(JoinStatus.Disabled, "You cannot edit places from 2017");

        // Cloud edit check
        var canCloudEdit = await games.CanEditUniverse(userId, placeInfo.universeId) || placeInfo.builderId == userId;
        if (!canCloudEdit)
            throw new PlaceLauncherException(JoinStatus.Unauthorized, "You do not have permission to edit this place.");

        var result = await gameServer.GetServerForPlace(placeInfo, MatchmakingContext.CloudEdit);
        
        if (result.status == JoinStatus.Joining)
        {
            string membership = await users.GetUserMemberShipAsString(userId);
            var userInfo = await users.GetUserById((long)userId);
            string clientTicket = sign.GenerateClientTicket(placeInfo.year, userId, username, userInfo.characterAppearanceUrl, membership, result.job, userInfo.accountAgeDays, placeInfo.placeId);

            dynamic settings = await games.GetJoinScript(placeInfo, userInfo, result, userInfo.characterAppearanceUrl, clientTicket, membership, userInfo.accountAgeDays, false, null);
            return new PlaceLaunchResponse()
            {
                jobId = result.job,
                status = (int)result.status,
                joinScriptUrl = $"{Roblox.Configuration.BaseUrl}/Game/Join.ashx?jobId={result.job}",
                authenticationUrl = Roblox.Configuration.BaseUrl + "/Login/Negotiate.ashx",
                settings = settings,
                authenticationTicket = "hi",
                message = $"Joining cloudedit session ({result.job})",
            };
        }
        return new PlaceLaunchResponse()
        {
            status = (int)JoinStatus.Loading,
            message = "Server found, loading...",
        };
    }
}