using MVC = Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Dynamic;
using Microsoft.AspNetCore.Mvc;
using Roblox.Services.Exceptions;
using Roblox.Website.WebsiteModels.Authentication;
using System.Text;
using System.Web;
using Roblox.Models.Users;
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
            bool isMobile = userAgent.Contains("ROBLOX Android App") || userAgent.ToLower().Contains("ios");
            Console.WriteLine(userAgent);
            string username = "";
            string password = "";
            long userId;

            using (StreamReader reader = new StreamReader(HttpContext.Request.Body, Encoding.UTF8))
            {
                requestBody = await reader.ReadToEndAsync();
            }
            Console.WriteLine(requestBody);
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
                if (isMobile)
                {
                    var loginRequest = JsonConvert.DeserializeObject<LoginRequestMobileV2>(requestBody);
                    username = loginRequest?.username ?? string.Empty;
                    password = loginRequest?.password ?? string.Empty;
                }
                else
                {
                    var loginRequest = JsonConvert.DeserializeObject<LoginRequest>(requestBody);
                    username = loginRequest?.cvalue ?? string.Empty;
                    password = loginRequest?.password ?? string.Empty;
                }
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new Roblox.Exceptions.ForbiddenException(1, "Username or password is missing.");
            }

            try
            {
                userId = await services.users.GetUserIdFromUsername(username);
                if (!await services.users.VerifyPassword(userId, password))
                {
                    throw new Roblox.Exceptions.ForbiddenException(1, "Incorrect username or password. Please try again.");
                }
            }
            catch (RecordNotFoundException)
            {
                throw new Roblox.Exceptions.ForbiddenException(1, "Incorrect username or password. Please try again.");
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

            return isMobile ? (dynamic)new
            {
                membershipType = 4,
                username,
                isUnder13 = false,
                countryCode = "US",
                userId,
                displayName = username
            } : new
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
    }
}