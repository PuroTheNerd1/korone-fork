using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Roblox.Cache;

namespace Roblox.Website.Services;

public record NonceClaim(long userId, long assetId, long price, long timestamp);
public record NonceLock(string nonce, long timestamp, string signature);

public static class ChallengeLockService
{
    private static byte[] _keyBytes = null!;
    private const string RedisPrefix = "purchase-lock:";
    private static readonly TimeSpan NonceTtl = TimeSpan.FromSeconds(30);
    private static readonly Regex NoncePattern = new(@"^[A-Za-z0-9_-]{32}$", RegexOptions.Compiled);

    public static void Configure(string secretKey)
    {
        if (_keyBytes is not null) throw new InvalidOperationException("Already configured");
        _keyBytes = Encoding.UTF8.GetBytes(secretKey);
    }

    public static async Task<NonceLock> IssueNonce(long userId, long assetId, long price)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
                          .Replace("+", "-").Replace("/", "_").Replace("=", "");
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var value = $"{userId}:{assetId}:{price}:{timestamp}";
        var cache = new DistributedCache();
        await cache.StringSetAsync(RedisPrefix + nonce, value, NonceTtl);
        return new NonceLock(nonce, timestamp, ComputeHmac(assetId, price, nonce, timestamp));
    }

    public static async Task<NonceClaim?> ConsumeNonce(string nonce)
    {
        if (!NoncePattern.IsMatch(nonce)) return null;
        var db = DistributedCache.redis.GetDatabase(0);
        var value = (string?)await db.StringGetDeleteAsync(RedisPrefix + nonce);
        if (value is null) return null;
        var parts = value.Split(':');
        if (parts.Length != 4) return null;
        if (!long.TryParse(parts[0], out var userId) ||
            !long.TryParse(parts[1], out var assetId) ||
            !long.TryParse(parts[2], out var price) ||
            !long.TryParse(parts[3], out var timestamp)) return null;
        return new NonceClaim(userId, assetId, price, timestamp);
    }

    public static string ComputeHmac(long assetId, long price, string nonce, long timestamp)
    {
        var message = $"{assetId}:{price}:{nonce}:{timestamp}";
        using var hmac = new HMACSHA256(_keyBytes);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
    }
}
