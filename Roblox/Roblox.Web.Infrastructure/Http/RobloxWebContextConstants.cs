namespace Roblox.Web.Infrastructure.Http;

public static class RobloxWebContextConstants
{
    public const string SessionCookieName = ".PUPPYSECURITY";
    public const string AltSessionCookieName = ".PEKORASECURITY";
    public const string RobloxSessionCookieName = ".ROBLOSECURITY";
    public const string CsrfCookieName = "rbxcsrf4";
    public const string DiscordCookieName = "PEKORA-DISCORD";
    public const string RobloxCookieName = "PEKORA-ROBLOX";
    public const string ProxyAuthorizationHeaderName = "rblx-authorization";
    public const string RequestContextItemKey = "Roblox.Web.Infrastructure.RequestContext";
    public const string LegacySessionItemKey = SessionCookieName;

    public const string UserIdHeaderName = "X-Pekora-UserId";
    public const string UsernameHeaderName = "X-Pekora-Username";
    public const string SessionIdHeaderName = "X-Pekora-SessionId";
    public const string AccountStatusHeaderName = "X-Pekora-AccountStatus";
    public const string AuthTypeHeaderName = "X-Pekora-AuthType";
    public const string GameIdHeaderName = "X-Pekora-GameId";
    public const string PlaceIdHeaderName = "X-Pekora-PlaceId";
    public const string ClientIpHashHeaderName = "X-Pekora-ClientIpHash";
    public const string UserAgentHeaderName = "X-Pekora-UserAgent";
}
