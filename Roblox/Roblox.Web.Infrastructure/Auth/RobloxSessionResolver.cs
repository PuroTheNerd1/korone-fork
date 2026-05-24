using JWT.Exceptions;
using Microsoft.AspNetCore.Http;
using Roblox.Dto.Users;
using Roblox.Models.Sessions;
using Roblox.Services;
using Roblox.Services.Exceptions;
using Roblox.Web.Infrastructure.Http;

namespace Roblox.Web.Infrastructure.Auth;

public static class RobloxSessionResolver
{
    public static async Task<RobloxResolvedSession?> TryResolveFromCookie(HttpContext context)
    {
        var cookie = GetCookieValue(context);
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return null;
        }

        try
        {
            var decoded = RobloxSessionTokenCodec.DecodeJwt<SessionTokenPayload>(cookie);
            if (string.IsNullOrWhiteSpace(decoded.sessionId))
            {
                return null;
            }

            using var users = ServiceProvider.GetOrCreate<UsersService>();
            var sessionInfo = await users.GetSessionById(decoded.sessionId);
            var userInfo = await users.GetUserById(sessionInfo.userId);
            var session = new UserSession(
                userInfo.userId,
                userInfo.username,
                userInfo.created,
                userInfo.accountStatus,
                0,
                false,
                decoded.sessionId);

            return new RobloxResolvedSession
            {
                EncodedCookie = cookie,
                Session = session,
                UserInfo = userInfo,
            };
        }
        catch (Exception exception) when (exception is InvalidTokenPartsException or NullReferenceException or FormatException or SignatureVerificationException or RecordNotFoundException)
        {
            return null;
        }
    }

    public static string? GetCookieValue(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(RobloxWebContextConstants.SessionCookieName, out var cookie))
        {
            return cookie;
        }

        if (context.Request.Cookies.TryGetValue(RobloxWebContextConstants.AltSessionCookieName, out var altCookie))
        {
            return altCookie;
        }

        return null;
    }
}
