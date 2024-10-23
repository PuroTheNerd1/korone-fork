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
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class RobloxLogin: ControllerBase
    {
        [HttpPostBypass("v1/login")]
        public async Task<dynamic> LoginV1([FromBody]LoginRequest request)
        {
            long userId;
            string username = request.cvalue;
            string password = request.password;
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
            var info = await services.users.GetUserById(userId);
            var isBanned =
                info.accountStatus != AccountStatus.Ok && 
                info.accountStatus != AccountStatus.MustValidateEmail && 
                info.accountStatus != AccountStatus.Suppressed;
            return new 
            {
                user = new
                {
                    id = userId,
                    name = username,
                    displayName = username
                },
                isBanned
            };

        }
        [HttpPostBypass("v2/login")]
        public async Task<dynamic> LoginV2()
        {
            string requestBody;
            string userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            bool isMobile = userAgent.Contains("ROBLOX Android App") || userAgent.ToLower().Contains("App");
            string username = "";
            string password = "";
            string totpCode = "";
            long userId;

            using (StreamReader reader = new StreamReader(HttpContext.Request.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
            }
            if (string.IsNullOrEmpty(requestBody))
            {
                throw new Roblox.Exceptions.ForbiddenException(1, "Request body is empty.");
            }

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
            {
                throw new Roblox.Exceptions.ForbiddenException(1, "Username or password is missing.");
            }
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
                throw new Roblox.Exceptions.ForbiddenException(1, "Incorrect username or password. Please try again.");
            }
            //verify password first
            if (!await services.users.VerifyPassword(userId, password))
            {
                throw new Roblox.Exceptions.ForbiddenException(1, "Incorrect username or password. Please try again.");
            }
            //get totp info
            TotpInfo totpInfo = await services.users.GetOrSetTotp(userId);
            if (totpInfo.status == TotpStatus.Enabled)
            {
                //null check
                if (string.IsNullOrEmpty(totpCode))
                {
                    throw new Roblox.Exceptions.ForbiddenException(1, $"You have 2FA enabled. Please login with this username format {username}|2FA Code");
                }
                //verify totp code
                if(!await services.users.VerifyTotp(totpInfo.secret, splittedUsername[1]))
                {
                    throw new Roblox.Exceptions.ForbiddenException(1, "Incorrect 2FA code. Please try again.");
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
            var info = await services.users.GetUserById(userId);
            var isBanned = info.accountStatus != AccountStatus.Ok &&
                        info.accountStatus != AccountStatus.MustValidateEmail &&
                        info.accountStatus != AccountStatus.Suppressed;
            return new
            {
                membershipType = 4,
                username,
                isUnder13 = false,
                countryCode = "US",
                userId,
                displayName = username,
                user = new
                {
                    id = userId,
                    name = username,
                    displayName = username
                },
                isBanned
            };
        }
    }
}