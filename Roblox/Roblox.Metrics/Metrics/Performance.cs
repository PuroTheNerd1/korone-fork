using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

namespace Roblox.Metrics;

public static class PerformanceMetrics
{
    private static string SanitizeTag(string? value, string fallback = "unknown")
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var sanitized = value.Trim();
        return sanitized.Length > 96 ? sanitized[..96] : sanitized;
    }

    public static void ReportRedisLookup(string prefix, string layer, bool hit)
    {
        RobloxInfluxDb.WritePointInBackground(PointData
            .Measurement("RedisCache")
            .Tag("prefix", SanitizeTag(prefix))
            .Tag("layer", SanitizeTag(layer))
            .Tag("result", hit ? "hit" : "miss")
            .Field("count", 1)
            .Timestamp(DateTime.UtcNow, WritePrecision.Ms));
    }

    public static void ReportDbDuration(string operation, long elapsedMilliseconds, bool slow)
    {
        RobloxInfluxDb.WritePointInBackground(PointData
            .Measurement("Database")
            .Tag("operation", SanitizeTag(operation))
            .Tag("slow", slow ? "true" : "false")
            .Field("elapsed_ms", elapsedMilliseconds)
            .Timestamp(DateTime.UtcNow, WritePrecision.Ms));
    }

    public static void ReportEndpointDuration(string route, string method, int statusCode, long elapsedMilliseconds)
    {
        RobloxInfluxDb.WritePointInBackground(PointData
            .Measurement("Endpoint")
            .Tag("route", SanitizeTag(route))
            .Tag("method", SanitizeTag(method))
            .Tag("status", statusCode.ToString())
            .Field("elapsed_ms", elapsedMilliseconds)
            .Timestamp(DateTime.UtcNow, WritePrecision.Ms));
    }
}
