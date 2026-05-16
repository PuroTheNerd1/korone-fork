using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
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

    public const short OutcomeIssued = 0;
    public const short OutcomeConsumedOk = 1;
    public const short OutcomeBadSignature = 2;
    public const short OutcomeMissingOrReplayed = 3;
    public const short OutcomeStaleTimestamp = 4;
    public const short OutcomeAssetMismatch = 5;

    public sealed record IssuedAttestation(string ticketId, string keyMaterial, long expiresAtMs);
    public sealed record ConsumedAttestation(long assetId, string keyMaterial);
    public sealed record IssuedPageToken(string pageToken, long expiresAtMs);

    private static readonly HttpClient TurnstileHttp = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(5),
    };

    private sealed class TurnstileResponse
    {
        [JsonPropertyName("success")] public bool Success { get; set; }
        [JsonPropertyName("error-codes")] public string[]? ErrorCodes { get; set; }
        [JsonPropertyName("challenge_ts")] public string? ChallengeTs { get; set; }
        [JsonPropertyName("hostname")] public string? Hostname { get; set; }
    }

    public async Task EnforceTurnstileAsync(string? token, string? remoteIp)
    {
        var secret = Roblox.Configuration.InvisibleTurnstileSecretKey;
        if (string.IsNullOrEmpty(secret))
            throw new RobloxException(503, 0, "Could not verify purchase");
        if (string.IsNullOrWhiteSpace(token))
            throw new RobloxException(400, 0, "Could not verify purchase");

        var form = new List<KeyValuePair<string, string>>
        {
            new("secret", secret),
            new("response", token),
        };
        if (!string.IsNullOrEmpty(remoteIp))
            form.Add(new("remoteip", remoteIp));

        try
        {
            using var content = new FormUrlEncodedContent(form);
            using var resp = await TurnstileHttp.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify", content);
            if (!resp.IsSuccessStatusCode)
                throw new RobloxException(400, 0, "Could not verify purchase");
            var parsed = await resp.Content.ReadFromJsonAsync<TurnstileResponse>();
            if (parsed == null || !parsed.Success)
                throw new RobloxException(400, 0, "Could not verify purchase");
        }
        catch (RobloxException)
        {
            throw;
        }
        catch
        {
            throw new RobloxException(400, 0, "Could not verify purchase");
        }
    }

    public async Task<IssuedPageToken> MintPageToken(long assetId)
    {
        var tokenBytes = new byte[PageTokenByteLen];
        RandomNumberGenerator.Fill(tokenBytes);
        var pageToken = Convert.ToHexString(tokenBytes).ToLowerInvariant();
        await redis.StringSetAsync(PageTokenKey(pageToken), assetId.ToString(), PageTokenTtl);
        var expiresAt = DateTimeOffset.UtcNow.Add(PageTokenTtl).ToUnixTimeMilliseconds();
        return new IssuedPageToken(pageToken, expiresAt);
    }

    public async Task EnforcePageTokenAsync(string? pageToken, long assetId)
    {
        if (string.IsNullOrWhiteSpace(pageToken) || pageToken.Length != PageTokenByteLen * 2)
            throw new RobloxException(400, 0, "Purchase failed (E1042)");
        foreach (var c in pageToken)
        {
            if (!(c is >= '0' and <= '9' or >= 'a' and <= 'f'))
                throw new RobloxException(400, 0, "Purchase failed (E1118)");
        }
        var raw = await redis.StringGetAsync(PageTokenKey(pageToken));
        if (raw == null)
            throw new RobloxException(400, 0, "Purchase failed (E1207)");
        if (!long.TryParse(raw, out var storedAssetId))
            throw new RobloxException(400, 0, "Purchase failed (E1334)");
        if (storedAssetId != assetId)
            throw new RobloxException(400, 0, "Purchase failed (E1455)");
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

    private static string PageTokenKey(string pageToken) =>
        $"econ:page:v1:{pageToken}";

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max);

    public bool IsThreadSafe() => true;
    public bool IsReusable() => false;
}
