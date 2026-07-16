using System.Globalization;
using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Roblox.Cache;
using StackExchange.Redis;
using AssetType = Roblox.Models.Assets.Type;

namespace Roblox.Services.Assets;

public static class AssetRenderQueue
{
    public static bool Enabled { get; private set; } = true;
    public static void Configure(bool enabled) => Enabled = enabled;
    internal const string Stream = "render:assets:v2";
    internal const string DeadLetterStream = "render:assets:v2:dead";
    internal const string Group = "asset-renderers";
    private const string EnqueueScript = """
        if redis.call('SET', KEYS[1], '1', 'EX', 604800, 'NX') then
          return redis.call('XADD', KEYS[2], '*', 'assetId', ARGV[1], 'versionId', ARGV[2], 'assetType', ARGV[3], 'renderKind', ARGV[4], 'enqueuedAt', ARGV[5], 'attempt', ARGV[6])
        end
        return false
        """;

    public static async Task<bool> EnqueueAsync(long assetId, long assetVersionId, AssetType assetType,
        string renderKind = "thumbnail", int attempt = 0)
    {
        var database = DistributedCache.redis.GetDatabase();
        var identity = $"render:assets:v2:pending:{assetId}:{assetVersionId}:{(int)assetType}:{renderKind}";
        var result = await database.ScriptEvaluateAsync(EnqueueScript,
            [identity, Stream],
            [assetId, assetVersionId, (int)assetType, renderKind,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), attempt]);
        return !result.IsNull;
    }

    public static void Enqueue(long assetId, AssetType assetType) =>
        EnqueueAsync(assetId, 0, assetType).GetAwaiter().GetResult();

    internal static string DedupKey(AssetRenderJob job) =>
        $"render:assets:v2:pending:{job.AssetId}:{job.AssetVersionId}:{(int)job.AssetType}:{job.RenderKind}";
}

public sealed class AssetRenderQueueWorker(ILogger<AssetRenderQueueWorker> logger) : BackgroundService
{
    private static readonly TimeSpan[] RetryDelays =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(120)];
    private readonly string _consumerPrefix = $"{Environment.MachineName}-{Environment.ProcessId}-{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!AssetRenderQueue.Enabled) return;
        var database = DistributedCache.redis.GetDatabase();
        await EnsureGroupAsync(database, stoppingToken);

        await Task.WhenAll(ConsumeAsync(database, _consumerPrefix + "-1", stoppingToken),
            ConsumeAsync(database, _consumerPrefix + "-2", stoppingToken),
            ReconcileAsync(stoppingToken));
    }

    private async Task ConsumeAsync(IDatabase database, string consumer, CancellationToken cancellationToken)
    {
        var claimCursor = (RedisValue)"0-0";
        while (!cancellationToken.IsCancellationRequested)
        {
            StreamEntry[] entries;
            try
            {
                var claimed = await database.StreamAutoClaimAsync(AssetRenderQueue.Stream, AssetRenderQueue.Group,
                    consumer, 60_000, claimCursor, 1);
                claimCursor = claimed.NextStartId;
                entries = claimed.ClaimedEntries;
                if (entries.Length == 0)
                    entries = await database.StreamReadGroupAsync(AssetRenderQueue.Stream, AssetRenderQueue.Group,
                        consumer, ">", 1);
            }
            catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
            {
                logger.LogWarning(ex, "Asset render queue is temporarily unavailable");
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                continue;
            }
            catch (RedisServerException ex) when (ex.Message.Contains("NOGROUP", StringComparison.OrdinalIgnoreCase))
            {
                await EnsureGroupAsync(database, cancellationToken);
                continue;
            }

            if (entries.Length == 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
                continue;
            }

            await ProcessAsync(database, entries[0], cancellationToken);
        }
    }

    private async Task EnsureGroupAsync(IDatabase database, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await database.StreamCreateConsumerGroupAsync(AssetRenderQueue.Stream, AssetRenderQueue.Group, "0-0", true);
                return;
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP", StringComparison.OrdinalIgnoreCase)) { return; }
            catch (Exception ex) when (ex is RedisConnectionException or RedisTimeoutException)
            {
                logger.LogWarning(ex, "Waiting for Redis before starting the asset render queue");
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
    }

    private async Task ProcessAsync(IDatabase database, StreamEntry entry, CancellationToken cancellationToken)
    {
        AssetRenderJob job;
        try { job = AssetRenderJob.Parse(entry); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Discarding malformed asset render queue entry {EntryId}", entry.Id);
            await database.StreamAcknowledgeAsync(AssetRenderQueue.Stream, AssetRenderQueue.Group, entry.Id);
            await database.StreamDeleteAsync(AssetRenderQueue.Stream, [entry.Id]);
            return;
        }

        try
        {
            using var assets = ServiceProvider.GetOrCreate<AssetsService>();
            var latest = await assets.GetLatestAssetVersion(job.AssetId);
            if (job.AssetVersionId == 0 || latest.assetVersionId != job.AssetVersionId)
            {
                await AssetRenderQueue.EnqueueAsync(job.AssetId, latest.assetVersionId, job.AssetType, job.RenderKind);
            }
            else
            {
                await assets.RenderAssetAsync(job.AssetId, job.AssetType, cancellationToken, job.AssetVersionId);
                var current = await assets.GetLatestAssetVersion(job.AssetId);
                if (current.assetVersionId != job.AssetVersionId)
                    await AssetRenderQueue.EnqueueAsync(job.AssetId, current.assetVersionId, job.AssetType, job.RenderKind);
            }

            await CompleteAsync(database, entry.Id, job);
        }
        catch (StaleAssetRenderException ex)
        {
            await AssetRenderQueue.EnqueueAsync(job.AssetId, ex.CurrentVersionId, job.AssetType, job.RenderKind);
            await CompleteAsync(database, entry.Id, job);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Asset render job failed for asset {AssetId}, version {AssetVersionId}, attempt {Attempt}",
                job.AssetId, job.AssetVersionId, job.Attempt);
            if (job.Attempt < RetryDelays.Length)
            {
                await Task.Delay(RetryDelays[job.Attempt], cancellationToken);
                await database.KeyDeleteAsync(AssetRenderQueue.DedupKey(job));
                await AssetRenderQueue.EnqueueAsync(job.AssetId, job.AssetVersionId, job.AssetType, job.RenderKind, job.Attempt + 1);
            }
            else
            {
                await database.StreamAddAsync(AssetRenderQueue.DeadLetterStream,
                [
                    new("assetId", job.AssetId), new("versionId", job.AssetVersionId),
                    new("assetType", (int)job.AssetType), new("renderKind", job.RenderKind),
                    new("failedAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), new("attempt", job.Attempt),
                ], maxLength: 10000, useApproximateMaxLength: true);
                await database.KeyExpireAsync(AssetRenderQueue.DeadLetterStream, TimeSpan.FromDays(7));
                await database.KeyDeleteAsync(AssetRenderQueue.DedupKey(job));
            }
            await database.StreamAcknowledgeAsync(AssetRenderQueue.Stream, AssetRenderQueue.Group, entry.Id);
            await database.StreamDeleteAsync(AssetRenderQueue.Stream, [entry.Id]);
        }
    }

    private static async Task CompleteAsync(IDatabase database, RedisValue entryId, AssetRenderJob job)
    {
        var transaction = database.CreateTransaction();
        _ = transaction.StreamAcknowledgeAsync(AssetRenderQueue.Stream, AssetRenderQueue.Group, entryId);
        _ = transaction.StreamDeleteAsync(AssetRenderQueue.Stream, [entryId]);
        _ = transaction.KeyDeleteAsync(AssetRenderQueue.DedupKey(job));
        await transaction.ExecuteAsync();
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var assets = ServiceProvider.GetOrCreate<AssetsService>();
                var missing = await assets.db.QueryAsync<(long assetId, long assetVersionId, AssetType assetType)>("""
                    SELECT asset.id AS assetId, asset_version.id AS assetVersionId, asset.asset_type AS assetType
                    FROM asset
                    JOIN LATERAL (SELECT id FROM asset_version WHERE asset_id = asset.id ORDER BY version_number DESC LIMIT 1) asset_version ON TRUE
                    LEFT JOIN asset_thumbnail ON asset_thumbnail.asset_id = asset.id AND asset_thumbnail.asset_version_id = asset_version.id
                    WHERE asset.moderation_status = 0 AND asset_thumbnail.asset_id IS NULL
                      AND asset.asset_type NOT IN (3, 9, 62)
                    ORDER BY asset.id LIMIT 100
                    """);
                foreach (var item in missing)
                    await AssetRenderQueue.EnqueueAsync(item.assetId, item.assetVersionId, item.assetType);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(ex, "Asset thumbnail reconciliation failed");
            }
            await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
        }
    }
}

internal sealed record AssetRenderJob(long AssetId, long AssetVersionId, AssetType AssetType, string RenderKind, int Attempt)
{
    public static AssetRenderJob Parse(StreamEntry entry)
    {
        var fields = entry.Values.ToDictionary(value => value.Name.ToString(), value => value.Value.ToString(), StringComparer.Ordinal);
        return new AssetRenderJob(
            long.Parse(fields["assetId"], CultureInfo.InvariantCulture),
            long.Parse(fields["versionId"], CultureInfo.InvariantCulture),
            (AssetType)int.Parse(fields["assetType"], CultureInfo.InvariantCulture),
            fields["renderKind"],
            int.Parse(fields["attempt"], CultureInfo.InvariantCulture));
    }
}

public sealed class StaleAssetRenderException(long expectedVersionId, long currentVersionId)
    : Exception($"Asset version changed from {expectedVersionId} to {currentVersionId} while rendering")
{
    public long ExpectedVersionId { get; } = expectedVersionId;
    public long CurrentVersionId { get; } = currentVersionId;
}
