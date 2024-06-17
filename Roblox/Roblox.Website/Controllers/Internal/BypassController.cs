using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Roblox.Dto.Games;
using Roblox.Dto.Persistence;
using Roblox.Dto.Users;
using MVC = Microsoft.AspNetCore.Mvc;
using Roblox.Libraries.Assets;
using Roblox.Libraries.FastFlag;
using Roblox.Libraries.RobloxApi;
using Roblox.Logging;
using Roblox.Services.Exceptions;
using BadRequestException = Roblox.Exceptions.BadRequestException;
using Roblox.Models.Assets;
using Roblox.Models.GameServer;
using Roblox.Models.Users;
using Roblox.Services;
using Roblox.Services.App.FeatureFlags;
using Roblox.Website.Controllers.Internal;
using Roblox.Website.Filters;
using Roblox.Website.Middleware;
using Roblox.Website.WebsiteModels.Asset;
using Roblox.Website.WebsiteModels.Games;
using HttpGet = Roblox.Website.Controllers.HttpGetBypassAttribute;
using JsonSerializer = System.Text.Json.JsonSerializer;
using MultiGetEntry = Roblox.Dto.Assets.MultiGetEntry;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;
using ServiceProvider = Roblox.Services.ServiceProvider;
using Type = Roblox.Models.Assets.Type;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using Roblox.Website.WebsiteModels.Authentication;
using System.Text.RegularExpressions;
using InfluxDB.Client.Core.Exceptions;
namespace Roblox.Website.Controllers
{
    [MVC.ApiController]
    [MVC.Route("/")]
    public class BypassController : ControllerBase
    {
        [HttpGetBypass("internal/release-metadata")]
        public dynamic GetReleaseMetaData([Required] string requester)
        {
            throw new RobloxException(RobloxException.BadRequest, 0, "BadRequest");
        }

        [HttpGetBypass("asset/shader")]
        public async Task<MVC.FileResult> GetShaderAsset(long id)
        {
            var isMaterialOrShader = BypassControllerMetadata.materialAndShaderAssetIds.Contains(id);
            if (!isMaterialOrShader)
            {
                // Would redirect but that could lead to infinite loop.
                // Just throw instead
                throw new RobloxException(400, 0, "BadRequest");
            }

            var assetId = id;
            try
            {
                var ourId = await services.assets.GetAssetIdFromRobloxAssetId(assetId);
                assetId = ourId;
            }
            catch (RecordNotFoundException)
            {
                // Doesn't exist yet, so create it
                var migrationResult = await MigrateItem.MigrateItemFromRoblox(assetId.ToString(), false, null, default, new ProductDataResponse()
                {
                    Name = "ShaderConversion" + id,
                    AssetTypeId = Type.Special, // Image
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow,
                    Description = "ShaderConversion1.0",
                });
                assetId = migrationResult.assetId;
            }
            
            var latestVersion = await services.assets.GetLatestAssetVersion(assetId);
            if (latestVersion.contentUrl is null)
            {
                throw new RobloxException(403, 0, "Forbidden"); // ?
            }
            // These files are large, encourage clients to cache them
            HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
            {
                Public = true,
                MaxAge = TimeSpan.FromDays(360),
            }.ToString();
            var assetContent = await services.assets.GetAssetContent(latestVersion.contentUrl);
            return File(assetContent, "application/binary");
        }
        private bool IsRcc()
        {
            var rccAccessKey = Request.Headers.ContainsKey("accesskey") ? Request.Headers["accesskey"].ToString() : null;
            var isRcc = rccAccessKey == Configuration.RccAuthorization;
            return isRcc;
        }
        [HttpGetBypass("v1/asset")]
        [HttpGetBypass("asset")]
        [HttpPostBypass("v1/asset")]
        [HttpPostBypass("asset")]
        public async Task<MVC.ActionResult> GetAssetById(long id, long? assetversionid = null)
        {
            HttpContext.Response.Headers.Add("Cache-Control", "no-cache, no-store");
            HttpContext.Response.Headers.Add("Pragma", "no-cache");
            HttpContext.Response.Headers.Add("Expires", "-1");
            HttpContext.Response.Headers.Add("ExpiresAbsolute", "0");
            // TODO: This endpoint needs to be updated to return a URL to the asset, not the asset itself.
            // The reason for this is so that cloudflare can cache assets without caching the response of this endpoint, which might be different depending on the client making the request (e.g. under 18 user, over 18 user, rcc, etc).
            if(assetversionid != null)
            {
                id = (long)assetversionid;
            }
            if(id == 507766388)
            {
                return PhysicalFile(@"C:\ProjectX\services\Roblox\FixJitter\507766388.rbxm", "application/octet-stream");  
            }
            else if(id == 507766666)
            {
                return PhysicalFile(@"C:\ProjectX\services\Roblox\FixJitter\507766666.rbxm", "application/octet-stream");      
            }
            var is18OrOver = false;
            if (userSession != null)
            {
                is18OrOver = await services.users.Is18Plus(safeUserSession.userId);
            }

            // TEMPORARY UNTIL AUTH WORKS ON STUDIO! REMEMBER TO REMOVE
            if (HttpContext.Request.Headers.ContainsKey("RbxTempBypassFor18PlusAssets"))
            {
                is18OrOver = true;
            }

            var assetId = id;
            var invalidIdKey = "InvalidAssetIdForConversionV1:" + assetId;
            // Opt
            if (Services.Cache.distributed.StringGetMemory(invalidIdKey) != null)
                throw new RobloxException(400, 0, "Asset is invalid or does not exist");
            
            var isBotRequest = Request.Headers["bot-auth"].ToString() == Roblox.Configuration.BotAuthorization;
            var isLoggedIn = userSession != null;
            var encryptionEnabled = !isBotRequest; // bots can't handle encryption :(
#if DEBUG == false
            var userAgent = Request.Headers["User-Agent"].FirstOrDefault()?.ToLower();
            var requester = Request.Headers["Requester"].FirstOrDefault()?.ToLower();
            if (!isBotRequest && !isLoggedIn && (userAgent == null || requester == null || (requester != "client" && requester != "server") || !BypassControllerMetadata.allowedUserAgents.Contains(userAgent)))
            {
                throw new BadRequestException();
            }
#endif

            var isMaterialOrShader = BypassControllerMetadata.materialAndShaderAssetIds.Contains(assetId);
            if (isMaterialOrShader)
            {
                return new MVC.RedirectResult("/asset/shader?id=" + assetId);
            }

            var isRcc = IsRcc();

            if (isRcc)
                encryptionEnabled = false;
#if DEBUG
            encryptionEnabled = false;
#endif
            MultiGetEntry details;
            try 
            {
                details = await services.assets.GetAssetCatalogInfo(assetId);
            } 
            catch (RecordNotFoundException) 
            {
                try
                {
                    var ourId = await services.assets.GetAssetIdFromRobloxAssetId(assetId);
                    assetId = ourId;
                }
                catch (RecordNotFoundException)
                {
                    /*if (await Services.Cache.distributed.StringGetAsync(invalidIdKey) != null)
                        throw new RobloxException(400, 0, "Asset is invalid or does not exist");
                    
                    try
                    {
                        // Doesn't exist yet, so create it
                        var migrationResult = await MigrateItem.MigrateItemFromRoblox(assetId.ToString(), false, null,
                            new List<Type>()
                            {
                                Type.Image,
                                Type.Audio,
                                Type.Mesh,
                                Type.Lua,
                                Type.Model,
                                Type.Decal,
                                Type.Animation,
                                Type.SolidModel,
                                Type.MeshPart,
                                Type.ClimbAnimation,
                                Type.DeathAnimation,
                                Type.FallAnimation,
                                Type.IdleAnimation,
                                Type.JumpAnimation,
                                Type.RunAnimation,
                                Type.SwimAnimation,
                                Type.WalkAnimation,
                                Type.PoseAnimation,
                            }, default, default, true);
                        assetId = migrationResult.assetId;
                    }
                    catch (AssetTypeNotAllowedException)
                    {
                        // TODO: permanently insert as invalid for AssetTypeNotAllowedException in a table
                        await Services.Cache.distributed.StringSetAsync(invalidIdKey,
                            "{}", TimeSpan.FromDays(7));
                        throw new RobloxException(400, 0, "Asset is invalid or does not exist");
                    }
                    catch (Exception e)
                    {
                        // temporary failure? mark as invalid, but only temporarily
                        Writer.Info(LogGroup.AssetDelivery, "Failed to migrate asset " + assetId + " - " + e.Message + "\n" + e.StackTrace);
                        await Services.Cache.distributed.StringSetAsync(invalidIdKey,
                            "{}", TimeSpan.FromMinutes(1));
                        throw new RobloxException(400, 0, "Asset is invalid or does not exist");
                    }
                    */
                    return Redirect($"https://assetdelivery.roblox.com/v1/asset/?id={assetId}");
                }
                details = await services.assets.GetAssetCatalogInfo(assetId);
            }
            if (details.is18Plus && !isRcc && !isBotRequest && !is18OrOver)
                throw new RobloxException(400, 0, "AssetTemporarilyUnavailable");
            if (details.moderationStatus != ModerationStatus.ReviewApproved && !isRcc && !isBotRequest)
                throw new RobloxException(403, 0, "Asset not approved for requester");
            
            var latestVersion = await services.assets.GetLatestAssetVersion(assetId);
            Stream? assetContent = null;
            switch (details.assetType)
            {
                // Special types
                case Roblox.Models.Assets.Type.TeeShirt:
                    return new MVC.FileContentResult(Encoding.UTF8.GetBytes(ContentFormatters.GetTeeShirt(latestVersion.contentId)), "application/binary");
                case Models.Assets.Type.Shirt:
                    return new MVC.FileContentResult(Encoding.UTF8.GetBytes(ContentFormatters.GetShirt(latestVersion.contentId)), "application/binary");
                case Models.Assets.Type.Pants:
                    return new MVC.FileContentResult(Encoding.UTF8.GetBytes(ContentFormatters.GetPants(latestVersion.contentId)), "application/binary");
                // Types that require no authentication and aren't encrypted
                case Models.Assets.Type.Image:
                case Models.Assets.Type.Special:
                    if (latestVersion.contentUrl != null)
                        assetContent = await services.assets.GetAssetContent(latestVersion.contentUrl);
                    // encryptionEnabled = false;
                    break;
                // Types that require no authentication
                case Models.Assets.Type.Audio:
                case Models.Assets.Type.Mesh:
                case Models.Assets.Type.Hat:
                case Models.Assets.Type.Model:
                case Models.Assets.Type.Decal:
                case Models.Assets.Type.Head:
                case Models.Assets.Type.Face:
                case Models.Assets.Type.Gear:
                case Models.Assets.Type.Badge:
                case Models.Assets.Type.Animation:
                case Models.Assets.Type.Torso:
                case Models.Assets.Type.RightArm:
                case Models.Assets.Type.LeftArm:
                case Models.Assets.Type.RightLeg:
                case Models.Assets.Type.LeftLeg:
                case Models.Assets.Type.Package:
                case Models.Assets.Type.GamePass:
                case Models.Assets.Type.Plugin: // TODO: do plugins need auth?
                case Models.Assets.Type.MeshPart:
                case Models.Assets.Type.HairAccessory:
                case Models.Assets.Type.FaceAccessory:
                case Models.Assets.Type.NeckAccessory:
                case Models.Assets.Type.ShoulderAccessory:
                case Models.Assets.Type.FrontAccessory:
                case Models.Assets.Type.BackAccessory:
                case Models.Assets.Type.WaistAccessory:
                case Models.Assets.Type.ClimbAnimation:
                case Models.Assets.Type.DeathAnimation:
                case Models.Assets.Type.FallAnimation:
                case Models.Assets.Type.IdleAnimation:
                case Models.Assets.Type.JumpAnimation:
                case Models.Assets.Type.RunAnimation:
                case Models.Assets.Type.SwimAnimation:
                case Models.Assets.Type.WalkAnimation:
                case Models.Assets.Type.PoseAnimation:
                case Models.Assets.Type.SolidModel:
                    if (latestVersion.contentUrl is null)
                        throw new RobloxException(400, 0, "BadRequest"); // todo: should we log this?
                    if (details.assetType == Models.Assets.Type.Audio)
                    {
                        // Convert to WAV file since that's what web client requires
                        assetContent = await services.assets.GetAudioContentAsWav(assetId, latestVersion.contentUrl);
                    }
                    else
                    {
                        assetContent = await services.assets.GetAssetContent(latestVersion.contentUrl);
                    }
                    break;
                default:
                    // anything else requires auth
                    var ok = false;
                    if (isRcc)
                    {
                        encryptionEnabled = false;
                        var placeIdHeader = Request.Headers["roblox-place-id"].ToString();
                        long placeId = 0;
                        if (!string.IsNullOrEmpty(placeIdHeader))
                        {
                            try
                            {
                                placeId = long.Parse(Request.Headers["roblox-place-id"].ToString());
                            }
                            catch (FormatException)
                            {
                                // Ignore
                            }
                        }
                        // if rcc is trying to access current place, allow through
                        ok = (placeId == assetId);
                        // If game server is trying to load a new place (current placeId is empty), then allow it
                        if (!ok && details.assetType == Models.Assets.Type.Place && placeId == 0)
                        {
                            // Game server is trying to load, so allow it
                            ok = true;
                        }
                        // If rcc is making the request, but it's not for a place, validate the request:
                        if (!ok)
                        {
                            // Check permissions
                            var placeDetails = await services.assets.GetAssetCatalogInfo(placeId);
                            if (placeDetails.creatorType == details.creatorType &&
                                placeDetails.creatorTargetId == details.creatorTargetId)
                            {
                                // We are authorized
                                ok = true;
                            }
                        }
                    }
                    else
                    {
                        // It's not RCC making the request. are we authorized?
                        if (userSession != null)
                        {
                            // Use current user as access check
                            ok = await services.assets.CanUserModifyItem(assetId, safeUserSession.userId);
                            if (!ok)
                            {
                                // Note that all users have access to "Roblox"'s content for legacy reasons
                                ok = (details.creatorType == CreatorType.User && details.creatorTargetId == 1);
                            }
#if DEBUG
                            // If staff, allow access in debug builds
                            if (UsersService.IsUserStaff(userSession.userId))
                            {
                                ok = true;
                            }
#endif
                            // Don't encrypt assets being sent to authorized users - they could be trying to download their own place to give to a friend or something
                            if (ok)
                            {
                                encryptionEnabled = false;
                            }
                        }
                    }

                    if (ok && latestVersion.contentUrl != null)
                    {
                        assetContent = await services.assets.GetAssetContent(latestVersion.contentUrl);
                    }

                    break;
            }

            if (assetContent != null)
            {
                return File(assetContent, "application/binary");
            }

            Console.WriteLine("[info] got BadRequest on /asset/ endpoint");
            throw new BadRequestException();
        }
        [HttpGetBypass("universes/get-universe-containing-place")]
        public async Task<dynamic> GetUniverse(long placeid)
        {
            return new 
            {
                UniverseId = await services.games.GetUniverseId(placeid)
            };
        }
        [HttpGetBypass("Game/LoadPlaceInfo.ashx")]
        public async Task<string> LoadPlaceInfo(long PlaceId)
        {
            var details = await services.assets.GetAssetCatalogInfo(PlaceId);
            // this is just easier for me then using replace all the time on every pcall
            string httpsToHttp = Configuration.BaseUrl.Replace("https", "http");
            string luaCode = $@"
                            pcall(function() game:SetCreatorID({details.creatorTargetId}, Enum.CreatorType.User) end);
                            pcall(function() game:GetService(""SocialService""):SetFriendUrl(""{httpsToHttp}/Game/LuaWebService/HandleSocialRequest.ashx?method=IsFriendsWith&playerid=%d&userid=%d"") end);
                            pcall(function() game:GetService(""SocialService""):SetBestFriendUrl(""{httpsToHttp}/Game/LuaWebService/HandleSocialRequest.ashx?method=IsBestFriendsWith&playerid=%d&userid=%d"") end);
                            pcall(function() game:GetService(""SocialService""):SetGroupUrl(""{httpsToHttp}/Game/LuaWebService/HandleSocialRequest.ashx?method=IsInGroup&playerid=%d&groupid=%d"") end);
                            pcall(function() game:GetService(""SocialService""):SetGroupRankUrl(""{httpsToHttp}/Game/LuaWebService/HandleSocialRequest.ashx?method=GetGroupRank&playerid=%d&groupid=%d"") end);
                            pcall(function() game:GetService(""SocialService""):SetGroupRoleUrl(""{httpsToHttp}/Game/LuaWebService/HandleSocialRequest.ashx?method=GetGroupRole&playerid=%d&groupid=%d"") end);
                            pcall(function() game:GetService(""GamePassService""):SetPlayerHasPassUrl(""{httpsToHttp}/Game/GamePass/GamePassHandler.ashx?Action=HasPass&UserID=%d&PassID=%d"") end);
            ";

            string[] lines = luaCode.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].TrimStart();
            }

            luaCode = string.Join("\n", lines);
            string SignedScript = SignatureController.SignStringResponseForClientFromPrivateKey(luaCode, true);
            return SignedScript;

        }
        [HttpGetBypass("Game/GamePass/GamePassHandler.ashx")]
        public async Task<string> GamePassHandler(string Action, long UserID, long PassID)
        {
            if (Action == "HasPass")
            {
                var has = await services.users.GetUserAssets(UserID, PassID);
                return has.Any() ? "True" : "False";
            }

            throw new NotImplementedException();
        }

        [HttpGetBypass("Game/LuaWebService/HandleSocialRequest.ashx")]
        public async Task<string> LuaSocialRequest([Required, MVC.FromQuery] string method, long? playerid = null, long? groupid = null, long? userid = null)
        {
            // TODO: Implement these
            method = method.ToLower();
            if (method == "isingroup" && playerid != null && groupid != null)
            {
                bool isInGroup = false;
                try
                {
                    if (groupid == 1200769 && await StaffFilter.IsStaff(playerid ?? 0))
                    {
                        isInGroup = true;
                    }
                    var group = await services.groups.GetUserRoleInGroup((long) groupid, (long) playerid);
                    if (group.rank != 0)
                        isInGroup = true;
                }
                catch (Exception)
                {
                    
                }

                return "<Value Type=\"boolean\">"+(isInGroup ? "true" : "false")+"</Value>";
            }

            if (method == "getgrouprank" && playerid != null && groupid != null)
            {
                int rank = 0;
                try
                {
                    var group = await services.groups.GetUserRoleInGroup((long) groupid, (long) playerid);
                    rank = group.rank;
                }
                catch (Exception)
                {
                    
                }

                return "<Value Type=\"integer\">"+rank+"</Value>";
            }

            if (method == "getgrouprole" && playerid != null && groupid != null)
            {
                var groups = await services.groups.GetAllRolesForUser((long) playerid);
                foreach (var group in groups)
                {
                    if (group.groupId == groupid)
                    {
                        return group.name;
                    }
                }

                return "Guest";
            }

            if (method == "isfriendswith" && playerid != null && userid != null)
            {
                var status = (await services.friends.MultiGetFriendshipStatus((long) playerid, new[] {(long) userid})).FirstOrDefault();
                if (status != null && status.status == "Friends")
                {
                    return "<Value Type=\"boolean\">True</Value>";
                }
                return "<Value Type=\"boolean\">False</Value>";

            }

            if (method == "isbestfriendswith")
            {
                return "<Value Type\"boolean\">False</value>";
            }

            throw new NotImplementedException();
        }


        [HttpGetBypass("/auth/submit")]
        public MVC.RedirectResult SubmitAuth(string auth)
        {
            return new MVC.RedirectResult("/");
        }

        [HttpPostBypass("/game/PlaceLauncher.ashx")]
        [HttpGetBypass("/game/PlaceLauncher.ashx")]
        public async Task<dynamic> PlaceLaunch(long placeId, string? jobId = null)
        {     
            FeatureFlags.FeatureCheck(FeatureFlag.GamesEnabled, FeatureFlag.GameJoinEnabled);
            long maxPlayerCount;
            bool isRoblox  = ApplicationGuardMiddleware.IsRoblox(Request);
            if (!isRoblox){
                //return bogus message if the request doesnt contain the roblox user agent
                return new
                {
                    status = (int)JoinStatus.Error,
                    message = "An error occured while starting the game."
                };
            }
            var jobPlayers = await services.gameServer.GetGameServerPlayers(jobId);
            maxPlayerCount = await services.games.GetMaxPlayerCount(placeId);
            if (jobId != null)
            {
                if (jobPlayers.Count() == maxPlayerCount)
                {
                    return new
                    {
                        status = (int)JoinStatus.GameFull,
                        message = "Game is full",
                    };
                }
                else
                {
                    return new
                    {
                        jobId = jobId,
                        status = (int)JoinStatus.Joining,
                        joinScriptUrl = $"{Configuration.BaseUrl}/Game/Join.ashx?jobId={jobId}&placeId={placeId}",
                        authenticationUrl = Configuration.BaseUrl + "/Login/Negotiate.ashx",
                        authenticationTicket = Request.Cookies[".ROBLOSECURITY"],
                        message = (string?)null,
                    };                    
                }                
            }
            long year = await services.games.GetYear(placeId);

            var result = await services.gameServer.GetServerForPlace(placeId, year);
            
            if (result.status == JoinStatus.Joining)
            {
                Thread.Sleep(2500);
                await Roblox.Metrics.GameMetrics.ReportGameJoinPlaceLauncherReturned(placeId);
                return new
                {
                    jobId = result.job,
                    status = (int)result.status,
                    joinScriptUrl = $"{Configuration.BaseUrl}/Game/Join.ashx?jobId={result.job}&placeId={placeId}",
                    authenticationUrl = Configuration.BaseUrl + "/Login/Negotiate.ashx",
                    authenticationTicket = Request.Cookies[".ROBLOSECURITY"],
                    message = (string?)null,
                };
            }

            return new
            {
                jobId = (string?)null,
                status = (int)JoinStatus.UserLeft,
                message = "Server found, loading...",
            };
        }

        public static long startUserId {get;set;} = 30;
#if DEBUG
        [HttpGetBypass("/game/get-join-script-debug")]
        public async Task<dynamic> GetJoinScriptDebug(long placeId, long userId = 12)
        {
            //startUserId = 12;
            var result = services.gameServer.CreateTicket(startUserId, placeId, GetIP());
            startUserId++;
            return new
            {
                placeLauncher = $"{Configuration.BaseUrl}/placelauncher.ashx?ticket={HttpUtility.UrlEncode(result)}",
                authenticationTicket = result,
            };
        }
#endif
        [HttpPostBypass("login/RequestAuth.ashx")]
        [HttpGetBypass("login/RequestAuth.ashx")]
        public async Task<MVC.ActionResult<dynamic?>> StudioRequestAuth()
        {
            Console.WriteLine(userSession.userId);
            if (userSession == null){
                return Unauthorized("User is not authorized.");
            }

            string cookie = HttpContext.Request.Cookies[".ROBLOSECURITY"];
            return Ok($"https://www.projex.zip/Login/Negotiate.ashx?suggest={cookie}");
        }

        [HttpGetBypass("My/Places.aspx")]
        public async Task<MVC.ActionResult<dynamic?>> MyPlaces()
        {
            return Ok();
        }
        [HttpGetBypass("games/list-json")]
        public IActionResult SillyGameJson()
        {
            dynamic gameDetail = new System.Dynamic.ExpandoObject();

            gameDetail.CreatorID = 20;
            gameDetail.CreatorName = "ass";
            gameDetail.CreatorUrl = "https://www.projex.zip/users/20/profile";
            gameDetail.Plays = 1;
            gameDetail.Price = 0;
            gameDetail.ProductID = 0;
            gameDetail.IsOwned = false;
            gameDetail.IsVotingEnabled = true;
            gameDetail.TotalUpVotes = 69;
            gameDetail.TotalDownVotes = 69;
            gameDetail.TotalBought = 69;
            gameDetail.UniverseID = 189;
            gameDetail.HasErrorOcurred = false;
            gameDetail.GameDetailReferralUrl = "https://www.projex.zip/games/189/Natural-Disaster-Survival";
            gameDetail.Url = "https://www.projex.zip/images/thumbnails/e70dd27c44ca8bebebb14f48fbba28c5b5a2ba79ebb1e3c820c3a1e84fc8aed5.png";
            gameDetail.RetryUrl = null;
            gameDetail.Final = true;
            gameDetail.Name = "Natural Disaster Survival";
            gameDetail.PlaceID = 189;
            gameDetail.PlayerCount = 10347;
            gameDetail.ImageId = 2311;

            List<dynamic> gameDetailList = new List<dynamic>(); 
            gameDetailList.Add(gameDetail); 
            gameDetailList.Add(gameDetail); 
            gameDetailList.Add(gameDetail); 
            string jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(gameDetailList); 
            return Content(jsonString, "application/json");
        }
        [HttpGetBypass("game/GetCurrentUser.ashx")]
        public IActionResult GetUserId()
        {
            if (userSession == null){
               return Ok("Bad Request");
            }
            string userIdAsString = userSession.userId.ToString();
            return Content(userIdAsString, "text/plain");
        }
        [HttpPostBypass("v2/login")]
        public async Task<dynamic> LoginV2()
        {
            string requestBody;

            string userAgent;
            userAgent = Request.Headers["User-Agent"]; 
            string username = "";
            string password = "";
            long userId;
            using (StreamReader reader = new StreamReader(HttpContext.Request.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
            }
            if(userAgent == "RobloxStudio/WinInet")
            {
                string[] keyValuePairs = requestBody.Split('&');
                foreach (string pair in keyValuePairs)
                {
                    string[] keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        string key = HttpUtility.UrlDecode(keyValue[0]);
                        string value = HttpUtility.UrlDecode(keyValue[1]);
                        if (key == "username")
                        {
                            username = value;
                        }
                        else if (key == "password")
                        {
                            password = value;
                        }
                    }
                }
            }
            else{
                using (StreamReader reader = new StreamReader(HttpContext.Request.Body, Encoding.UTF8))
                {
                    var serializedResponse = JsonConvert.DeserializeObject<LoginRequestMobile>(requestBody) ?? new LoginRequestMobile();
                    username = serializedResponse.username;
                    password = serializedResponse.password;
                }         
            }


            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new Roblox.Exceptions.ForbiddenException(1, "Username or password is missing.");
            }
            else
            {
                try
                {
                    userId = await services.users.GetUserIdFromUsername(username);

                    if (!await services.users.VerifyPassword(userId, password))
                    {
                        throw new Roblox.Exceptions.ForbiddenException(1, "Incorrect username or password. Please try again");
                    }
                }
                catch (RecordNotFoundException)
                {
                    throw new Roblox.Exceptions.ForbiddenException(1, "Incorrect username or password. Please try again");
                }
            }
            var sess = await services.users.CreateSession(userId);
            var sessionCookie = Roblox.Website.Middleware.SessionMiddleware.CreateJwt(new Middleware.JwtEntry()
            {
                sessionId = sess,
                createdAt = DateTimeOffset.Now.ToUnixTimeSeconds(),
            });
            HttpContext.Response.Cookies.Append(".ROBLOSECURITY", sessionCookie, new CookieOptions()
            {
                Domain = ".projex.zip",
                Secure = false,
                Expires = DateTimeOffset.Now.Add(TimeSpan.FromDays(364)),
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Unspecified,
            });
            var userBalance = await services.economy.GetUserBalance(userId);
            var jsonData = new
            {
                membershipType = 4,
                username = username,
                isUnder13 = false,
                countryCode = "US",
                userId = userId,
                displayName = username
            };
            string jsonString = JsonConvert.SerializeObject(jsonData);
            return Content(jsonString, "application/json");
        }
        [HttpGetBypass("/mobileapi/check-app-version")]
        [HttpPostBypass("/mobileapi/check-app-version")]
        public ActionResult<dynamic> CheckAppVersion()
        {

            dynamic data = new { UpgradeAction = "None" };
            var json = new
            { data = data };

            string jsonString = JsonConvert.SerializeObject(json);
            return Content(jsonString, "application/json");
        }
        [HttpPostBypass("mobileapi/login")]
        public async Task<ContentResult> Login()
        {
            FeatureFlags.FeatureCheck(FeatureFlag.LoginEnabled);
            string username = Request.Form["username"]!;
            string password = Request.Form["password"]!;
            long userId;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new Roblox.Exceptions.ForbiddenException(1, "Username or password is missing.");
            }
            else
            {
                try
                {
                    userId = await services.users.GetUserIdFromUsername(username);

                    if (!await services.users.VerifyPassword(userId, password))
                    {
                        throw new Roblox.Exceptions.ForbiddenException(1, "Incorrect username or password. Please try again");
                    }
                }
                catch (RecordNotFoundException)
                {
                    throw new Roblox.Exceptions.ForbiddenException(1, "Incorrect username or password. Please try again");
                }
            }
            var sess = await services.users.CreateSession(userId);
            var sessionCookie = Roblox.Website.Middleware.SessionMiddleware.CreateJwt(new Middleware.JwtEntry()
            {
                sessionId = sess,
                createdAt = DateTimeOffset.Now.ToUnixTimeSeconds(),
            });
            HttpContext.Response.Cookies.Append(".ROBLOSECURITY", sessionCookie, new CookieOptions()
            {
                Domain = ".projex.zip",
                Secure = false,
                Expires = DateTimeOffset.Now.Add(TimeSpan.FromDays(364)),
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Unspecified,
            });
            var userBalance = await services.economy.GetUserBalance(userId);
            dynamic successJson = new
            {
                Status = "OK",
                UserInfo = new
                {
                    UserName = username,
                    RobuxBalance = userBalance.robux,
                    TicketsBalance = userBalance.tickets,
                    IsAnyBuildersClubMember = true,
                    ThumbnailUrl = $"https://www.projex.zip/Thumbs/Avatar.ashx?userId={userId}",
                    UserID = userId
                }
            };
            string jsonString = JsonConvert.SerializeObject(successJson);
            return Content(jsonString, "application/json");
        }
        [HttpGetBypass("games/start")]
        public void AndroidStart(long placeId)
        {
            Thread.Sleep(2000);
        }
        [HttpGetBypass("login/negotiate.ashx"), HttpGetBypass("login/negotiateasync.ashx"), HttpPostBypass("login/negotiate.ashx")]
        public void Negotiate([Required, MVC.FromQuery] string suggest)
        {
            HttpContext.Response.Cookies.Append(".ROBLOSECURITY", suggest, new CookieOptions
            {
                Domain = ".projex.zip",
                Secure = false,
                Expires = DateTimeOffset.Now.Add(TimeSpan.FromDays(364)),
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
            });
            HttpContext.Response.Headers.Add("Cache-Control", "no-cache, no-store");
            HttpContext.Response.Headers.Add("Pragma", "no-cache");
            HttpContext.Response.Headers.Add("Expires", "-1");
            HttpContext.Response.Headers.Add("ExpiresAbsolute", "0");
        }

        [HttpPostBypass("game/join.ashx")]
        [HttpGetBypass("game/join.ashx")]
        public async Task<dynamic> JoinGame(string jobId, long placeId, bool GenerateTeleportJoin = false)
        {
            Console.WriteLine("Client connected to join.ashx");
            bool isRoblox = ApplicationGuardMiddleware.IsRoblox(Request);
            if (!isRoblox){
                return Redirect("https://www.projex.zip/404");
            }
            var jobPlayers = await services.gameServer.GetGameServerPlayers(jobId);
            PlaceEntry uni = (await services.games.MultiGetPlaceDetails(new[] { placeId })).First();
            long year = await services.games.GetYear(placeId);
            string username = userSession!.username;
            long userId = userSession!.userId;
            string membership;
            var membership2 = await services.users.GetUserMembership(userId);
            DateTime currentUtcDateTime = DateTime.UtcNow;
            string formattedDateTime = currentUtcDateTime.ToString("M/d/yyyy h:mm:ss tt");
            string finalTicket;
            string characterAppearanceUrl = $"{Configuration.BaseUrl}/v1.1/avatar-fetch?userId={userId}";
            if (jobPlayers.Count() >= uni.maxPlayerCount)
            {
                return new
                {
                    error = "The requested game is full",
                    status = 5
                };
            }
            var userInfo = await services.users.GetUserById(userSession!.userId);
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

            switch (year)
            {
                case 2016:
                    characterAppearanceUrl = $"{Configuration.BaseUrl}/Asset/CharacterFetch.ashx?userId={userId}";
                    finalTicket = SignatureController.GenerateClientTicketV1(userId, username, jobId, characterAppearanceUrl);
                    break;
                case 2017:                  
                    finalTicket = SignatureController.GenerateClientTicketV1(userId, username, jobId, characterAppearanceUrl);
                    break;
                case 2018:
                    finalTicket = SignatureController.GenerateClientTicketV2(userId, username, jobId, characterAppearanceUrl);
                    break;
                case 2019:
                    finalTicket = SignatureController.GenerateClientTicketV3(userId, username, jobId, formattedDateTime);
                    break;
                case 2020:
                    finalTicket = SignatureController.GenerateClientTicketV4(userId, username, jobId, formattedDateTime, accountAgeDays, placeId);
                    break;
                default:
                    throw new InvalidOperationException($"This year does not exist: {year}");
            }
            

            FeatureFlags.FeatureCheck(FeatureFlag.GamesEnabled, FeatureFlag.GameJoinEnabled);         
            dynamic joinScript2016 = new
            {
                ClientPort = 0,
                MachineAddress = "194.15.36.134",
                ServerPort = GameServerService.currentGameServerPorts[jobId],
                PingUrl = "",
                PingInterval = 50,
                UserName = username,
                SeleniumTestMode = false,
                UserId = userId,
                SuperSafeChat = false,
                CharacterAppearance = characterAppearanceUrl,
                ClientTicket = finalTicket,
                GameId = jobId,
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
                IsRobloxPlace = uni.builderId == 1,
                GenerateTeleportJoin = false,
                IsUnknownOrUnder13 = false,
                SessionId = "",
                DataCenterId = 0,
                UniverseId = uni.universeId,
                BrowserTrackerId = 0,
                UsePortraitMode = false,
                FollowUserId = 0,
                characterAppearanceId = userId
            };
            dynamic joinScript20172018 = new
            {
                ClientPort = 0,
                MachineAddress = "194.15.36.134",
                ServerPort = GameServerService.currentGameServerPorts[jobId],
                PingUrl = "",
                PingInterval = 120,
                UserName = username,
                SeleniumTestMode = false,
                UserId = userId,
                SuperSafeChat = false,
                CharacterAppearance = characterAppearanceUrl,
                ClientTicket = finalTicket,
                NewClientTicket = finalTicket,
                GameId = jobId,
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
                IsRobloxPlace = uni.builderId == 1,
                GenerateTeleportJoin = GenerateTeleportJoin,
                IsUnknownOrUnder13 = false,
                GameChatType = "AllUsers",
                SessionId = $"{Guid.NewGuid().ToString()}|{jobId}|0|{Configuration.BaseUrl.Replace("https://", "")}|8|{formattedDateTime}|0|null|{Request.Cookies[".ROBLOSECURITY"]}|null|null|null",
                DataCenterId = 0,
                UniverseId = placeId, 
                BrowserTrackerId = 0,
                UsePortraitMode = false,
                FollowUserId = 0,
                characterAppearanceId = 0
            };
            dynamic joinScript20192020 = new
            {
                ClientPort = 0,
                MachineAddress = "194.15.36.134",
                ServerConnections = new List<dynamic>
                {
                    new
                    {
                        Port = GameServerService.currentGameServerPorts[jobId], 
                        Address = "194.15.36.134", 
                    }
                },

                ServerPort = GameServerService.currentGameServerPorts[jobId], 
                PingUrl = "", 
                PingInterval = 120, 
                UserName = username, 
                DisplayName = username,
                SeleniumTestMode = false, 
                UserId = userId, 
                SuperSafeChat = false, 
                CharacterAppearance = characterAppearanceUrl,
                ClientTicket = finalTicket, 
                GameId = jobId, 
                PlaceId = placeId, 
                MeasurementUrl = "",
                WaitingForCharacterGuid = Guid.NewGuid().ToString(),
                BaseUrl = Configuration.BaseUrl, 
                ChatStyle = "ClassicAndBubble", 
                VendorId = 0,
                ScreenShotInfo = "",
                VideoInfo = "",
                CreatorId = 1,
                CreatorTypeEnum = "User",
                MembershipType = "Premium", 
                AccountAge = accountAgeDays, 
                CookieStoreFirstTimePlayKey = "rbx_evt_ftp",
                CookieStoreFiveMinutePlayKey = "rbx_evt_fmp",
                CookieStoreEnabled = true,
                IsRobloxPlace = true,
                GenerateTeleportJoin = false,
                IsUnknownOrUnder13 = false,
                GameChatType = "AllUsers",
                SessionId = $"{Guid.NewGuid().ToString()}|{jobId}|0|{Configuration.BaseUrl.Replace("https://", "")}|8|{formattedDateTime}|0|null|{Request.Cookies[".ROBLOSECURITY"]}|null|null|null",
                AnalyticsSessionId = Guid.NewGuid().ToString(),
                DataCenterId = 0,
                UniverseId = uni.universeId,
                BrowserTrackerId = 0,
                UsePortraitMode = false,
                FollowUserId = 0,
                characterAppearanceId = userId,
                CountryCode = "US"
            };

            HttpContext.Response.Headers.Add("Cache-Control", "no-cache, no-store");
            HttpContext.Response.Headers.Add("Pragma", "no-cache");
            HttpContext.Response.Headers.Add("Expires", "-1");
            HttpContext.Response.Headers.Add("ExpiresAbsolute", "0");

            switch (year)
            {
                case 2016:
                    return SignatureController.SignJsonResponseForClientFromPrivateKey(joinScript2016);
                case 2017:
                    return SignatureController.SignJsonResponseForClientFromPrivateKey(joinScript20172018);
                case 2018:
                    return SignatureController.SignJson2048(joinScript20192020);      
                case 2019:
                case 2020:
                    return SignatureController.SignJson2048(joinScript20192020);
                default:
                    return "Fail";
            }
        }
        [HttpGetBypass("GenerateVersion")]
        public string GenerateVersion()
        {
            return $"version-{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15)}";
        }
        [HttpGetBypass("Asset/CharacterFetch.ashx")]
        public async Task<string> CharacterFetchASHX(long userId)
        {
            var assets = await services.avatar.GetWornAssets(userId);
            return $"{Configuration.BaseUrl}/Asset/BodyColors.ashx?userId={userId};{string.Join(";", assets.Select(c => Configuration.BaseUrl + "/Asset/?id=" + c))}";
        }

        [HttpGetBypass("Asset/FakeCharacterFetch.ashx")]
        public async Task<string> FakeCharacterFetchASHX(long assetId)
        {
            var assets = await services.assets.GetPackageAssets(assetId);
            return $"{Configuration.BaseUrl}/Asset/BodyColors.ashx?userId=2;{string.Join(";", assets.Select(c => Configuration.BaseUrl + "/Asset/?id=" + c))}";
        }
        [HttpGetBypass("v1/avatar-rules")]
        public async Task<IActionResult> AvatarRules()
        {
            AvatarControllerV1 avatar = new AvatarControllerV1();
            var avatarRules = avatar.GetAvatarRules();
            return Ok(avatarRules);
        }
        [HttpPostBypass("v1/avatar/set-body-color")]
        public async Task<dynamic> SetBodyColor()
        {
            return Ok();
        }      
        [HttpGetBypass("v1/avatar/set-scales")]
        public async Task<dynamic> SetScale()
        {
            var result = new
            {
                success = true
            };
            string jsonString = JsonConvert.SerializeObject(result);
            return Content(jsonString, "application/json");           
        }
        [HttpGetBypass("v2/stream-notifications/unread-count")]
        public async Task<dynamic> PushNotif()
        {
            var result = new
            {
                unreadNotifications = 999,
                statusMessage = string.Empty
            };
            string jsonString = JsonConvert.SerializeObject(result);
            return Content(jsonString, "application/json");           
        }        
        [HttpGetBypass("sponsoredpage/list-json")]
        [HttpGetBypass("mobile-ads/v1/get-ad-details")]
        [HttpGetBypass("incoming-items/counts")]
        public async Task<dynamic> IncomingItems()
        {
            var result = new
            {
                success = true
            };
            string jsonString = JsonConvert.SerializeObject(result);
            return Content(jsonString, "application/json");           
        }
        [HttpGetBypass("v1/avatar/metadata")]
        public async Task<IActionResult> AvatarMetadata()
        {
            AvatarControllerV1 avatar = new AvatarControllerV1();
            var avatarMetadata = avatar.GetAvatarMetadata();
            return Ok(avatarMetadata);
        }        
        [HttpGetBypass("v1/avatar")]
        public async Task<IActionResult> MobileCharapp()
        {
            AvatarControllerV1 avatar = new AvatarControllerV1();
            var avatarData = await avatar.GetAvatar(safeUserSession.userId);
            return Ok(avatarData);
        }

        [HttpGetBypass("/v1/avatar-fetch")]
        [HttpGetBypass("/v1.1/avatar-fetch")]
        public async Task<MVC.IActionResult> CharacterFetch(long userId)
        {
            List<long> accessoryVersionIds = new List<long>();
            List<long> equippedGearVersionIds = new List<long>();
            string userAgent = Request.Headers["User-Agent"].ToString();
            var wornAssets = await services.avatar.GetWornAssets(userId);
            var avatar = await services.avatar.GetAvatar(userId);
            dynamic bodyColors = new { HeadColor = avatar.headColorId, LeftArmColor = avatar.leftArmColorId, LeftLegColor = avatar.leftLegColorId, RightArmColor = avatar.rightArmColorId, RightLegColor = avatar.rightLegColorId, TorsoColor = avatar.torsoColorId };
            dynamic scales = new { height = 1, Height = 1, width = 1, Width = 1, head = 1, Head = 1, Depth = 1, depth = 1, proportion = 0, Proportion = 0, bodyType = 0, BodyType = 0};
            string AvatarType = (avatar.avatar_type == 2) ? "R15" : "R6";
            foreach (long assetId in wornAssets)
            {
                var assetInfo = await services.assets.GetAssetCatalogInfo(assetId);
                if (assetInfo.assetType == Type.Gear){
                    equippedGearVersionIds.Add(assetId);
                }

                else{
                    accessoryVersionIds.Add(assetId);
                }
            }
            if (userAgent != "Roblox/Win2020"){
                equippedGearVersionIds = new List<long>();
            }
            var result = new {
                resolvedAvatarType = AvatarType,
                accessoryVersionIds,
                equippedGearVersionIds,
                backpackGearVersionIds = equippedGearVersionIds,
                animationAssetIds = new {},
                playerAvatarType = AvatarType,
                scales,
                bodyColorsUrl = $"{Configuration.BaseUrl}/Asset/BodyColors.ashx?userId={userId}",
                bodyColors
            };
            string jsonString = JsonConvert.SerializeObject(result);
            return Content(jsonString, "application/json");
        }
        private void CheckServerAuth(string auth)
        {
            if (auth != Configuration.GameServerAuthorization)
            {
                Roblox.Metrics.GameMetrics.ReportRccAuthorizationFailure(HttpContext.Request.GetEncodedUrl(),
                    auth, GetRequesterIpRaw(HttpContext));
                throw new BadRequestException();
            }
        }
        [HttpPostBypass("marketplace/purchase")]
        public async Task<dynamic> TestGamepass(long assetId)
        {
            var data = new
            {
                success = "true",
                status = "Bought",
                receipt = "test"
            };

            return Ok(data);
        }
        [HttpGetBypass("marketplace/productinfo")]
        public async Task<dynamic> GetProductInfo(long assetId)
        {
            try
            {
                var details = await services.assets.GetAssetCatalogInfo(assetId);
                return new
                {
                    TargetId = details.id,
                    AssetId = details.id,
                    ProductId = details.id, 
                    Name = details.name,
                    Description = details.description,
                    AssetTypeId = (int)details.assetType,
                    IsForSale = details.isForSale,
                    IsPublicDomain = details.isForSale && details.price == 0,
                    Creator = new
                    {
                        Id = details.creatorTargetId,
                        Name = details.creatorName,
                    }
                };
            }
            catch (RecordNotFoundException)
            {
                return Redirect($"https://economy.roblox.com/v2/assets/{assetId}/details");
            }
        }

        [HttpPostBypass("/gs/activity")]
        public async Task<dynamic> GetGsActivity([Required, MVC.FromBody] ReportActivity request)
        {
            Console.WriteLine(request.authorization);

            CheckServerAuth(request.authorization);
            var result = await services.gameServer.GetLastServerPing(request.serverId);
            return new
            {
                isAlive = result >= DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1)),
                updatedAt = result,
            };
        }

        [HttpPostBypass("/gs/ping")]
        public async Task ReportServerActivity([Required, MVC.FromBody] ReportActivity request)
        {
            CheckServerAuth(request.authorization);
            //await services.gameServer.SetServerGSPing(request.serverId, request.ping);
            await services.gameServer.SetServerPing(request.serverId);
        }

        [HttpPostBypass("/gs/delete")]
        public async Task DeleteServer([Required, MVC.FromBody] ReportActivity request)
        {
            CheckServerAuth(request.authorization);
            await services.gameServer.DeleteGameServer(request.serverId);
        }
        //this is for the newer years that dont have a custom monitoring script
        [HttpPostBypass("presence/register-game-presence")]
        public async Task RegisterGamePresence(long visitorId, long placeId, string gameId, string locationType) 
        {
            bool IsRCC = IsRcc();

            if(!IsRCC)
            {
                return;
            }
            Thread.Sleep(500);
            if(GameServerService.CurrentPlayersInGame.ContainsKey(visitorId))
            {
                return;
            }
            await services.gameServer.OnPlayerJoin(visitorId, placeId, gameId);
        }


        [HttpPostBypass("presence/register-absence")]
        public async Task RegisterGamePresenceAbsence(long visitorId)
        {
            GameServerService gameServerService = new GameServerService();
            string JobId = await gameServerService.GetJobIdByUserId(visitorId);
            bool IsRCC = IsRcc();
            if(!IsRCC)
            {
                return;
            }
            if(!GameServerService.CurrentPlayersInGame.ContainsKey(visitorId))
            {
                return;
            }
            long placeId = GameServerService.GetUserPlaceId(visitorId);

            await gameServerService.OnPlayerLeave(visitorId, placeId, JobId);
        }
        [HttpGetBypass("/device/initialize")]
        [HttpPostBypass("/device/initialize")]
        public ActionResult<dynamic> InitDevice()
        {
            string? appDeviceIdentifier = null;

            var json = new
            {
                browserTrackerId = 1234567890,
                appDeviceIdentifier = appDeviceIdentifier,
            };

            string? jsonString = JsonConvert.SerializeObject(json);
            return Content(jsonString, "application/json");
        }
        [HttpGetBypass("/Game/ClientPresence.ashx")]
        public async Task ClientPresenceAshx(string action, long placeId, long userId, bool IsTeleport)
        {
            GameServerService gameServerService = new GameServerService();
            bool IsRCC = IsRcc();
            if(!IsRCC)
            {
                return;
            }
            if(!GameServerService.CurrentPlayersInGame.ContainsKey(userId))
            {
                return;
            }
            if(action == "disconnect"){
                string JobId = await gameServerService.GetJobIdByUserId(userId);
                await gameServerService.OnPlayerLeave(userId, placeId, JobId);
            }
        }
        [HttpPostBypass("/gs/shutdown")]
        public async Task ShutDownServer([Required, MVC.FromBody] ReportActivity request)
        {
            CheckServerAuth(request.authorization);
            await services.gameServer.ShutDownServerAsync(request.serverId);
        }

        [HttpPostBypass("/gs/players/report")]
        public async Task ReportPlayerActivity([Required, MVC.FromBody] ReportPlayerActivity request)
        {
            CheckServerAuth(request.authorization);
            if (request.eventType == "Leave")
            {
                await services.gameServer.OnPlayerLeave(request.userId, request.placeId, request.serverId);
            }
            else if (request.eventType == "Join")
            {
                await Roblox.Metrics.GameMetrics.ReportGameJoinSuccess(request.placeId);
                await services.gameServer.OnPlayerJoin(request.userId, request.placeId, request.serverId);
            }
            else
            {
                throw new Exception("Unexpected type " + request.eventType);
            }
        }

        [HttpPostBypass("/gs/a")]
        public void ReportGS()
        {
            // Doesn't do anything yet. See: services/api/src/controllers/bypass.ts:1473
            return;
        }
        /*
        [HttpPostBypass("/Game/ValidateTicket.ashx")]
        public async Task<string> ValidateClientTicketRcc([Required, MVC.FromBody] ValidateTicketRequest request)
        {
            try
            {
                // Below is intentionally caught by local try/catch. RCC could crash if we give a 500 error.
                FeatureFlags.FeatureCheck(FeatureFlag.GamesEnabled, FeatureFlag.GameJoinEnabled);
                var ticketData = services.gameServer.DecodeTicket(request.ticket, null);
                if (ticketData.userId != request.expectedUserId)
                {
                    // Either bug or someone broke into RCC
                    Roblox.Metrics.GameMetrics.ReportTicketErrorUserIdNotMatchingTicket(request.ticket,
                        ticketData.userId, request.expectedUserId);
                    throw new Exception("Ticket userId does not match expected userId");
                }
                // From TS: it is possible for a client to spoof username or appearance to be empty string, 
                // so make sure you don't do much validation on those params (aside from assertion that it's a string)
                if (request.expectedUsername != null)
                {
                    var userInfo = await services.users.GetUserById(ticketData.userId);
                    if (userInfo.username != request.expectedUsername)
                    {
                        throw new Exception("Ticket username does not match expected username");
                    }
                }

                if (request.expectedAppearanceUrl != null)
                {
                    // will always be format of "http://localhost/v1.1/avatar-fetch?userId=12", NO EXCEPTIONS!
                    var expectedUrl =
                        $"{Roblox.Configuration.BaseUrl}/v1.1/avatar-fetch?userId={ticketData.userId}";
                    if (request.expectedAppearanceUrl != expectedUrl)
                    {
                        throw new Exception("Character URL is bad");
                    }
                }
                
                // Confirm user isn't already in a game
                var gameStatus = (await services.users.MultiGetPresence(new [] {ticketData.userId})).First();
                if (gameStatus.placeId != null && gameStatus.userPresenceType == PresenceType.InGame)
                {
                    // Make sure that the only game they are playing is the one they are trying to join.
                    var playingGames = await services.gameServer.GetGamesUserIsPlaying(ticketData.userId);
                    foreach (var game in playingGames)
                    {
                        if (game.id != request.gameJobId)
                            throw new Exception("User is already playing another game");
                    }
                }

                return "true";
            }
            catch (Exception e)
            {
                Console.WriteLine("[error] Verify ticket failed. Error = {0}\n{1}", e.Message, e.StackTrace);
                return "false";
            }
        }
        */
        [HttpPostBypass("game/validate-machine")]
        public async Task<dynamic> ValidateMachineAsync()
        {
            HWID hwid = new HWID();
            long userId = safeUserSession.userId;
            bool isBanned = false; 
            string macAddress = null; 

            using (StreamReader reader = new StreamReader(Request.Body))
            {
                string rawBody = await reader.ReadToEndAsync();

                string[] macAddresses = rawBody.Split('&');
                List<string> processedMacAddresses = new List<string>();
                foreach (string macAddressString in macAddresses)
                {
                    string[] parts = macAddressString.Split('=');
                    if (parts.Length == 2)
                    {
                        macAddress = parts[1];
                        isBanned = await hwid.CheckHWID(userId, macAddress);
                        if (!isBanned) 
                        {
                            break;
                        }                        
                    }
                }
            }

            if (macAddress == null)
            {
                return new
                {
                    success = false,
                    message = "Invalid Data",
                };
            }

            return new
            {
                success = isBanned,
                message = "",
            };
        }

        [HttpGetBypass("/my/balance")]
        public async Task<ActionResult<dynamic>> MyBalance()
        {

            var bal = await services.economy.GetUserRobux(safeUserSession.userId);
            var json = new
            {
                robux = bal
            };

            string? jsonString = JsonConvert.SerializeObject(json);
            return Content(jsonString, "application/json");
        }
        [HttpGetBypass("Users/ListStaff.ashx")]
        public async Task<dynamic> GetStaffList()
        {
            if(!IsRcc()) return Redirect("/404");
            return (await StaffFilter.GetStaff()).Where(c => c != 12);
        }

        [HttpGetBypass("Users/GetBanStatus.ashx")]
        public async Task<IEnumerable<dynamic>> MultiGetBanStatus(string userIds)
        {

            var ids = userIds.Split(",").Select(long.Parse).Distinct();
            var result = new List<dynamic>();
#if DEBUG
            return ids.Select(c => new
            {
                userId = c,
                isBanned = false,
            });
#else
            var multiGetResult = await services.users.MultiGetAccountStatus(ids);
            foreach (var user in multiGetResult)
            {
                result.Add(new
                {
                    userId = user.userId,
                    isBanned = user.accountStatus != AccountStatus.Ok,
                });
            }

            return result;
#endif
        }

        [HttpGetBypass("Asset/BodyColors.ashx")]
        public async Task<string> GetBodyColors(long userId)
        {
            var colors = await services.avatar.GetAvatar(userId);

            var xsi = XNamespace.Get("http://www.w3.org/2001/XMLSchema-instance");

            var robloxRoot = new XElement("roblox",
                new XAttribute(XNamespace.Xmlns + "xmime", "http://www.w3.org/2005/05/xmlmime"),
                new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                new XAttribute(xsi + "noNamespaceSchemaLocation", "http://www.roblox.com/roblox.xsd"),
                new XAttribute("version", 4)
            );
            robloxRoot.Add(new XElement("External", "null"));
            robloxRoot.Add(new XElement("External", "nil"));
            var items = new XElement("Item", new XAttribute("class", "BodyColors"));
            var properties = new XElement("Properties");
            // set colors
            properties.Add(new XElement("int", new XAttribute("name", "HeadColor"), colors.headColorId.ToString()));
            properties.Add(new XElement("int", new XAttribute("name", "LeftArmColor"), colors.leftArmColorId.ToString()));
            properties.Add(new XElement("int", new XAttribute("name", "LeftLegColor"), colors.leftLegColorId.ToString()));
            properties.Add(new XElement("string", new XAttribute("name", "Name"), "Body Colors"));
            properties.Add(new XElement("int", new XAttribute("name", "RightArmColor"), colors.rightArmColorId.ToString()));
            properties.Add(new XElement("int", new XAttribute("name", "RightLegColor"), colors.rightLegColorId.ToString()));
            properties.Add(new XElement("int", new XAttribute("name", "TorsoColor"), colors.torsoColorId.ToString()));
            properties.Add(new XElement("bool", new XAttribute("name", "archivable"), "true"));
            // add
            items.Add(properties);
            robloxRoot.Add(items);
            // return as string
            return new XDocument(robloxRoot).ToString();
        }
        
        [HttpPostBypass("Game/PlaceVisit.ashx")]       
        [HttpGetBypass("Game/PlaceVisit.ashx")]
        public async Task<dynamic> PlaceVisit()
        {
            return Ok();
        }
        [HttpGetBypass("rcc/killallservers")]
        public async Task<dynamic> ShutdownServersForPlace(long placeId)
        {
            string jobId;
            var serverjobs = await services.gameServer.GetGameServersForPlace(placeId);
            bool canManagePlace = await services.assets.CanUserModifyItem(placeId, safeUserSession.userId);
            if (canManagePlace)
            {
                foreach (var jobs in serverjobs)
                {
                    jobId = jobs.jobid.ToString(); 
                    await services.gameServer.ShutDownServerAsync(jobId);
                }
                return "OK!";
            }
            else{
                return "Unauthorized";
            }
        }
        [HttpGetBypass("rcc/kickplayer")]
        public async Task<dynamic> KickPlayerAsync(long userId, string reason)
        {
            GameServerService gameServerService = new GameServerService();
            bool isOwner = userSession != null && StaffFilter.IsOwner(safeUserSession.userId);
            if (safeUserSession.userId == userId)
            {
                return "You can't kick yourself!";
            }
            if (isOwner)
            {
                try
                {
                    await gameServerService.KickPlayer(userId, reason);
                }
                catch (Exception)
                {
                    return "failed to kick";
                }
                return $"Kicked player {userId} with reason: {reason}";
            }
            else
            {
                return "not the owner";
            }
        }

        [HttpGetBypass("/Game/ChatFilter.ashx")]
        public string RCC_GetChatFilter()
        {
            return "True";
        }
        [HttpPostBypass("moderation/filtertext/")]
        public dynamic GetModerationText()
        {
            var text = services.filter.FilterText(HttpContext.Request.Form["text"].ToString());
            if (services.filter.ContainsCyrillic(text))
            {
                text = "I will speak english";
            }
            return new
            {
                success = true,
                data = new 
                {
                    white = text,
                    black = text
                }
            };
        }
        [HttpPostBypass("moderation/v2/filtertext/")]
        public dynamic GetModerationTextV2()
        {
            var text = services.filter.FilterText(HttpContext.Request.Form["text"].ToString());
            if (services.filter.ContainsCyrillic(text))
            {
                text = "I will speak english";
            }
            var json = new
            {
                success = true,
                data = new
                {
                    AgeUnder13 = text,
                    Age13OrOver = text,
                }
            };
            string jsonString = JsonConvert.SerializeObject(json);
            return Content(jsonString, "application/json");
        }

        private void ValidateBotAuthorization()
        {
#if DEBUG == false
	        if (Request.Headers["bot-auth"].ToString() != Roblox.Configuration.BotAuthorization)
	        {
		        throw new Exception("Intern al");
	        }
#endif
        }

        [HttpGetBypass("botapi/migrate-alltypes")]
        public async Task<dynamic> MigrateAllItemsBot([Required, MVC.FromQuery] string url)
        {
            ValidateBotAuthorization();
            return await MigrateItem.MigrateItemFromRoblox(url, false, null, new List<Type>()
            {
                Type.Image,
                Type.Audio,
                Type.Mesh,
                Type.Lua,
                Type.Model,
                Type.Decal,
                Type.Animation,
                Type.SolidModel,
                Type.MeshPart,
                Type.GamePass,
                Type.ClimbAnimation,
                Type.DeathAnimation,
                Type.FallAnimation,
                Type.IdleAnimation,
                Type.JumpAnimation,
                Type.RunAnimation,
                Type.SwimAnimation,
                Type.WalkAnimation,
                Type.PoseAnimation,
            }, default, false);
        }

        [HttpGetBypass("botapi/migrate-clothing")]
        public async Task<dynamic> MigrateClothingBot([Required] string assetId)
        {
            ValidateBotAuthorization();
            return await MigrateItem.MigrateItemFromRoblox(assetId, true, 5, new List<Models.Assets.Type>() { Models.Assets.Type.TeeShirt, Models.Assets.Type.Shirt, Models.Assets.Type.Pants });
        }

        [HttpGetBypass("BuildersClub/Upgrade.ashx")]
        public MVC.IActionResult UpgradeNow()
        {
            return new MVC.RedirectResult("/internal/membership");
        }
        [HttpGetBypass("game/players/{userId}")]
        public MVC.ActionResult<dynamic> ChatWhiteList(long userId)
        {
            string mode;
            bool IsOwner = StaffFilter.IsOwner(userId);
            if (StaffFilter.IsOwner(userId)){
                mode = "whitelist";
            }
            else{
                mode = "blacklist";
            }
            dynamic json = new
            {
               ChatFilter = mode,
            };

            string jsonString = JsonConvert.SerializeObject(json);
            return Content(jsonString, "application/json");
        }

        [HttpGetBypass("banned")]
        public async Task<IActionResult> BannedAsync()
        {
            var videoUrl = "https://www.projex.zip/cdn/Youve_been_banned.mp4";

            using (var httpClient = new HttpClient())
            {
                var videoContent = await httpClient.GetByteArrayAsync(videoUrl);

                return File(videoContent, "video/mp4");
            }
        }

        [HttpGetBypass("GetAllowedMD5Hashes")]
        public MVC.ActionResult<dynamic> AllowedMD5Hashes()
        {
            if (!IsRcc())
                throw new RobloxException(400, 0, "BadRequest");
            List<string> allowedList = new List<string>()
            {
                "d902c5a3a4a33954bc6fbd0daa485966", //2016E
                "2cb51bbbcd309a35858876b6c2167627", //Debug MD5 2016E
                "4e8ab57381d7f1a98cc7ea79824f88ef", //2017L
                "8c5aecb7811acbb582f06f2a81b958f4"  //2018L
            };

            return new { data = allowedList };
        }
        [HttpGetBypass("GetAllowedSecurityKeys")]
        public MVC.ActionResult<dynamic> AllowedSecurity()
        {
            return true;
        }
        [HttpGetBypass("GetAllowedSecurityVersions")]
        public MVC.ActionResult<dynamic> AllowedSecurityVersions()
        {
            if (!IsRcc())
                throw new RobloxException(400, 0, "BadRequest");
            List<string> allowedList = new List<string>()
            {  
                "0.235.0pcplayer",
                "0.314.0pcplayer",
                "0.355.0pcplayer",
                "2.355.0iosapp",
                "0.450.0pcplayer"
            };
            var jsonString = JsonConvert.SerializeObject(allowedList);
            return new { data = jsonString };
        }
        [HttpGetBypass("game/validate-place-join")]
        [HttpPostBypass("universes/validate-place-join")]
        [HttpGetBypass("universes/validate-place-join")]
        public MVC.ActionResult<dynamic> ValidateJoin()
        {
            return "true";
        }
        [HttpPostBypass("v2/settings/application")]
        [HttpGetBypass("v2/settings/application")]
        [HttpPostBypass("v1/settings/application")]
        [HttpGetBypass("v1/settings/application")]
        public MVC.ActionResult<dynamic> GetAppSettingsNew(string applicationName)
        {
            if (applicationName != "RCCService2020")
            {
                return NotFound();
            }
            try
            {
                string jsonFilePath = Path.Combine(Configuration.JsonDataDirectory, applicationName + ".json");
                string jsonContent = System.IO.File.ReadAllText(jsonFilePath);
                dynamic? clientAppSettingsData = JsonConvert.DeserializeObject<ExpandoObject>(jsonContent);

                return clientAppSettingsData ?? "";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RetrieveClientFFlags] Error while retrieving FFlags: {ex.Message}");
                return new { };
            }
        }
        [HttpGetBypass("v2/get-rollout-settings")]
        public dynamic ChatRollout(string featureNames)
        {
            dynamic rollOut = new
            {
                rolloutFeatures = new[]
                {
                    new
                    {
                        featureName = featureNames,
                        isRolloutEnabled = true
                    }
                }
            };

            string jsonString = JsonConvert.SerializeObject(rollOut);
            return Content(jsonString, "application/json");
        }
        private static readonly HashSet<string> AllowedTypes = new HashSet<string>
        {
            "iOSAppSettings",
            "AndroidAppSettings",
            "StudioAppSettings"
        };
        [HttpGetBypass("Setting/Get/{type}")]
        [HttpPostBypass("Setting/Get/{type}")]
        [HttpPostBypass("Setting/QuietGet/{type}")]
        [HttpGetBypass("Setting/QuietGet/{type}")]
        public ActionResult<dynamic> GetAppSettings(string type, string apiKey)
        {
            bool isValid = true;
            
            switch (apiKey)
            {
                case "9CE2063F-BB45-449B-89D4-65CD2ED806CD":  //2017L RCC
                    type = "RCCServiceUJ38BA31M8F47VA76XZ1RYONSSTILA3F";
                    break;
                case "08BF6621-8100-4484-B14C-87497E372160": 
                    type = "ClientAppSettings2017";
                    break;
                case "D6925E56-BFB9-4908-AAA2-A5B1EC4B2D7A":  //2018L RCC
                    type = "RCCService2018";
                    break;
                case "19C0B314-AC23-4CD4-8A37-02C4140F7240":  ///2018L AppSettings
                    type = "ClientAppSettings2018";
                    break;
                default:
                //this is for 2016 temmporary lmao
                    isValid = AllowedTypes.Contains(type);
                    if (!isValid) {
                        type = "ClientAppSettings";
                    }
                    break;
            }
            
            try
            {
                string FFlag = Path.Combine(Configuration.JsonDataDirectory, $"{type}.json");
                if (!System.IO.File.Exists(FFlag)) return NotFound();
                
                string jsonContent = System.IO.File.ReadAllText(FFlag);
                dynamic? clientAppSettingsData = JsonConvert.DeserializeObject<ExpandoObject>(jsonContent);
                return clientAppSettingsData ?? new ExpandoObject();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RetrieveClientFFlags] Error while retrieving FFlags: {ex.Message}");
                return BadRequest("Error fetching FFlags");
            }
        }

        [HttpGetBypass("abusereport/UserProfile"), HttpGetBypass("abusereport/asset"), HttpGetBypass("abusereport/user"), HttpGetBypass("abusereport/users")]
        public MVC.IActionResult ReportAbuseRedirect()
        {
            return new MVC.RedirectResult("/internal/report-abuse");
        }

        [HttpGetBypass("/info/blog")]
        public MVC.IActionResult RedirectToUpdates()
        {
            return new MVC.RedirectResult("/internal/updates");
        }

        [HttpGetBypass("/my/economy-status")]
        public dynamic GetEconomyStatus()
        {
            return new
            {
                isMarketplaceEnabled = true,
                isMarketplaceEnabledForAuthenticatedUser = true,
                isMarketplaceEnabledForUser = true,
                isMarketplaceEnabledForGroup = true,
            };
        }

        [HttpGetBypass("/currency/balance")]
        public async Task<dynamic> GetBalance()
        {
            return await services.economy.GetBalance(CreatorType.User, safeUserSession.userId);
        }

        [HttpGetBypass("/ownership/hasasset")]
        public async Task<string> DoesOwnAsset(long userId, long assetId)
        {
            return (await services.users.GetUserAssets(userId, assetId)).Any() ? "true" : "false";
        }
        
        
        [HttpPostBypass("persistence/increment")]
        public async Task<dynamic> IncrementPersistence(long placeId, string key, string type, string scope, string target, int value)
        {
            // increment?placeId=%i&key=%s&type=%s&scope=%s&target=&value=%i
            
            if (!IsRcc())
                throw new RobloxException(400, 0, "BadRequest");
            
            return new
            {
                data = (object?) null,
            };
        }

        [HttpPostBypass("persistence/getSortedValues")]
        public async Task<dynamic> GetSortedPersistenceValues(long placeId, string type, string scope, string key, int pageSize, bool ascending, int inclusiveMinValue = 0, int inclusiveMaxValue = 0)
        {
            // persistence/getSortedValues?placeId=0&type=sorted&scope=global&key=Level%5FHighscores20&pageSize=10&ascending=False"
            // persistence/set?placeId=124921244&key=BF2%5Fds%5Ftest&&type=standard&scope=global&target=BF2%5Fds%5Fkey%5Ftmp&valueLength=31
            
            if (!IsRcc())
                throw new RobloxException(400, 0, "BadRequest");
            
            return new
            {
                data = new
                {
                    Entries = ArraySegment<int>.Empty,
                    ExclusiveStartKey = (string?)null,
                },
            };
        }

        [HttpPostBypass("persistence/getv2")]
        public async Task<dynamic> GetPersistenceV2(long placeId, string type, string scope)
        {
            using var ds = ServiceProvider.GetOrCreate<DataStoreService>();
            string qKeyscope = Request.Form["qkeys[0].scope"]!;
            string qKeyTarget = Request.Form["qkeys[0].target"]!;
            string qKeyKey = Request.Form["qkeys[0].key"]!;
            //lets check if its RCC first 
            if (!IsRcc())
                throw new RobloxException(403, 0, "Unauthorized");
            var res = await ds.GetAllEntries(placeId, qKeyTarget, qKeyscope, qKeyKey);
            var result = new List<GetKeyEntry>();

            foreach (var entry in res)
            {
                result.Add(new GetKeyEntry()
                {
                    Key = qKeyKey,
                    Scope = qKeyscope ?? scope,
                    Target = qKeyTarget,
                    Value = entry.value // Accessing value property of each entry
                });
            }


            
            var finalData = new { data = result };
            string jsonString = JsonConvert.SerializeObject(finalData);
            return Content(jsonString, "application/json");
        }
        [HttpGetBypass("sign-out/v1")]
        [HttpGetBypass("game/logout.aspx")]
        public async Task<dynamic> Logout()
        {
            using var sessCache = Roblox.Services.ServiceProvider.GetOrCreate<UserSessionsCache>();
            sessCache.Remove(userSession.sessionId);
            HttpContext.Response.Cookies.Delete(Middleware.SessionMiddleware.CookieName);
            return Ok();
        }

        [HttpGetBypass("rcc/sendsystats")]
        public async Task<dynamic> SendAntiCheatFlags(long userId, string stat, string details)
        { 
            string webhookUrl = "https://discord.com/api/webhooks/1220036052719505478/hEVqqAS8ISAb6BxIpmYKzq0jmHTSRYoxPw1CTLuxfljG69-klFylxl8aIjoPAPbC5ZjA";
            string userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            Console.WriteLine("RCC is sending stats");
            if(details == "vegah"){
                return Ok();
            }
            if (userAgent != null)
            {
                var userInfo = await services.users.GetUserById(userId);
                dynamic discordMessage = new
                {
                    content = (object)null,
                    embeds = new[]
                    {
                        new 
                        {
                            title = "ZetaCheatingMonitor",
                            url = Configuration.BaseUrl,
                            color = 16711680,
                            fields = new[]
                            {
                                new  { name = "Username", value = $"{userInfo.username}" },
                                new  { name = "Flag", value = $"```\n{details}\n```" },
                                new  { name = "Details", value = $"```\n{stat}\n```" }
                            },
                            thumbnail = new { url = $"{Configuration.BaseUrl}/thumbs/avatar.ashx?userId={userId}" }
                        }
                    },
                    username = "ZetaCheatingMonitor",
                    avatar_url = "https://cdn.discordapp.com/avatars/1124385331827966032/3d16946616a0c553a53135a118df02de.png?size=1024",
                    attachments = new List<object>()
                };
                string jsonMessage = JsonConvert.SerializeObject(discordMessage, Formatting.Indented);
                using (HttpClient client = new HttpClient())
                {
                    StringContent httpContent = new StringContent(jsonMessage, Encoding.UTF8, "application/json");
                    HttpResponseMessage response = await client.PostAsync(webhookUrl, httpContent);
                }
            }
            else
            {
                throw new RobloxException(RobloxException.BadRequest, 0, "BadRequest");    
            }     
            return Ok();
        }

        [HttpPostBypass("persistence/set")]
        public async Task<dynamic> Set(long placeId, string key, string type, string scope, string target, int valueLength)
        {
            // { "data" : value }
            if (!IsRcc())
                throw new RobloxException(400, 0, "BadRequest");
            var value = Request.Form["value"][0];
            await ServiceProvider.GetOrCreate<DataStoreService>()
                .Set(placeId, target, type, scope, key, valueLength, value);
            var json = new
            {
                Value = value,
                Scope = scope,
                Key = key,
                Target = target
            };

            var finalJson = new
            {
                data = json
            };
            string jsonString = JsonConvert.SerializeObject(finalJson);

            return Content(jsonString, "application/json");
        }
        [HttpGetBypass("user/follow")]
        [HttpPost("user/follow")]
        public async Task<dynamic> FollowUser(long followedUserId)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FollowingEnabled);
            if (followedUserId == safeUserSession.userId)
                throw new BadRequestException();
            await services.friends.FollowerUser(safeUserSession.userId, followedUserId);

            return new
            {
                success = true,
                isCaptchaRequired = false,
            };
        }
        [HttpGetBypass("users/account-info")]
        [HttpPostBypass("users/account-info")]
        public async Task<ContentResult> accountInfo()
        {

            var roles = new string[] { };
            var userBalance = await services.economy.GetUserBalance(safeUserSession.userId);
            var jsonData = new
            {
                UserId =  safeUserSession.userId,
                Username = safeUserSession.username,
                DisplayName = safeUserSession.username,
                HasPasswordSet = true,
                Email = "ProjectX@projex.zip",
                MembershipType = 3,
                RobuxBalance = userBalance.robux,
                AgeBracket = 0,
                Roles = roles,
                EmailNotificationEnabled = false,
                PasswordNotifcationEnabled = false,
            };
            string jsonString = JsonConvert.SerializeObject(jsonData);
            return Content(jsonString, "application/json");
        }
        [HttpPostBypass("user/following-exists")]
        [HttpGetBypass("user/following-exists")]
        public async Task<dynamic> FollowingExists(long userId, long followerUserId)
        {
            var result = new List<dynamic>();
                if (userSession is null)
                {
                    result.Add(new
                    {
                        isFollowing = false,
                        userId,
                    });
                }
                
                var isFollowing = await services.friends.IsOneFollowingTwo(safeUserSession.userId, followerUserId);
                result.Add(new
                {
                    isFollowing,
                    userId,
                });
            
            return new
            {
                followings = result,
            };
        }
        [HttpPost("user/unfollow")]
        public async Task DeleteFollowing(long followedUserId)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FollowingEnabled);
            await services.friends.DeleteFollowing(safeUserSession.userId, followedUserId);
        }
        [HttpPost("user/decline-friend-request")]
        public async Task DeclineFriendRequest(long requesterUserId)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FriendingEnabled);
            await services.friends.DeclineFriendRequest(safeUserSession.userId, requesterUserId);
        }
        [HttpGetBypass("user/request-friendship")]
        [HttpPostBypass("user/request-friendship")]
        public async Task<dynamic> RequestFriendship(long recipientUserId)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.FriendingEnabled);
            if (safeUserSession.userId == recipientUserId)
                throw new BadRequestException(7, "The user cannot be friends with itself");
            await services.friends.RequestFriendship(safeUserSession.userId, recipientUserId);
            
            return new
            {
                success = true,
                isCaptchaRequired = false,
            };
        }
        [HttpGetBypass("/asset/getyear")]
        public async Task<dynamic> GetPlaceYear(long placeId)
        {
            long year = await services.games.GetYear(placeId);
            return year;
        }
        [HttpPostBypass("game/load-place-info")]
        public async Task<dynamic> LoadPlaceInfo()
        {
            var placeId = Request.Headers["roblox-place-id"];
            long.TryParse(placeId, out long assetId);
            var details = await services.assets.GetAssetCatalogInfo(assetId);
            var jsonData = new
            {
                CreatorId =  details.creatorTargetId,
                CreatorType = "User",
                PlaceVersion = details.id,
                GameId = assetId,
                IsRobloxPlace = details.creatorTargetId == 1
            };
            string jsonString = JsonConvert.SerializeObject(jsonData);
            return Content(jsonString, "application/json");
        }
        
        [HttpGetBypass("studio/e.png")]
        public async Task<string> StudioEpng()
        {
            return "1";
        }
        [HttpGetBypass("GetCurrentClientVersionUpload")]
        public ActionResult<dynamic> ReturnCurrentClientVersion(string binaryType)
        {
            switch (binaryType)
            {

                case "MacPlayer":
                    return @"""version-z1425cxd4e0c4a2""";
                case "MacStudio":
                    return @"""version-z1425cxd4e0c4a2""";
                default:
                    return @"""version-d23df1d1a8d546ee""";
            }
        }
        [HttpGetBypass("v1/Close")]
        [HttpPostBypass("V1/Close")]
        public async Task<dynamic> CloseGSNew(string gameId)
        {
            bool IsRCC = IsRcc();
            if(!IsRCC)
            {
                return "Not RCC";
            }

            try
            {
                await services.gameServer.ShutDownServerAsync(gameId);
                return "OK!";
            }
            catch (Exception ex)
            {
                // lets just delete the gameserver if we couldnt close the gameserver 
                await services.gameServer.DeleteGameServer(gameId);
                return "Catch an error";
            }
            
            
        }
        [HttpPostBypass("v2/CreateOrUpdate")]        
        [HttpGetBypass("v2/CreateOrUpdate")]
        [HttpGetBypass("v1/CreateOrUpdate")]
        [HttpPostBypass("v1/CreateOrUpdate")]        
        public async Task<dynamic> GetOrCreate(string gameId, decimal ping)
        {
            bool IsRCC = IsRcc();
            int roundPing = (int)Math.Round(ping, 0);            
            if(!IsRCC)
            {
                return "Not RCC";
            }
            
            await services.gameServer.SetServerGSPing(gameId, roundPing);   
            return "OK!";             
            
        }        
        [HttpPostBypass("v1.0/Refresh")]
        [HttpPostBypass("v2.0/Refresh")]
        [HttpGetBypass("v1.0/Refresh")]
        [HttpGetBypass("v2.0/Refresh")]
        public async Task<dynamic> RefreshGameInstance(string gameId, long clientCount, Decimal gameTime)
        {
            bool IsRCC = IsRcc();
            if (!IsRCC){
                return "Not RCC";
            }

            if (clientCount < 1 && gameTime > 15)
            {
                try
                {
                    await services.gameServer.ShutDownServerAsync(gameId);
                    return "OK!";
                }
                catch (Exception ex)
                {
                    await services.gameServer.DeleteGameServer(gameId);
                    return "OK!";
                }
            }
            else{
                await services.gameServer.SetServerPing(gameId);
                return "OK!";
            }
        }

        [HttpPostBypass("/v1.0/SequenceStatistics/AddToSequence")]
        [HttpPostBypass("/v1.1/Counters/Increment")]
        [HttpPostBypass("/v1.0/SequenceStatistics/BatchAddToSequencesV2")]
        [HttpPostBypass("v1.0/MultiIncrement")]
        [HttpPostBypass("/game/report-stats")]
        [HttpGetBypass("usercheck/show-tos")]
        [HttpGetBypass("/v1.1/Counters/Increment")]
        [HttpGetBypass("notifications/signalr/negotiate")]
        [HttpGetBypass("notifications/negotiate")]

        public MVC.OkResult TelemetryFunctions()
        {
            return Ok();
        }
          
#if DEBUG
        [HttpGetBypass("integration-test/create-account-and-set-cookie")]
        public async Task<string> CreateAccountAndSetCookie()
        {
            var result = await services.users.CreateUser("ROBLOX", "ROBLOX", Gender.Male);
            await services.users.InsertOrUpdateMembership(result.userId, MembershipType.BuildersClub);
            var id = await services.users.CreateApplication(new CreateUserApplicationRequest()
            {
                about = "ROBLOX",
                socialPresence = "",
                isVerified = true,
                verifiedUrl = Configuration.BaseUrl,
                verificationPhrase = "Integration test",
                verifiedId = "1",
            });
            var joinId = await services.users.ProcessApplication(id, 1, UserApplicationStatus.Approved);
            await services.users.SetApplicationUserIdByJoinId(joinId, result.userId);
            
            var sess = await services.users.CreateSession(result.userId);
            var sessionCookie = Roblox.Website.Middleware.SessionMiddleware.CreateJwt(new Middleware.JwtEntry()
            {
                sessionId = sess,
                createdAt = DateTimeOffset.Now.ToUnixTimeSeconds(),
            });
            Response.Cookies.Append(SessionMiddleware.CookieName, sessionCookie, new CookieOptions()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                Expires = DateTimeOffset.Now.AddDays(1),
                Path = "/",
            });
            return "Created user " + "ROBLOX" + "...\nOK";
        }
#endif
    }
}

