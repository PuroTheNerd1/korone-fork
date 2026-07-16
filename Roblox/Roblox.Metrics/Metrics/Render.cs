namespace Roblox.Metrics;

public static class RenderMetrics
{
    public static void ReportAvatarThumbnailFailure(bool nullBody = false) => RobloxMetrics.RenderFailures.Add(1,
        new KeyValuePair<string, object?>("render.type", "avatar_thumbnail"),
        new KeyValuePair<string, object?>("failure.reason", nullBody ? "null_body" : "render_failed"));

    public static void ReportAvatarThumbnailDuration(long elapsedMilliseconds) => RobloxMetrics.RenderDuration.Record(elapsedMilliseconds,
        new KeyValuePair<string, object?>("render.type", "avatar_thumbnail"));
}
