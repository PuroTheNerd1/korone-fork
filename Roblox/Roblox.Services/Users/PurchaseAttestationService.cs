using System.Security.Cryptography;
using Dapper;
using Roblox.Cache;
using Roblox.Services.Exceptions;

namespace Roblox.Services;

public class PurchaseAttestationService : ServiceBase, IService
{
    private const int KeyByteLen = 32;
    private const int TicketByteLen = 16;
    private const int PageTokenByteLen = 24;
    private static readonly TimeSpan AttestationTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PageTokenTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan HandshakeBurstWindow = TimeSpan.FromMilliseconds(750);
    private static readonly TimeSpan HandshakeSlidingWindow = TimeSpan.FromMinutes(1);
    private const int HandshakeMaxPerWindow = 30;
    private const int BehaviorScoreMinimum = 20;
    public const long TicketMinAgeMs = 1_500;
    public const long PageTokenMinDwellMs = 3_000;

    public const short OutcomeIssued = 0;
    public const short OutcomeConsumedOk = 1;
    public const short OutcomeBadSignature = 2;
    public const short OutcomeMissingOrReplayed = 3;
    public const short OutcomeStaleTimestamp = 4;
    public const short OutcomeAssetMismatch = 5;
    public const short OutcomeMinAgeViolation = 6;

    public sealed record IssuedAttestation(string ticketId, string keyMaterial, long expiresAtMs);
    public sealed record ConsumedAttestation(long assetId, long issuedAtMs, string keyMaterial);
    public sealed record IssuedPageToken(string pageToken, long expiresAtMs);

    public async Task EnforceIssuanceRateLimit(long userId)
    {
        var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (long)HandshakeSlidingWindow.TotalSeconds;
        var counterKey = $"econ:attest:rl:{userId}:{bucket}";
        var redisDb = DistributedCache.redis.GetDatabase(0);
        var count = await redisDb.StringIncrementAsync(counterKey);
        if (count == 1)
            await redisDb.KeyExpireAsync(counterKey, HandshakeSlidingWindow + TimeSpan.FromSeconds(5));
        if (count > HandshakeMaxPerWindow)
            throw new RobloxException(429, 0, "Purchase failed (E3017)");
    }

    public async Task<IAsyncDisposable> AcquireIssuanceBurstLock(long userId)
    {
        var redLock = await Cache.redLock.CreateLockAsync(
            $"CheckoutHandshake:{userId}", HandshakeBurstWindow);
        if (!redLock.IsAcquired)
        {
            await redLock.DisposeAsync();
            throw new RobloxException(429, 0, "Purchase failed (E2916)");
        }
        return redLock;
    }

    public async Task<IssuedPageToken> MintPageToken(long assetId, string clientIpHash)
    {
        var tokenBytes = new byte[PageTokenByteLen];
        RandomNumberGenerator.Fill(tokenBytes);
        var pageToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        var mintedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var ipEscaped = (clientIpHash ?? "").Replace("|", "");
        var value = $"{assetId}|{mintedAtMs}|{ipEscaped}";
        await redis.StringSetAsync(PageTokenKey(pageToken), value, PageTokenTtl);
        var expiresAt = mintedAtMs + (long)PageTokenTtl.TotalMilliseconds;
        return new IssuedPageToken(pageToken, expiresAt);
    }

    public async Task EnforcePageTokenAsync(string? pageToken, long assetId, string callerIpHash)
    {
        if (string.IsNullOrWhiteSpace(pageToken) || pageToken.Length != PageTokenByteLen * 2)
            throw new RobloxException(400, 0, "Purchase failed (E1042)");
        foreach (var c in pageToken)
        {
            if (!(c is >= '0' and <= '9' or >= 'a' and <= 'f'))
                throw new RobloxException(400, 0, "Purchase failed (E1118)");
        }
        var raw = await redis.StringGetDeleteAsync(PageTokenKey(pageToken));
        if (raw == null)
            throw new RobloxException(400, 0, "Purchase failed (E1207)");
        var parts = raw.Split('|');
        if (parts.Length < 3
            || !long.TryParse(parts[0], out var storedAssetId)
            || !long.TryParse(parts[1], out var mintedAtMs))
            throw new RobloxException(400, 0, "Purchase failed (E1334)");
        var storedIpHash = parts[2];
        if (storedAssetId != assetId)
            throw new RobloxException(400, 0, "Purchase failed (E1455)");
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (now - mintedAtMs < PageTokenMinDwellMs)
            throw new RobloxException(400, 0, "Purchase failed (E1572)");
        if (!string.IsNullOrEmpty(storedIpHash) && !string.IsNullOrEmpty(callerIpHash)
            && !CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(storedIpHash),
                System.Text.Encoding.UTF8.GetBytes(callerIpHash)))
            throw new RobloxException(400, 0, "Purchase failed (E1689)");
    }

    public void EnforceBehaviorScore(int score)
    {
        if (score < BehaviorScoreMinimum)
            throw new RobloxException(400, 0, "Purchase failed (E1763)");
    }

    public async Task<IssuedAttestation> Issue(long userId, long assetId, string? clientIpHash, string? userAgent)
    {
        var ticketBytes = new byte[TicketByteLen];
        var keyBytes = new byte[KeyByteLen];
        RandomNumberGenerator.Fill(ticketBytes);
        RandomNumberGenerator.Fill(keyBytes);

        var ticketId = Convert.ToHexString(ticketBytes).ToLowerInvariant();
        var keyMaterial = Convert.ToBase64String(keyBytes);
        var issuedAtMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var storedValue = $"{assetId}:{issuedAtMs}:{keyMaterial}";

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

        var expiresAt = issuedAtMs + (long)AttestationTtl.TotalMilliseconds;
        return new IssuedAttestation(ticketId, keyMaterial, expiresAt);
    }

    public async Task<ConsumedAttestation?> Consume(long userId, string ticketId)
    {
        var raw = await redis.StringGetDeleteAsync(RedisKey(userId, ticketId));
        if (raw == null) return null;
        var firstSep = raw.IndexOf(':');
        if (firstSep <= 0) return null;
        var secondSep = raw.IndexOf(':', firstSep + 1);
        if (secondSep <= firstSep) return null;
        if (!long.TryParse(raw.Substring(0, firstSep), out var aid)) return null;
        if (!long.TryParse(raw.Substring(firstSep + 1, secondSep - firstSep - 1), out var issuedAtMs)) return null;
        var key = raw.Substring(secondSep + 1);
        if (key.Length == 0) return null;
        return new ConsumedAttestation(aid, issuedAtMs, key);
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

    private static string PageTokenKey(string pageToken) =>
        $"econ:page:v1:{pageToken}";

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max);

    public bool IsThreadSafe() => true;
    public bool IsReusable() => false;
}
