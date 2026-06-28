using Microsoft.AspNetCore.Http;
using Roblox.Web.Infrastructure.Http;

namespace Roblox.Web.Infrastructure.Auth;

public static class RobloxSessionCookieWriter
{
    public static string AppendSessionCookies(HttpContext httpContext, string sessionId)
    {
        var sessionCookie = RobloxSessionTokenCodec.CreateJwt(new SessionTokenPayload
        {
            sessionId = sessionId,
            createdAt = DateTimeOffset.Now.ToUnixTimeSeconds(),
        });

        AppendSessionCookiesForToken(httpContext, sessionCookie);
        return sessionCookie;
    }

    public static void AppendSessionCookiesForToken(HttpContext httpContext, string sessionCookie)
    {
        var options = CreateSessionCookieOptions();
        httpContext.Response.Cookies.Append(RobloxWebContextConstants.RobloxSessionCookieName, sessionCookie, options);
        httpContext.Response.Cookies.Append(RobloxWebContextConstants.SessionCookieName, sessionCookie, CreateSessionCookieOptions());
    }

    private static CookieOptions CreateSessionCookieOptions()
    {
        return new CookieOptions
        {
            Domain = $".{Roblox.Configuration.ShortBaseUrl}",
            Secure = false,
            Expires = DateTimeOffset.Now.Add(TimeSpan.FromDays(14)),
            IsEssential = true,
            Path = "/",
            SameSite = SameSiteMode.Lax,
        };
    }
}
