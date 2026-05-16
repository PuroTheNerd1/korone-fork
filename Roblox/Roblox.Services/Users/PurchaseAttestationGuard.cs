using System.Security.Cryptography;
using System.Text;
using Roblox.Services.Exceptions;

namespace Roblox.Services;

public static class PurchaseAttestationGuard
{
    public static string Canonicalize(long assetId, long expectedPrice, string ticketId) =>
        $"{assetId}|{expectedPrice}|{ticketId}";

    public readonly record struct ParsedSeal(string TicketId, string SignatureHex);

    public static ParsedSeal ParseSealHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
            throw new RobloxException(400, 0, "Purchase failed (E1951)");

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
                case "k": kStr = value; break;
                case "v": vStr = value; break;
            }
        }

        if (kStr == null || vStr == null)
            throw new RobloxException(400, 0, "Purchase failed (E2089)");
        if (kStr.Length is < 8 or > 64)
            throw new RobloxException(400, 0, "Purchase failed (E2128)");
        if (vStr.Length != 64)
            throw new RobloxException(400, 0, "Purchase failed (E2147)");

        return new ParsedSeal(kStr, vStr);
    }

    public static async Task EnforceAsync(
        PurchaseAttestationService svc,
        long userId,
        long assetId,
        long expectedPrice,
        string sealHeader)
    {
        var parsed = ParseSealHeader(sealHeader);

        var consumed = await svc.Consume(userId, parsed.TicketId);
        if (consumed == null)
        {
            _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeMissingOrReplayed);
            throw new RobloxException(400, 0, "Purchase failed (E2233)");
        }

        if (consumed.assetId != assetId)
        {
            _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeAssetMismatch);
            throw new RobloxException(400, 0, "Purchase failed (E2367)");
        }

        var canonical = Canonicalize(assetId, expectedPrice, parsed.TicketId);

        byte[] keyRaw;
        byte[] actualBytes;
        try
        {
            keyRaw = Convert.FromBase64String(consumed.keyMaterial);
            actualBytes = Convert.FromHexString(parsed.SignatureHex);
        }
        catch
        {
            _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeBadSignature);
            throw new RobloxException(400, 0, "Purchase failed (E2541)");
        }

        var expectedBytes = HMACSHA256.HashData(keyRaw, Encoding.UTF8.GetBytes(canonical));

        if (!CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
        {
            _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeBadSignature);
            throw new RobloxException(400, 0, "Purchase failed (E2638)");
        }

        _ = svc.MarkOutcome(userId, parsed.TicketId, PurchaseAttestationService.OutcomeConsumedOk);
    }
}
