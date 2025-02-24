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
using Roblox.Website.WebsiteModels.Authentication;
using System.Text.RegularExpressions;
using InfluxDB.Client.Core.Exceptions;
using Roblox.Exceptions;
using Roblox.Website.Pages;
using System.IO.Compression;
using Roblox.Models;
using Roblox.Dto.Assets;
using Roblox.Models.AbuseReport;
using Roblox.Dto.AbuseReport;
using Roblox.Models.Games;
using System.Diagnostics.CodeAnalysis;
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
                        isInGroup = true;
                    var group = await services.groups.GetUserRoleInGroup((long)groupid, (long?)playerid ?? (long)0);
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
                return $"<Value Type=\"boolean\">{status != null && status.status == "Friends"}</Value>";
            }

            return $"<Value Type\"boolean\">{method == "isbestfriendswith"}</value>";
            throw new NotImplementedException();
        }

        [HttpGetBypass("v2/users/{userId:long}/groups/roles")]
        public async Task<RobloxCollection<dynamic>> GetUserGroupRoles(long userId)
        {
            var roles = await services.groups.GetAllRolesForUser(userId);
            var result = new List<dynamic>();
            foreach (var role in roles)
            {
                var groupDetails = await services.groups.GetGroupById(role.groupId);
                result.Add(new
                {
                    group = new
                    {
                        id = groupDetails.id,
                        name = groupDetails.name,
                        memberCount = groupDetails.memberCount,
                    },
                    role = role,
                });
            }
            if (await StaffFilter.IsStaff(userId))
            {
                result.Add(new
                {
                    group = new
                    {
                        id = 1200769,
                        name = "Project X Admin",
                        memberCount = 100,
                    },
                    role = new
                    {
                        id = 1,
                        name = "Admin",
                        rank = 100
                    }
                });
            }
            return new()
            {
                data = result,
            };
        }
        [HttpGetBypass("/auth/submit")]
        public MVC.RedirectResult SubmitAuth(string auth)
        {
            return new MVC.RedirectResult("/");
        }

        [HttpPostBypass("/v1/join-game")]
        public async Task<PlaceLaunchResponse> JoinGameMobile([FromBody] JoinGame request)
        {
            long year = await services.games.GetYear(request.placeId);
            if (year != 2020 && year != 2021)
            {
                return new PlaceLaunchResponse()
                {
                    status = (int)JoinStatus.Error,
                    message = "An error occured while starting the game."
                };
            }
            var placeLauncherRequest = new PlaceLaunchRequest
            {
                request = "RequestGame",
                placeId = request.placeId,
                userId = safeUserSession.userId,
                username = safeUserSession.username,
                cookie = ROBLOSECURITY,
                special = true
            };
            return await services.placeLauncherFactory.PlaceLauncherAsync(placeLauncherRequest);
        }
        [HttpPostBypass("/game/PlaceLauncher.ashx")]
        [HttpGetBypass("/game/PlaceLauncher.ashx")]
        public async Task<PlaceLaunchResponse> PlaceLaunch([FromQuery] PlaceLaunchRequest Placelauncher)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.GamesEnabled, FeatureFlag.GameJoinEnabled);
            long year = await services.games.GetYear(Placelauncher.placeId);

            if (!ApplicationGuardMiddleware.IsRoblox(Request) || year == 2016){
                return new PlaceLaunchResponse()
                {
                    status = (int)JoinStatus.Error,
                    message = "An error occured while starting the game."
                };
            }
            if (userSession == null)
            {
                return new PlaceLaunchResponse()
                {
                    status = (int)JoinStatus.Unauthorized,
                    message = "You are not authorized to join"
                };
            }
            Placelauncher.cookie = ROBLOSECURITY;
            Placelauncher.userId = userSession.userId;
            Placelauncher.username = userSession.username;
            return await services.placeLauncherFactory.PlaceLauncherAsync(Placelauncher);
        }

        public static long startUserId {get;set;} = 30;

        [HttpPostBypass("login/RequestAuth.ashx")]
        [HttpGetBypass("login/RequestAuth.ashx")]
        public ActionResult<dynamic?> StudioRequestAuth()
        {
            if (userSession == null)
            {
                return Unauthorized("User is not authorized.");
            }

            string? cookie = ROBLOSECURITY;
            return Ok($"{Configuration.BaseUrl}/Login/Negotiate.ashx?suggest={cookie}");
        }

        [HttpGetBypass("getserverinfo")]
        public async Task<dynamic> GetServerInfo(string ip)
        {
            return await services.games.GetInfoFromIp(ip);
        }

        [HttpGetBypass("joinserver")]
        public async Task<IActionResult> JoinServerFromJobId(string jobId, long placeId)
        {
            string clientVer;
            if (userSession == null)
            {
                throw new RobloxException(403, 1, "User is not authorized.");
            }
            long year = await services.games.GetYear(placeId);
            clientVer = services.games.clientVersionMap.TryGetValue(year, out var ver) ? ver : throw new BadRequestException();
            var placeInfo = await services.assets.GetAssetCatalogInfo(placeId);
            if (placeInfo.assetType != Models.Assets.Type.Place) throw new BadRequestException();
            var modInfo = (await services.assets.MultiGetAssetDeveloperDetails(new[] {placeId})).First();
            if (modInfo.moderationStatus != ModerationStatus.ReviewApproved) throw new BadRequestException();
            var bootstrapperArgs = $":1+launchmode:play+clientversion:{clientVer}+gameinfo:{ROBLOSECURITY}+placelauncherurl:{Configuration.BaseUrl}/Game/PlaceLauncher.ashx?request=RequestGameJob&placeId={placeId}&gameId={jobId}&isPartyLeader=false&gender=&isTeleport=true+k:l+client";
            return Redirect($"pekora-player{bootstrapperArgs}");
        }

        [HttpGetBypass("getrichpresence")]
        public async Task<dynamic> GetRichPresenceInfo(long userId, long placeId, string jobId)
        {
            string username = "";
            int playerCount = 0;
            bool IsFurry = false;
            long fluffyHat = 18306;
            try
            {
                if (userId != 0)
                {
                    var currentPlayerCount = await services.gameServer.GetGameServerPlayers(jobId);
                    playerCount = currentPlayerCount.Count();
                }
            }
            catch (Exception)
            {
                playerCount = 0;
            }
            if (userSession != null)
            {
                username = userSession.username;
            }
            // check if the user owns fluffy ha
            var owned = await services.users.GetUserAssets(userId, fluffyHat);
            if (owned.Any())
                IsFurry = true;
            long maxplayers = await services.games.GetMaxPlayerCount(placeId);
            var placeInfo = await services.assets.GetAssetCatalogInfo(placeId);
            long year = await services.games.GetYear(placeId);

            return new
            {
                Creator = placeInfo.creatorName,
                Name = placeInfo.name,
                Username = username ?? "",
                Year = year,
                IsFurry,
                MaxPlayers = maxplayers,
                PartyId = Guid.NewGuid().ToString(),
                CurrentPlayers = playerCount,
            };

        }
        [HttpGetBypass("My/Places.aspx")]
        public ActionResult<dynamic?> MyPlaces()
        {
            return Ok();
        }

        [HttpGetBypass("game/GetCurrentUser.ashx")]
        public IActionResult GetUserId()
        {
            if (userSession == null)
                return Ok("Bad Request");
            return Content(userSession.userId.ToString(), "text/plain");
        }
        [HttpGetBypass("/mobileapi/check-app-version")]
        [HttpPostBypass("/mobileapi/check-app-version")]
        public ActionResult<dynamic> CheckAppVersion()
        {
            return new
            {
                data = new
                {
                    UpgradeAction = "None"
                }
            };
        }

        [HttpGetBypass("download2")]
        public async Task<dynamic> DownloadPage()
        {
            //do this for anti reporting shit
            if(userSession == null)
                return Redirect("/auth/homepage");

            return Content(await System.IO.File.ReadAllTextAsync("download.html"), "text/html");
        }

        [HttpGetBypass("set-year")]
        public async Task TaskAsync (long universeId, int year)
        {
            var place = await services.games.GetRootPlaceId(universeId);
            await services.assets.ValidatePermissions(place, safeUserSession.userId);
            await services.games.SetYear(place, year);
        }

        [HttpGetBypass("login/negotiate.ashx"), HttpGetBypass("login/negotiateasync.ashx"), HttpPostBypass("login/negotiate.ashx")]
        public void Negotiate([Required, FromQuery] string suggest)
        {
            HttpContext.Response.Cookies.Append(Middleware.SessionMiddleware.CookieName, suggest, new CookieOptions
            {
                Domain = $".{Configuration.ShortBaseUrl}",
                Secure = false,
                Expires = DateTimeOffset.Now.Add(TimeSpan.FromDays(364)),
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
            });
        }

        [HttpPostBypass("game/join.ashx")]
        [HttpGetBypass("game/join.ashx")]
        public async Task<dynamic> JoinGame(string jobId, bool GenerateTeleportJoin = false)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.GamesEnabled, FeatureFlag.GameJoinEnabled);

            string username = safeUserSession.username;
            long userId = safeUserSession.userId;

            GameServerDb jobInfo = await services.gameServer.GetGameServer(jobId);
            if (jobInfo == null)
                throw new BadRequestException(1, "Gameserver does not exist");
            
            long placeId = jobInfo.asset_id;
            PlaceEntry placeInfo = (await services.games.MultiGetPlaceDetails(new[] { placeId })).First();
            
            string characterAppearanceUrl = $"{Configuration.BaseUrl.Replace("https", "http")}/v1.1/avatar-fetch?userId={userId}&placeId={placeId}";
            
            var jobPlayers = await services.gameServer.GetGameServerPlayers(jobId);
            if (jobPlayers.Count() >= placeInfo.maxPlayerCount)
            {
                return new
                {
                    error = "The requested game is full",
                    status = 5
                };
            }

            var userInfo = await services.users.GetUserById(userId);
            var accountAgeDays = DateTime.UtcNow.Subtract(userInfo.created).Days;

            string membership = await services.users.GetUserMemberShipAsString(userId);
            if (placeInfo.year != 2020 && placeInfo.year != 2021 && membership == "Premium")
            {
                membership = "OutrageousBuildersClub";
            }
            string clientTicket = services.sign.GenerateClientTicket(placeInfo.year, userId, username, characterAppearanceUrl, membership, jobId, accountAgeDays, placeId);

            dynamic? joinScript;
            try
            {
                joinScript = await services.games.GetJoinScript(placeInfo, userInfo, jobInfo, characterAppearanceUrl, clientTicket, membership, accountAgeDays, GenerateTeleportJoin, ROBLOSECURITY);
            }
            catch (Exception)
            {
                throw new BadRequestException(1, "Couldn't find gameserver");
            }

            return services.games.SignJoinScript(placeInfo.year, joinScript);
        }
        [HttpGetBypass("GenerateVersion")]
        public string GenerateVersion()
        {
            return $"version-{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16)}";
        }
        [HttpGetBypass("GenerateAuthString")]
        public string GenerateAuthString()
        {
            return "PJX-" + Guid.NewGuid().ToString().Replace("-", "") + Guid.NewGuid().ToString().Replace("-", "");
        }
        [HttpGetBypass("Asset/CharacterFetch.ashx")]
        public async Task<string> CharacterFetchASHX(long userId)
        {
            var assets = await services.avatar.GetWornAssets(userId);
            return $"{Configuration.BaseUrl}/Asset/BodyColors.ashx?userId={userId};{string.Join(";", assets.Select(c => Configuration.BaseUrl + "/Asset/?id=" + c))}";
        }
        // prob the most worse code ive ever written
        [HttpPost("AbuseReport/InGameChatHandler.ashx")]
        [Consumes("application/xml")]
        public async Task<MVC.OkResult> AbuseReport([FromBody] InGameAbuseReportEntry report)
        {
            if (!isRCC)
                throw new Roblox.Exceptions.UnauthorizedException(0, "Unauthorized");
            string gameMessages = "";
            string reportMessage = @$"This report was sent by the in-game report system.
            Place ID: {report.placeId}
            Job ID: {report.gameJobId}
            {{0}}
            ";

            // Example: AbuserID:0;Inappropriate Content;User Report:
            // very hacky

            long abuserId = long.Parse(report.comment.Split(":")[1].Trim().Split(";")[0]);
            string[] splittedComment = report.comment.Split(";");
            // If the abuserId is 0 it is a place report
            if (abuserId == 0)
            {
                reportMessage = string.Format(reportMessage, splittedComment[2]);
                await services.abuseReport.InsertReport(report.userId, AbuseReportReason.BadGame, reportMessage);
                return Ok();
            }

            foreach (InGameMessage message in report.messages.message)
            {
                string user = message.userId == abuserId
                    ? $"(Abuser) UID: {message.userId}"
                    : $"UID: {message.userId}";
                gameMessages += $"{user}: {message.text}\n";
            }
            // EW!
            reportMessage = string.Format(reportMessage, $"Abuser ID: {abuserId}\nReason: {splittedComment[2]}");
            string reportId = await services.abuseReport.InsertReport(report.userId, AbuseReportReason.BadChatMessagesInGame, reportMessage);
            await services.abuseReport.InsertGameMessages(reportId, report.gameJobId, gameMessages);
            return Ok();
        }

        [HttpGetBypass("my/settings/json")]
        public async Task<dynamic> SettingsJsonA()
        {
            var userInfo = await services.users.GetUserById(userSession.userId);
            string membership;
            bool isAdmin = await StaffFilter.IsStaff(userSession.userId);
            var membership2 = await services.users.GetUserMembership(userSession.userId);
            if (membership2 == null)
            {
                membership = "None";
            }
            else
            {
                membership = (int)membership2!.membershipType == 4 ? "Premium" : (int)membership2!.membershipType == 3 ? "OutrageousBuildersClub" : (int)membership2.membershipType == 2 ? "TurboBuildersClub" : (int)membership2.membershipType == 1 ? "BuildersClub" : "None";
            }
            return new
            {
                ChangeUsernameEnabled = true,
                IsAdmin = isAdmin,
                UserId = userSession.userId,
                Name = userSession.username,
                DisplayName = userSession.username,
                IsEmailOnFile = true,
                IsEmailVerified = true,
                IsPhoneFeatureEnabled = true,
                RobuxRemainingForUsernameChange = 0,
                PreviousUserNames = "",
                UseSuperSafePrivacyMode = false,
                IsSuperSafeModeEnabledForPrivacySetting = false,
                UseSuperSafeChat = false,
                IsAppChatSettingEnabled = true,
                IsGameChatSettingEnabled = true,
                IsAccountPrivacySettingsV2Enabled = true,
                IsSetPasswordNotificationEnabled = false,
                ChangePasswordRequiresTwoStepVerification = false,
                ChangeEmailRequiresTwoStepVerification = false,
                UserEmail = "pekora@pekora.zip",
                UserEmailMasked = true,
                UserEmailVerified = true,
                CanHideInventory = true,
                CanTrade = false,
                MissingParentEmail = false,
                IsUpdateEmailSectionShown = true,
                IsUnder13UpdateEmailMessageSectionShown = false,
                IsUserConnectedToFacebook = false,
                IsTwoStepToggleEnabled = false,
                AgeBracket = 0,
                UserAbove13 = true,
                ClientIpAddress = GetRequesterIpRaw(HttpContext),
                AccountAgeInDays = DateTime.UtcNow.Subtract(userInfo.created).Days,
                IsOBC = false,
                IsTBC = false,
                IsAnyBC = false,
                IsPremium = false,
                IsBcRenewalMembership = false,
                BcExpireDate = "/Date(-0)/",
                BcRenewalPeriod = (string?)null,
                BcLevel = (int?)null,
                HasCurrencyOperationError = false,
                CurrencyOperationErrorMessage = (string?)null,
                BlockedUsersModel = new
                {
                    BlockedUserIds = new List<int>() { },
                    BlockedUsers = new List<string>() { },
                    MaxBlockedUsers = 50,
                    Total = 1,
                    Page = 1
                },
                Tab = (string?)null,
                ChangePassword = false,
                IsAccountPinEnabled = true,
                IsAccountRestrictionsFeatureEnabled = true,
                IsAccountRestrictionsSettingEnabled = false,
                IsAccountSettingsSocialNetworksV2Enabled = false,
                IsUiBootstrapModalV2Enabled = true,
                IsI18nBirthdayPickerInAccountSettingsEnabled = true,
                InApp = false,
                MyAccountSecurityModel = new
                {
                    IsEmailSet = true,
                    IsEmailVerified = true,
                    IsTwoStepEnabled = false,
                    ShowSignOutFromAllSessions = true,
                    TwoStepVerificationViewModel = new
                    {
                        UserId = safeUserSession.userId,
                        IsEnabled = false,
                        CodeLength = 6,
                        ValidCodeCharacters = (int?)null
                    }
                },
                ApiProxyDomain = Configuration.BaseUrl,
                AccountSettingsApiDomain = Configuration.BaseUrl,
                AuthDomain = Configuration.BaseUrl,
                IsDisconnectFbSocialSignOnEnabled = true,
                IsDisconnectXboxEnabled = true,
                NotificationSettingsDomain = Configuration.BaseUrl,
                AllowedNotificationSourceTypes = new List<string>
                {
                    "Test",
                    "FriendRequestReceived",
                    "FriendRequestAccepted",
                    "PartyInviteReceived",
                    "PartyMemberJoined",
                    "ChatNewMessage",
                    "PrivateMessageReceived",
                    "UserAddedToPrivateServerWhiteList",
                    "ConversationUniverseChanged",
                    "TeamCreateInvite",
                    "GameUpdate",
                    "DeveloperMetricsAvailable"
                },
                AllowedReceiverDestinationTypes = new List<string>
                {
                    "DesktopPush",
                    "NotificationStream"
                },
                BlacklistedNotificationSourceTypesForMobilePush = new List<string> { },
                MinimumChromeVersionForPushNotifications = 50,
                PushNotificationsEnabledOnFirefox = true,
                LocaleApiDomain = Configuration.BaseUrl,
                HasValidPasswordSet = true,
                IsUpdateEmailApiEndpointEnabled = true,
                FastTrackMember = (string?)null,
                IsFastTrackAccessible = false,
                HasFreeNameChange = false,
                IsAgeDownEnabled = false,
                IsSendVerifyEmailApiEndpointEnabled = true,
                IsPromotionChannelsEndpointEnabled = true,
                ReceiveNewsletter = false,
                SocialNetworksVisibilityPrivacy = 6,
                SocialNetworksVisibilityPrivacyValue = "AllUsers",
                Facebook = (string?)null,
                Twitter = (string?)null,
                YouTube = (string?)null,
                Twitch = (string?)null
            };
        }
        [HttpGetBypass("v2/stream-notifications/unread-count")]
        public dynamic PushNotif()
        {
            return new
            {
                unreadNotifications = 69,
                statusMessage = string.Empty
            };
        }

        [HttpGetBypass("sponsoredpage/list-json")]
        [HttpGetBypass("mobile-ads/v1/get-ad-details")]
        [HttpGetBypass("incoming-items/counts")]
        public dynamic IncomingItems()
        {
            return new
            {
                success = true
            };
        }

        [HttpGetBypass("v1.1/game-start-info")]
        public dynamic GameStartInfo(long universeId)
        {
            var uni = services.games.MultiGetUniverseInfo(new[] { universeId }).Result.First();
            return new
            {
                gameAvatarType = uni.universeAvatarType,
                allowCustomAnimations = "True",
                universeAvatarCollisionType = "OuterBox",
                universeAvatarBodyType = "Standard",
                jointPositioningType = "ArtistIntent",
                message = "",
                universeAvatarMinScales = new
                {
                    height = 0.9,
                    width = 0.7,
                    head = 0.95,
                    depth = 0.0,
                    proportion = 0.0,
                    bodyType = 0.0
                },
                universeAvatarMaxScales = new
                {
                    height = 1.05,
                    width = 1.0,
                    head = 1.0,
                    depth = 0.0,
                    proportion = 1.0,
                    bodyType = 1.0
                },
                universeAvatarAssetOverrides = new List<object>(),
                moderationStatus = ""
            };
        }
        [HttpGetBypass("hor")]
        public async Task<IActionResult> HallOfRetards()
        {
            return Content(await System.IO.File.ReadAllTextAsync("hor.txt"), "text/plan");
        }
        [HttpGetBypass("/v1/avatar-fetch")]
        [HttpGetBypass("/v1.1/avatar-fetch")]
        public async Task<MVC.IActionResult> CharacterFetch(long userId)
        {
            List<long> accessoryVersionIds = new List<long>();
            List<long> equippedGearVersionIds = new List<long>();
            var wornAssets = await services.avatar.GetWornAssets(userId);
            var avatar = await services.avatar.GetAvatar(userId);
            // If we dont have an avatar then its most likely a game loading a avatar

            var assetInfo = await services.assets.MultiGetInfoById(wornAssets);

            dynamic bodyColors = new
            {
                headColorId = avatar.headColorId,
                leftArmColorId = avatar.leftArmColorId,
                leftLegColorId = avatar.leftLegColorId,
                rightArmColorId = avatar.rightArmColorId,
                rightLegColorId = avatar.rightLegColorId,
                torsoColorId = avatar.torsoColorId,

                HeadColor = avatar.headColorId,
                LeftArmColor = avatar.leftArmColorId,
                LeftLegColor = avatar.leftLegColorId,
                RightArmColor = avatar.rightArmColorId,
                RightLegColor = avatar.rightLegColorId,
                TorsoColor = avatar.torsoColorId
            };
            dynamic scales = new { height = 1, Height = 1, width = 1, Width = 1, head = 1, Head = 1, Depth = 1, depth = 1, proportion = 0, Proportion = 0, bodyType = 0, BodyType = 0};
            
            equippedGearVersionIds.AddRange(assetInfo.Where(d => d.assetType == Type.Gear).Select(d => d.id));
            accessoryVersionIds.AddRange(assetInfo.Where(d => d.assetType != Type.Gear || d.assetType != Type.EmoteAnimation).Select(d => d.id));
            
            if (UserAgent != "Roblox/Win2020")
            {
                equippedGearVersionIds = new List<long>();
            }
            int positionCounter = 1;
            var result = new 
            {
                resolvedAvatarType = avatar.avatarType.ToString(),
                accessoryVersionIds,
                equippedGearVersionIds,
                assetAndAssetTypeIds = assetInfo.Where(c => c.assetType != Type.EmoteAnimation).Select(c => new
                {
                    assetId = c.id,
                    assetTypeId = (int)c.assetType,
                }),
                backpackGearVersionIds = equippedGearVersionIds,
                animationAssetIds = new {},
                playerAvatarType = avatar.avatarType.ToString(),
                scales,
                bodyColorsUrl = $"{Configuration.BaseUrl}/Asset/BodyColors.ashx?userId={userId}",
                bodyColors,
                emotes = assetInfo.Where(c => c.assetType == Type.EmoteAnimation).Select(c => new
                {
                    assetId = c.id,
                    assetName = c.name,
                    position = positionCounter++, 
                }),
            };

            string jsonString = JsonConvert.SerializeObject(result);
            return Content(jsonString, "application/json");
        }

        private void CheckServerAuth(string auth)
        {
            if (auth != Configuration.GameServerAuthorization)
            {
                throw new BadRequestException();
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
        public async Task<dynamic> RegisterGamePresence(long visitorId, long placeId, string gameId, string locationType)
        {
            if (!isRCC)
                throw new UnauthorizedAccessException();

            await services.gameServer.OnPlayerJoin(visitorId, placeId, gameId);
            return Ok();
        }

        [HttpPostBypass("presence/register-absence")]
        public async Task RegisterGamePresenceAbsence(long visitorId)
        {
            if (!isRCC)
                throw new UnauthorizedAccessException();
            string jobId = await services.gameServer.GetJobIdByUserId(visitorId);
            if(jobId == null)
                return;
            long placeId = GameServerService.GetUserPlaceId(visitorId);

            await services.gameServer.OnPlayerLeave(visitorId, placeId, jobId);
        }
        [HttpGetBypass("/device/initialize")]
        [HttpPostBypass("/device/initialize")]
        public ActionResult<dynamic> InitDevice()
        {
            return new
            {
                browserTrackerId = 1234567890,
                appDeviceIdentifier = (string?)null,
            };
        }
        [HttpGetBypass("/Game/ClientPresence.ashx")]
        public void ClientPresenceAshx(string action, long placeId, long userId, bool IsTeleport)
        {
            return;
            // if (!ApplicationGuardMiddleware.IsRcc(Request))
            // {
            //     return;
            // }
            // if(action == "disconnect")
            // {
            //     string JobId = await services.gameServer.GetJobIdByUserId(userId);
            //     if(JobId == null)
            //     {
            //         return;
            //     }
            //     await services.gameServer.OnPlayerLeave(userId, placeId, JobId);
            // }
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
        [HttpPostBypass("game/validate-machine")]
        public dynamic ValidateMachineAsync()
        {
            return new
            {
                success = true,
                message = "",
            };
        }
        [HttpGetBypass("/v1/user/currency")]
        [HttpGetBypass("/my/balance")]
        public async Task<dynamic> MyBalance()
        {
            return new
            {
                robux = await services.economy.GetUserRobux(userSession?.userId ?? (long.TryParse(HttpContext.Request.Cookies["USERID"], out var userId) ? userId : 1)),
            };
        }
        [HttpGetBypass("Users/ListStaff.ashx")]
        public async Task<dynamic> GetStaffList()
        {
            if (!isRCC) return Redirect("/404");
            return (await StaffFilter.GetStaff()).Where(c => c != 12);
        }

        [HttpGetBypass("GenerateOtpSecret")]
        public async Task<dynamic> GenerateOtpSecret()
        {
            var totpInfo = await services.users.GetOrSetTotp(safeUserSession.userId);
            return totpInfo.secret;
        }

        [HttpGetBypass("GenereateOtpQrCode")]
        public IActionResult GenerateOtpQrCode(string secret)
        {
            return File(services.users.GetOtpQrCode(safeUserSession.userId, secret), "image/png");
        }

//         [HttpGetBypass("Users/GetBanStatus.ashx")]
//         public async Task<IEnumerable<dynamic>> MultiGetBanStatus(string userIds)
//         {

//             var ids = userIds.Split(",").Select(long.Parse).Distinct();
//             var result = new List<dynamic>();
// #if DEBUG
//             return ids.Select(c => new
//             {
//                 userId = c,
//                 isBanned = false,
//             });
// #else
//             var multiGetResult = await services.users.MultiGetAccountStatus(ids);
//             foreach (var user in multiGetResult)
//             {
//                 result.Add(new
//                 {
//                     userId = user.userId,
//                     isBanned = user.accountStatus != AccountStatus.Ok,
//                 });
//             }

//             return result;
// #endif
//         }

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
        public dynamic PlaceVisit()
        {
            return Ok();
        }

        [HttpGetBypass("rcc/killserver")]
        public async Task<dynamic> ShutdownSpecificServerForPlace(long placeId, string jobId)
        {
            if (!await services.assets.CanUserModifyItem(placeId, safeUserSession.userId))
                throw new Roblox.Exceptions.UnauthorizedException(0, "Unauthorized");
            
            await services.gameServer.ShutDownServerAsync(jobId);
            return "OK!";
        }

        [HttpGetBypass("rcc/killallservers")]
        public async Task<dynamic> ShutdownServersForPlace(long placeId)
        {
            if (!await services.assets.CanUserModifyItem(placeId, safeUserSession.userId))
                throw new Roblox.Exceptions.UnauthorizedException(0, "Unauthorized");

            var serverjobs = await services.gameServer.GetGameServersForPlace(placeId);
            foreach (var jobs in serverjobs)
            {
                await services.gameServer.ShutDownServerAsync(jobs.id.ToString());
            }
            return "OK!";
        }

        [HttpGetBypass("rcc/kickplayer")]
        public async Task<dynamic> KickPlayerAsync(long userId)
        {
            if (!StaffFilter.IsOwner(safeUserSession.userId))
                return "Unauthorized";

            if (safeUserSession.userId == userId)
                return "You can't kick yourself!";

            await services.gameServer.KickPlayer(userId);

            return $"Kicked player {userId}";
        }

        [HttpGetBypass("/Game/ChatFilter.ashx")]
        public string RCC_GetChatFilter()
        {
            return "True";
        }

        [HttpPostBypass("moderation/v2/filtertext/")]
        [HttpPostBypass("moderation/filtertext/")]
        public dynamic GetModerationText()
        {
            var text = services.filter.FilterText(HttpContext.Request.Form["text"].ToString());
            return new
            {
                success = true,
                data = new
                {
                    AgeUnder13 = text,
                    Age13OrOver = text,
                    white = text,
                    black = text
                }
            };
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
            return new
            {
                ChatFilter = StaffFilter.IsOwner(userId) ? "whitelist" : "blacklist",
            };
        }

        [HttpGetBypass("GetAllowedMD5Hashes")]
        public MVC.ActionResult<dynamic> AllowedMD5Hashes()
        {
            if (!isRCC)
                throw new RobloxException(400, 0, "BadRequest");
            List<string> allowedList = new List<string>()
            {
                "a9912debcb6347c402e4139f452d4fd2", //2015M Prod
                "d902c5a3a4a33954bc6fbd0daa485966", //2016E Prod
                "abc9d2132ef2c21101804d8e25e0413f", //Repatched 2017Client
                "fc5f43ec839bbbffcb26c48846b3c865", //2017L RAGELoader Debug
                "0a5d9189b9f7a764ccf8b5655f442971", //2017L Prod
                "bba43f967698feff49038f51b391b48e", //2018L Prod
                "4022369076d608d1a99b7b3d250e4de5", //2018L RAGELoader Debug
                "9d7975454cee0e948e35cdc1fb55f92a", //2019E Prod
                "ff693c76d9c15e7e97eb09e133942412", //2020L Prod
                "7da7086e7f3a739873fa5970ef586e98", //2021M Prod
                "1fd6e7becff68acc140b2db17e24c86e", //2021M June 6
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
            if (!isRCC)
                throw new RobloxException(400, 0, "BadRequest");
            List<string> allowedList = new List<string>()
            {
                "0.206.0pcplayer",
                "0.235.0pcplayer",
                "0.314.0pcplayer",
                "0.376.0pcplayer",
                "0.355.0pcplayer",
                "2.355.0iosapp",
                "0.450.0pcplayer",
                "0.463.0pcplayer"
            };
            var jsonString = JsonConvert.SerializeObject(allowedList);
            return new { data = jsonString };
        }

        private static int pendingAssetUploads { get; set; } = 0;
        private static readonly Mutex pendingAssetUploadsMux = new();

        [HttpPostBypass("ide/publish/UploadFromCloudEdit")]
        [HttpPostBypass("Data/Upload.ashx")]
        public async Task<dynamic> UploadPlaceFromStudio(long? assetId = null)
        {
            // if assetId is 0 try getting it from the headers
            long placeId = assetId ?? 0;
            long userId = 0;
            if (placeId == 0)
            {
                long.TryParse(Request.Headers["roblox-place-id"].ToString(), out placeId);
            }

            var info = await services.assets.GetAssetCatalogInfo(placeId);
            bool canUpload = false;
            // check if the user can upload if they cant then check if rcc can
            if (userSession != null)
            {
                userId = userSession.userId;
                canUpload = await services.assets.CanUserModifyItem(placeId, userSession.userId);
            }

            if (!canUpload)
            {
                userId = info.creatorTargetId;
                canUpload = isRCC;
            }

            if (info.assetType != Models.Assets.Type.Place)
                canUpload = false;

            if (!canUpload)
                throw new RobloxException(403, 0, "Unauthorized");

            lock (pendingAssetUploadsMux)
            {
                if (pendingAssetUploads >= 2)
                    throw new RobloxException(429, 0, "TooManyRequests");
                pendingAssetUploads++;
            }
            try
            {
                MemoryStream placeStream = await GetRequestBodyAsMemoryStream();
                if (!await services.assets.RobloxFileValidation(placeStream))
                    throw new RobloxException(400, 0, "The asset file doesn't look correct. Please try again.");
                placeStream.Position = 0;
                // Create asset version in background
                _ = await services.assets.CreateAssetVersion(placeId, userId, placeStream);
                services.assets.RenderAsset(placeId, info.assetType);
            }
            finally
            {
                lock (pendingAssetUploadsMux)
                {
                    pendingAssetUploads--;
                }

            }
            return new
            {
                success = true,
            };
        }

        [HttpPostBypass("universes/{universeId:long}/enablecloudedit")]
        public async Task<OkObjectResult> EnableCloudEdit(long universeId)
        {
            if (!await services.games.CanManageUniverse(safeUserSession.userId, universeId))
                throw new RobloxException(403, 0, "Unauthorized");
            await services.games.SetCloudedit(true, universeId);
            return Ok(new { });
        }

        [HttpGetBypass("universes/{universeId:long}/cloudeditenabled")]
        public async Task<dynamic> IsCloudEditEnabled(long universeId)
        {
            return new
            {
                enabled = await services.games.IsCloudeditEnabled(universeId)
            };
        }


        [HttpGetBypass("v1/user/{userId:long}/is-admin-developer-console-enabled")]
        public async Task<dynamic> NewCanManage(long userId)
        {
            long placeId = long.Parse(Request.Headers["roblox-place-id"].ToString());
            bool canManagePlace = await services.assets.CanUserModifyItem(placeId, userId);
            bool isOwner =  StaffFilter.IsOwner(userId);
            return new
            {
                isAdminDeveloperConsoleEnabled = (canManagePlace || isOwner)
            };
        }


        [HttpGetBypass("game/validate-place-join")]
        [HttpPostBypass("universes/validate-place-join")]
        [HttpGetBypass("universes/validate-place-join")]
        public MVC.ActionResult<dynamic> ValidateJoin()
        {
            return "true";
        }

        [HttpGetBypass("v2/get-rollout-settings")]
        public dynamic ChatRollout(string featureNames)
        {
            return new
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
        public async Task<bool> DoesOwnAsset(long userId, long assetId)
        {
            var owned = await services.users.GetUserAssets(userId, assetId);
            if (owned.Any())
                return true;
            return false;
        }

        [HttpPostBypass("v1/logout")]
        [HttpGetBypass("sign-out/v1")]
        [HttpGetBypass("game/logout.aspx")]
        public dynamic Logout()
        {
            using var sessCache = Roblox.Services.ServiceProvider.GetOrCreate<UserSessionsCache>();
            sessCache.Remove(userSession.sessionId);
            HttpContext.Response.Cookies.Delete(Middleware.SessionMiddleware.CookieName);
            return Ok();
        }

        [HttpGetBypass("users/get-by-username")]
        public async Task<dynamic> GetByUsername(string username)
        {
            var userInfo = await services.users.GetUserByName(username);
            var onlineStatus = (await services.users.MultiGetPresence(new[] {userInfo.userId})).First();
            var result = (await services.thumbnails.GetUserHeadshots(new[] { userInfo.userId })).ToList();
            return new
            {
                Id = userInfo.userId,
                Username = username,
                AvatarUri = Configuration.BaseUrl + result?.FirstOrDefault()?.imageUrl ?? "/img/placeholder.png",
                AvatarFinal = true,
                IsOnline = onlineStatus.userPresenceType == PresenceType.Online,
            };
        }
        [HttpGetBypass("users/account-info")]
        [HttpPostBypass("users/account-info")]
        public async Task<dynamic> AccountInfo()
        {
            var userBalance = await services.economy.GetUserBalance(safeUserSession.userId);
            return new
            {
                UserId = safeUserSession.userId,
                Username = safeUserSession.username,
                DisplayName = safeUserSession.username,
                HasPasswordSet = true,
                Email = "pekora@pekora.zip",
                MembershipType = 3,
                RobuxBalance = userBalance.robux,
                AgeBracket = 0,
                Roles = new string[] { },
                EmailNotificationEnabled = false,
                PasswordNotifcationEnabled = false,
            };
        }

        [HttpGetBypass("/asset/getyear")]
        public async Task<dynamic> GetPlaceYear(long placeId)
        {
            return await services.games.GetYear(placeId);
        }
        [HttpPostBypass("game/load-place-info")]
        public async Task<dynamic> LoadPlaceInfo()
        {
            var placeId = Request.Headers["roblox-place-id"];
            long.TryParse(placeId, out long assetId);
            var details = await services.assets.GetAssetCatalogInfo(assetId);
            return new
            {
                CreatorId =  details.creatorTargetId,
                CreatorType = "User",
                PlaceVersion = details.id,
                GameId = assetId,
                IsRobloxPlace = details.creatorTargetId == 1
            };
        }


        [HttpGetBypass("studio/e.png")]
        public string StudioEpng()
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

            if(!isRCC)
                throw new Roblox.Exceptions.UnauthorizedException(0, "Unauthorized");

            try
            {
                await services.gameServer.ShutDownServerAsync(gameId);
                return "OK!";
            }
            catch (Exception)
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

            if(!isRCC)
                throw new Roblox.Exceptions.UnauthorizedException(0, "Unauthorized");

            if (GameServerService.unreadyGameServers.ContainsKey(gameId)) {
                GameServerService.unreadyGameServers.Remove(gameId);
            }

            int roundPing = (int)Math.Round(ping, 0);
            await services.gameServer.SetServerGSPing(gameId, roundPing);
            return "OK!";

        }

        [HttpPostBypass("v1.0/Refresh")]
        [HttpPostBypass("v2.0/Refresh")]
        [HttpGetBypass("v1.0/Refresh")]
        [HttpGetBypass("v2.0/Refresh")]
        public async Task RefreshGameInstance(string gameId, long clientCount, Decimal gameTime)
        {
            if (!isRCC)
                throw new Roblox.Exceptions.UnauthorizedException(0, "Unauthorized");

            if (clientCount == 0 && gameTime > 50)
            {
                try
                {
                    await services.gameServer.ShutDownServerAsync(gameId);
                }
                catch (Exception)
                {
                    await services.gameServer.DeleteGameServer(gameId);
                }
                return;
            }
            await services.gameServer.SetServerPing(gameId);

        }
        // just a test move to webcontroller later on
        [HttpPostBypass("apisite/develop/v1/assets/upload-gameicon")]
        public async Task<dynamic> UploadGameIcon(long placeId, [Required, FromForm] IFormFile file)
        {
            await services.assets.ValidatePermissions(placeId, safeUserSession.userId);
            var details = await services.assets.GetAssetCatalogInfo(placeId);
            if (details.assetType != Models.Assets.Type.Place) {
                throw new BadRequestException(1, "Cannot upload a game icon for a non place");
            }

            await services.assets.CreateGameIcon(placeId, file.OpenReadStream());
            return Ok();
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
        [HttpPostBypass("v1.1/Counters/BatchIncrement")]
        [HttpGetBypass("v1.1/Counters/BatchIncrement")]
        public MVC.OkResult TelemetryFunctions()
        {
            return Ok();
        }

    }
}

