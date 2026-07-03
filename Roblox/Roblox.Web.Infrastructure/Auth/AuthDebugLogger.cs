using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Roblox.Web.Infrastructure.Http;

namespace Roblox.Web.Infrastructure.Auth;

public static class AuthDebugLogger
{
    private const string Path = @"C:\KoroneServices\auth.debug.log";
    private static readonly object Sync = new();

    public static void Write(HttpContext context, string message)
    {
        try
        {
            var directory = System.IO.Path.GetDirectoryName(Path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var line = string.Join(" ",
                DateTimeOffset.UtcNow.ToString("O"),
                $"host={context.Request.Host.Host}",
                $"path={context.Request.Path}",
                $"method={context.Request.Method}",
                message);

            lock (Sync)
            {
                File.AppendAllText(Path, line + Environment.NewLine);
            }
        }
        catch
        {
        }
    }

    public static string CookieNames(HttpContext context)
    {
        var names = context.Request.Cookies.Keys
            .Where(name => string.Equals(name, RobloxWebContextConstants.SessionCookieName, StringComparison.Ordinal) ||
                           string.Equals(name, RobloxWebContextConstants.AltSessionCookieName, StringComparison.Ordinal) ||
                           string.Equals(name, RobloxWebContextConstants.RobloxSessionCookieName, StringComparison.Ordinal))
            .ToArray();

        return names.Length == 0 ? "none" : string.Join(",", names);
    }

    public static string Fingerprint(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..12].ToLowerInvariant();
    }
}
