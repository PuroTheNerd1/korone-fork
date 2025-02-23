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
            string username = request.cvalue;
            string password = request.password;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                throw new BadRequestException(3, "Username or password is missing.");

            // Format: {username}|{2facode}
            string[] splittedUsername = username.Split('|');

            username = splittedUsername[0];
            string totpCode = splittedUsername.Length == 2 ? splittedUsername[1] : "";

            UserInfo userInfo;
            try
            {
                userInfo = await services.users.GetUserByName(username);
            }
            catch (RecordNotFoundException)
            {
                throw new ForbiddenException(1, "Incorrect username or password. Please try again.");
            }

            if (await Login(userInfo.username, request.password, userInfo.userId, totpCode))
                await CreateSessionAndSetCookie(userInfo.userId);


            return new
            {
                user = new
                {
                    id = userInfo.userId,
                    name = userInfo.username,
                    displayName = userInfo.username,
                },
                isBanned = userInfo.IsDeleted()
            };

        }

        [HttpPostBypass("v2/login")]
        public async Task<dynamic> LoginV2()
        {
            FeatureFlags.FeatureCheck(FeatureFlag.LoginEnabled);
            string requestBody = await GetRequestBody();
            string? username = "";
            string? password = "";

            if (string.IsNullOrEmpty(requestBody))
                throw new BadRequestException(8, "Empty request body.");

            if (UserAgent == "RobloxStudio/WinInet")
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
                throw new BadRequestException(3, "Username and Password are required. Please try again.");


            // Format: {username}|{2facode}
            string[] splittedUsername = username.Split('|');

            username = splittedUsername[0];
            string totpCode = splittedUsername.Length == 2 ? splittedUsername[1] : "";

            UserInfo userInfo;
            try
            {
                userInfo = await services.users.GetUserByName(username);
            }
            catch (RecordNotFoundException)
            {
                throw new ForbiddenException(1, "Incorrect username or password. Please try again.");
            }

            if (await Login(username, password, userInfo.userId, totpCode))
                await CreateSessionAndSetCookie(userInfo.userId);

            // will be removed later this is just a hack to get the website to work :sob:
            HttpContext.Response.Cookies.Append("USERID", userInfo.userId.ToString(), new CookieOptions()
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
                userInfo.username,
                name = userInfo.username,
                isUnder13 = false,
                countryCode = "US",
                userId = userInfo.userId,
                id = userInfo.userId,
                displayName = userInfo.username,
                user = new
                {
                    id = userInfo.userId,
                    name = userInfo.username,
                    displayName = userInfo.username
                },
                isBanned = false
            };
        }

        [HttpPostBypass("mobileapi/login")]
        public async Task<dynamic> LegacyLogin([FromBody] LegacyLoginRequest request)
        {
            FeatureFlags.FeatureCheck(FeatureFlag.LoginEnabled);
            // Format: {username}|{2facode}
            string[] splittedUsername = request.username.Split('|');

            request.username = splittedUsername[0];
            
            string totpCode = splittedUsername.Length == 2 ? splittedUsername[1] : "";

            if (string.IsNullOrEmpty(request.username) || string.IsNullOrEmpty(request.password))
                throw new ForbiddenException(1, "Incorrect username or password. Please try again.");

            UserInfo userInfo;
            try
            {
                userInfo = await services.users.GetUserByName(request.username);
            }
            catch (RecordNotFoundException)
            {
                throw new ForbiddenException(1, "Incorrect username or password. Please try again.");
            }

            if(await Login(request.username, request.password, userInfo.userId, totpCode))
                await CreateSessionAndSetCookie(userInfo.userId);
            var userBalance = await services.economy.GetUserBalance(userInfo.userId);
            return new
            {
                Status = "OK",
                UserInfo = new
                {
                    UserName = request.username,
                    RobuxBalance = userBalance.robux,
                    TicketsBalance = userBalance.tickets,
                    IsAnyBuildersClubMember = true,
                    ThumbnailUrl = $"{Configuration.BaseUrl}/Thumbs/Avatar.ashx?userId={userInfo.userId}",
                    UserID = userInfo.userId
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
                    throw new ForbiddenException(6, $"You have 2FA enabled. Please login with this username format {username}|2FA Code");

                //verify totp code
                if (!services.users.VerifyTotp(totpInfo.secret, totpCode))
                    throw new ForbiddenException(6, "Incorrect 2FA code. Please try again.");
            }

            if (!await services.users.VerifyPassword(userId, password))
                throw new ForbiddenException(1, "Incorrect username or password. Please try again");

            return true;
        }
    }
}