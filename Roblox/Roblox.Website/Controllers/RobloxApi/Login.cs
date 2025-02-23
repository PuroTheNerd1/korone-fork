using MVC = Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Dynamic;
using Microsoft.AspNetCore.Mvc;
using Roblox.Services.Exceptions;
using Roblox.Website.WebsiteModels.Authentication;
using System.Text;
using System.Web;
using Roblox.Models.Users;
using Roblox.Dto.Users;
using Roblox.Services.App.FeatureFlags;
using Roblox.Exceptions;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class RobloxLogin: ControllerBase
    {
        [HttpPostBypass("v1/login")]
        public async Task<dynamic> LoginV1([FromBody] LoginRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.LoginEnabled);
            long userId;
            string username = request.cvalue;
            string password = request.password;
            string totpCode = "";
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new RobloxException(400, 1, "Username or password is missing.");

            // Format: {username}|{2facode}
            string[] splittedUsername = username.Split('|');

            username = splittedUsername[0];
            totpCode = splittedUsername.Length == 2 ? splittedUsername[1] : "";

            try
            {
                userId = await services.users.GetUserIdFromUsername(username);
            }
            catch (RecordNotFoundException)
            {
                throw new RobloxException(403, 1, "Incorrect username or password. Please try again");
            }

            if (await Login(request.username, request.password, userId, totpCode))
                await CreateSessionAndSetCookie(userId);

            var info = await services.users.GetUserById(userId);

            return new
            {
                user = new
                {
                    id = userId,
                    name = username,
                    displayName = username
                },
                isBanned = info.IsDeleted()
            };

        }

        [HttpPostBypass("v2/login")]
        public async Task<dynamic> LoginV2()
        {
            FeatureFlags.FeatureCheck(FeatureFlag.LoginEnabled);
            string requestBody;
            string userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            string username = "";
            string password = "";
            string totpCode = "";
            long userId;

            requestBody = await GetRequestBody();

            if (string.IsNullOrEmpty(requestBody))
                throw new BadRequestException(1, "Empty request body.");

            if (userAgent == "RobloxStudio/WinInet")
            {
                var keyValuePairs = requestBody.Split('&');
                foreach (var pair in keyValuePairs)
                {
                    var keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var key = HttpUtility.UrlDecode(keyValue[0]);
                        var value = HttpUtility.UrlDecode(keyValue[1]);
                        if (key == "username") username = value;
                        if (key == "password") password = value;
                    }
                }
            }
            else
            {
                var loginRequest = JsonConvert.DeserializeObject<LoginRequest>(requestBody);
                username = loginRequest?.username ?? loginRequest?.cvalue;
                password = loginRequest?.password;
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new UnauthorizedException(1, "Incorrect username or password. Please try again.");


            // Format: {username}|{2facode}
            string[] splittedUsername = username.Split('|');

            username = splittedUsername[0];
            totpCode = splittedUsername.Length == 2 ? splittedUsername[1] : "";

            try
            {
                userId = await services.users.GetUserIdFromUsername(username);
            }
            catch (RecordNotFoundException)
            {
                throw new UnauthorizedException(1, "Incorrect username or password. Please try again.");
            }
            await Login(username, password, userId, totpCode);
            var info = await services.users.GetUserById(userId);

            // will be removed later this is just a hack to get the website to work :sob:
            HttpContext.Response.Cookies.Append("USERID", info.userId.ToString(), new CookieOptions()
            {
                Domain = ".pekora.zip",
                Secure = false,
                Expires = DateTimeOffset.Now.Add(TimeSpan.FromDays(364)),
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.Lax,
            });

            return new
            {
                membershipType = 4,
                info.username,
                isUnder13 = false,
                countryCode = "US",
                userId,
                displayName = info.username,
                user = new
                {
                    id = userId,
                    name = info.username,
                    displayName = info.username
                },
                isBanned = false
            };
        }

        [HttpPostBypass("mobileapi/login")]
        public async Task<dynamic> LegacyLogin([FromBody] LegacyLoginRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.LoginEnabled);
            string totpCode = "";
            long userId;
            // Format: {username}|{2facode}
            string[] splittedUsername = request.username.Split('|');

            request.username = splittedUsername[0];
            totpCode = splittedUsername.Length == 2 ? splittedUsername[1] : "";

            if (string.IsNullOrEmpty(request.username) || string.IsNullOrEmpty(request.password))
                throw new UnauthorizedException(1, "Incorrect username or password. Please try again.");

            try
            {
                userId = await services.users.GetUserIdFromUsername(request.username);
            }
            catch (RecordNotFoundException)
            {
                throw new UnauthorizedException(1, "Incorrect username or password. Please try again.");
            }

            if(await Login(request.username, request.password, userId, totpCode))
                await CreateSessionAndSetCookie(userId);

            var userBalance = await services.economy.GetUserBalance(userId);
            return new
            {
                Status = "OK",
                UserInfo = new
                {
                    UserName = request.username,
                    RobuxBalance = userBalance.robux,
                    TicketsBalance = userBalance.tickets,
                    IsAnyBuildersClubMember = true,
                    ThumbnailUrl = $"{Configuration.BaseUrl}/Thumbs/Avatar.ashx?userId={userId}",
                    UserID = userId
                }
            };
        }
        private async Task CreateSessionAndSetCookie(long userId)
        {
            var sessionCookie = Middleware.SessionMiddleware.CreateJwt(new Middleware.JwtEntry()
            {
                sessionId = await services.users.CreateSession(userId),
                createdAt = DateTimeOffset.Now.ToUnixTimeSeconds(),
            });
            HttpContext.Response.Cookies.Append(Middleware.SessionMiddleware.CookieName, sessionCookie, new CookieOptions()
            {
                Domain = ".pekora.zip",
                Secure = false,
                Expires = DateTimeOffset.Now.Add(TimeSpan.FromDays(364)),
                IsEssential = true,
                Path = "/",
                SameSite = SameSiteMode.None,
            });
        }

        private async Task<bool> Login(string username, string password, long userId, string? totpCode)
        {
            //get totp info
            TotpInfo totpInfo = await services.users.GetOrSetTotp(userId);
            if (totpInfo.status == TotpStatus.Enabled)
            {
                //null check
                if (string.IsNullOrEmpty(totpCode))
                    throw new UnauthorizedException(1, $"You have 2FA enabled. Please login with this username format {username}|2FA Code");

                //verify totp code
                if (!services.users.VerifyTotp(totpInfo.secret, totpCode))
                    throw new UnauthorizedException(1, "Incorrect 2FA code. Please try again.");
            }

            if (!await services.users.VerifyPassword(userId, password))
                throw new UnauthorizedException(1, "Incorrect username or password. Please try again");

            return true;
        }
    }
}