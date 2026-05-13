using System.Security.Cryptography;
using Dapper;
using Roblox.Cache;
using Roblox.Services.Exceptions;

namespace Roblox.Services;

public class PurchaseAttestationService : ServiceBase, IService
{
    private const int KeyByteLen = 32;
    private const int TicketByteLen = 16;
    private static readonly TimeSpan AttestationTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan HandshakeBurstWindow = TimeSpan.FromMilliseconds(750);
    private static readonly TimeSpan HandshakeSlidingWindow = TimeSpan.FromMinutes(1);
    private const int HandshakeMaxPerWindow = 30;

    public const short OutcomeIssued = 0;
    public const short OutcomeConsumedOk = 1;
    public const short OutcomeBadSignature = 2;
    public const short OutcomeMissingOrReplayed = 3;
    public const short OutcomeStaleTimestamp = 4;
    public const short OutcomeAssetMismatch = 5;

    public sealed record IssuedAttestation(string ticketId, string keyMaterial, long expiresAtMs);
    public sealed record ConsumedAttestation(long assetId, string keyMaterial);

    public async Task EnforceIssuanceRateLimit(long userId)
    {
        var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (long)HandshakeSlidingWindow.TotalSeconds;
        var counterKey = $"econ:attest:rl:{userId}:{bucket}";
        var redisDb = DistributedCache.redis.GetDatabase(0);
        var count = await redisDb.StringIncrementAsync(counterKey);
        if (count == 1)
            await redisDb.KeyExpireAsync(counterKey, HandshakeSlidingWindow + TimeSpan.FromSeconds(5));
        if (count > HandshakeMaxPerWindow)
            throw new RobloxException(429, 0, "TooManyRequests");
    }

    public async Task<IAsyncDisposable> AcquireIssuanceBurstLock(long userId)
    {
        var redLock = await Cache.redLock.CreateLockAsync(
            $"CheckoutHandshake:{userId}", HandshakeBurstWindow);
        if (!redLock.IsAcquired)
        {
            await redLock.DisposeAsync();
            throw new RobloxException(429, 0, "TooManyRequests");
        }
        return redLock;
    }

    public async Task<IssuedAttestation> Issue(long userId, long assetId, string? clientIpHash, string? userAgent)
    {
        var ticketBytes = new byte[TicketByteLen];
        var keyBytes = new byte[KeyByteLen];
        RandomNumberGenerator.Fill(ticketBytes);
        RandomNumberGenerator.Fill(keyBytes);

        var ticketId = Convert.ToHexString(ticketBytes).ToLowerInvariant();
        var keyMaterial = Convert.ToBase64String(keyBytes);
        var storedValue = $"{assetId}:{keyMaterial}";

        await redis.StringSetAsync(RedisKey(userId, ticketId), storedValue, AttestationTtl);

        await db.ExecuteAsync(
            @"INSERT INTO purchase_attestation_log (user_id, ticket_id, asset_id, outcome, client_ip_hash, user_agent)
              VALUES (:uid, :tid, :aid, :oc, :ip, :ua)",
            new
            {
                uid = userId,
                tid = ticketId,
                aid = assetId,
                oc = OutcomeIssued,
                ip = clientIpHash,
                ua = userAgent == null ? null : Truncate(userAgent, 512),
            });

        var expiresAt = DateTimeOffset.UtcNow.Add(AttestationTtl).ToUnixTimeMilliseconds();
        return new IssuedAttestation(ticketId, keyMaterial, expiresAt);
    }

    public async Task<ConsumedAttestation?> Consume(long userId, string ticketId)
    {
        var raw = await redis.StringGetDeleteAsync(RedisKey(userId, ticketId));
        if (raw == null) return null;
        var sep = raw.IndexOf(':');
        if (sep <= 0) return null;
        if (!long.TryParse(raw.Substring(0, sep), out var aid)) return null;
        var key = raw.Substring(sep + 1);
        if (key.Length == 0) return null;
        return new ConsumedAttestation(aid, key);
    }

    public Task MarkOutcome(long userId, string ticketId, short outcome, long? expectedPrice) =>
        db.ExecuteAsync(
            @"UPDATE purchase_attestation_log
              SET outcome = :oc, consumed_at = now(), expected_price = :px
              WHERE user_id = :uid AND ticket_id = :tid",
            new
            {
                oc = outcome,
                px = expectedPrice,
                uid = userId,
                tid = ticketId,
            });

    private static string RedisKey(long userId, string ticketId) =>
        $"econ:attest:v1:{userId}:{ticketId}";

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max);

    public bool IsThreadSafe() => true;
    public bool IsReusable() => false;
}
