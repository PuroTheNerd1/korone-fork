using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Roblox.Web.Infrastructure.Http;

public static class RobloxIpHasher
{
    private class RedisIpHashSetupV1
    {
        public Dictionary<string, string> digitToGuid { get; set; } = new();
        public string endKey { get; set; } = string.Empty;
    }

    private static readonly Mutex RedisIpHashSetupMux = new();
    private static RedisIpHashSetupV1? _redisIpHashSetup;

    public static string GetRequesterIpRaw(HttpContext ctx)
    {
        Debug.Assert(ctx != null);
        var headers = ctx.Request.Headers;
        if (headers.ContainsKey("cf-connecting-ip"))
        {
            return headers["cf-connecting-ip"]!;
        }

        var ipString = ctx.Connection.RemoteIpAddress?.ToString();
        if (string.IsNullOrEmpty(ipString))
        {
            throw new Exception("Bad IP address - empty or null string");
        }

        return ipString;
    }

    public static void InitializeIpHashSetup()
    {
        if (_redisIpHashSetup != null)
        {
            return;
        }

        lock (RedisIpHashSetupMux)
        {
            if (_redisIpHashSetup != null)
            {
                return;
            }

            const string key = "IpHashKeyV1";
            var data = Roblox.Services.Cache.distributed.StringGet(key);
            if (data == null)
            {
                var created = new RedisIpHashSetupV1();
                for (var i = 0; i < 10; i++)
                {
                    created.digitToGuid[i.ToString()] = Guid.NewGuid() + Guid.NewGuid().ToString();
                }

                created.endKey = Guid.NewGuid() + Guid.NewGuid().ToString();
                Roblox.Services.Cache.distributed.StringSet(key, JsonSerializer.Serialize(created));
                _redisIpHashSetup = created;
                return;
            }

            _redisIpHashSetup = JsonSerializer.Deserialize<RedisIpHashSetupV1>(data);
        }
    }

    public static ulong ConvertFromIpAddressToInteger(string ipAddress)
    {
        var address = IPAddress.Parse(ipAddress);
        address = address.MapToIPv6();
        var bytes = address.GetAddressBytes();

        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        return BitConverter.ToUInt64(bytes, 0);
    }

    public static string GetIP(string trueIp, string? salt = null)
    {
        InitializeIpHashSetup();
        var ip = ConvertFromIpAddressToInteger(trueIp);
        var ipString = ip.ToString();
        var first = ipString.Substring(0, 1);
        var last = ipString.Substring(ipString.Length - 1, 1);
        var keyToUse = _redisIpHashSetup!.digitToGuid[first];
        if (first is "2" or "6" or "3" or "7")
        {
            keyToUse = new string(keyToUse.ToCharArray().Reverse().ToArray());
        }

        var key = keyToUse + ip;
        key += last != "9"
            ? _redisIpHashSetup.digitToGuid[last]
            : _redisIpHashSetup.digitToGuid[last].ToUpperInvariant();

        for (var i = 0; i < ipString.Length; i++)
        {
            var toAdd = _redisIpHashSetup.digitToGuid[ipString[i].ToString()];
            toAdd = toAdd.Length >= i + 1 ? toAdd.Substring(i, 1) : toAdd.Substring(0, Math.Min(2, toAdd.Length));
            key += toAdd;
        }

        if (salt != null)
        {
            key += salt;
        }

        using var alg = SHA512.Create();
        var hash = alg.ComputeHash(Encoding.UTF8.GetBytes(key + _redisIpHashSetup.endKey));
        return Convert.ToBase64String(hash);
    }
}
