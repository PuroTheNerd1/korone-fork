using System.Security.Cryptography;
using System.Text;
using Roblox.Services.Exceptions;

namespace Roblox.Services;

public static class PurchaseAttestationGuard
{
    private const long ClockSkewToleranceMs = 5_000;
    private const long MaxAgeMs = 30_000;

    public static string Canonicalize(long assetId, long expectedPrice, string ticketId, long timestampMs) =>
        $"{assetId}|{expectedPrice}|{ticketId}|{timestampMs}";

    public readonly record struct ParsedSeal(string TicketId, long TimestampMs, string SignatureHex);

    public static ParsedSeal ParseSealHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            throw new RobloxException(400, 0, "Purchase failed (E1951)");

        string? tStr = null;
        string? kStr = null;
        string? vStr = null;
        foreach (var part in header.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            var name = part.Substring(0, eq);
            var value = part.Substring(eq + 1);
            switch (name)
            {
                case "t": tStr = value; break;
                case "k": kStr = value; break;
                case "v": vStr = value; break;
            }
        }

        if (tStr == null || kStr == null || vStr == null)
            throw new RobloxException(400, 0, "Purchase failed (E2089)");
        if (!long.TryParse(tStr, out var ts))
            throw new RobloxException(400, 0, "Purchase failed (E2113)");
        if (kStr.Length is < 8 or > 64)
            throw new RobloxException(400, 0, "Purchase failed (E2128)");
        if (vStr.Length != 64)
            throw new RobloxException(400, 0, "Purchase failed (E2147)");

        return new ParsedSeal(kStr, ts, vStr);
    }

    public static async Task EnforceAsync(
        PurchaseAttestationService svc,
        long userId,
        long assetId,
        long expectedPrice,
        string sealHeader)
    {
        var parsed = ParseSealHeader(sealHeader);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (parsed.TimestampMs > now + ClockSkewToleranceMs ||
            parsed.TimestampMs < now - MaxAgeMs)
        {
            _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeStaleTimestamp, expectedPrice);
            throw new RobloxException(400, 0, "Purchase failed (E2156)");
        }

        var consumed = await svc.Consume(userId, parsed.TicketId);
        if (consumed == null)
        {
            _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeMissingOrReplayed, expectedPrice);
            throw new RobloxException(400, 0, "Purchase failed (E2233)");
        }

        if (consumed.assetId != assetId)
        {
            _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeAssetMismatch, expectedPrice);
            throw new RobloxException(400, 0, "Purchase failed (E2367)");
        }

        var ageMs = now - consumed.issuedAtMs;
        if (ageMs < PurchaseAttestationService.TicketMinAgeMs)
        {
            _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeMinAgeViolation, expectedPrice);
            throw new RobloxException(400, 0, "Purchase failed (E2418)");
        }

        var canonical = Canonicalize(assetId, expectedPrice, parsed.TicketId, parsed.TimestampMs);

        byte[] keyRaw;
        byte[] actualBytes;
        try
        {
            keyRaw = Convert.FromBase64String(consumed.keyMaterial);
            actualBytes = Convert.FromHexString(parsed.SignatureHex);
        }
        catch
        {
            _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeBadSignature, expectedPrice);
            throw new RobloxException(400, 0, "Purchase failed (E2541)");
        }

        var expectedBytes = HMACSHA256.HashData(keyRaw, Encoding.UTF8.GetBytes(canonical));

        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
        {
            _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeBadSignature, expectedPrice);
            throw new RobloxException(400, 0, "Purchase failed (E2638)");
        }

        _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeConsumedOk, expectedPrice);
    }
}
