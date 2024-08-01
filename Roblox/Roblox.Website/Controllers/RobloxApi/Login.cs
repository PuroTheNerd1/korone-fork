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
            bool isMobile = userAgent.Contains("ROBLOX Android App") || userAgent.ToLower().Contains("roblox ios app");
            userAgent = Request.Headers["User-Agent"]; 
            Console.WriteLine(userAgent);
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
                    if (isMobile)
                    {
                        var serializedResponse = JsonConvert.DeserializeObject<LoginRequestMobileV2>(requestBody) ?? new LoginRequestMobileV2();
                        username = serializedResponse.username;
                        password = serializedResponse.password;
                    }
                    else
                    {
                        var serializedResponse = JsonConvert.DeserializeObject<LoginRequest>(requestBody) ?? new LoginRequest();
                        username = serializedResponse.cvalue;
                        password = serializedResponse.password;
                    }
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
            var info = await services.users.GetUserById(userId);
            var isBanned =
                info.accountStatus != AccountStatus.Ok && 
                info.accountStatus != AccountStatus.MustValidateEmail && 
                info.accountStatus != AccountStatus.Suppressed;
            if(isMobile)
            {
                return new
                {
                    membershipType = 4,
                    username = username,
                    isUnder13 = false,
                    countryCode = "US",
                    userId = userId,
                    displayName = username
                };
            }
            else{
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
        }
    }
}