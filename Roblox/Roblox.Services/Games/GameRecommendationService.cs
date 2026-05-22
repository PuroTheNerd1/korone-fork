using System.Text;
using System.Text.Json;
using Dapper;
using Roblox.Models.Assets;
using Roblox.Services.AI;
using StackExchange.Redis;

namespace Roblox.Services.Games;

public class GameRecommendationService : ServiceBase, IService
{
    private const int CandidatePoolPerSource = 50;
    private const int FinalRecommendationCount = 30;
    private const int LlmRerankSize = 50;
    private const int MaxUsersPerCronCycle = 1000;
    private const int MaxProfileEntryLength = 280;
    private const int MaxCandidateNameLength = 100;
    private const int MaxCandidateTopicLength = 280;
    private static readonly TimeSpan CooldownTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan PeriodicInterval = TimeSpan.FromHours(12);
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(13);

    private static string CacheKey(long userId) => $"rec:list:{userId}";
    private static string HasKey(long userId) => $"rec:has:{userId}";

    private sealed record CandidateRow(
        long AssetId,
        string Name,
        string? Topic,
        int PlayerCount,
        long VisitCount,
        long UpVotes,
        long DownVotes,
        long FavoriteCount,
        long FriendPlays,
        long SimilarUserPlays);

    public async Task<bool> HasRecommendationsAsync(long userId)
    {
        var cached = await redis.StringGetAsync(HasKey(userId));
        if (cached == "1") return true;
        if (cached == "0") return false;

        var exists = await db.QuerySingleOrDefaultAsync<long?>(
            "SELECT 1 FROM user_game_recommendation WHERE user_id = :id LIMIT 1",
            new { id = userId });
        var has = exists.HasValue;
        await redis.StringSetAsync(HasKey(userId), has ? "1" : "0", TimeSpan.FromMinutes(15));
        return has;
    }

    public async Task<IEnumerable<long>> GetTopAsync(long userId, int limit)
    {
        var cached = await redis.StringGetAsync(CacheKey(userId));
        if (!string.IsNullOrEmpty(cached))
        {
            var parsed = cached
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => long.TryParse(s, out var v) ? v : 0L)
                .Where(v => v > 0)
                .Take(limit)
                .ToList();
            if (parsed.Count > 0) return parsed;
        }

        var rows = (await db.QueryAsync<long>(
            "SELECT asset_id FROM user_game_recommendation WHERE user_id = :id ORDER BY position ASC LIMIT :limit",
            new { id = userId, limit })).ToList();

        if (rows.Count > 0)
        {
            await redis.StringSetAsync(CacheKey(userId), string.Join(',', rows), CacheTtl);
        }
        return rows;
    }

    private async Task InvalidateCacheAsync(long userId)
    {
        try { await redis.KeyDeleteAsync(CacheKey(userId)); } catch { }
        try { await redis.KeyDeleteAsync(HasKey(userId)); } catch { }
    }

    private static string Truncate(string input, int max)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var s = input.Replace('\0', ' ');
        return s.Length > max ? s.Substring(0, max) : s;
    }

    public async Task<bool> TryRecomputeWithCooldownAsync(long userId)
    {
        var key = $"rec:cooldown:{userId}";
        var acquired = await Roblox.Cache.DistributedCache.redis.GetDatabase(0)
            .StringSetAsync(key, "1", CooldownTtl, when: When.NotExists);
        if (!acquired) return false;

        await RecomputeAsync(userId);
        return true;
    }

    public async Task RecomputeAsync(long userId)
    {
        var modOk = (int)ModerationStatus.ReviewApproved;

        var poolIds = new HashSet<long>();

        async Task AddFromQuery(string sql, object? p = null)
        {
            var rows = await db.QueryAsync<long>(sql, p);
            foreach (var id in rows) poolIds.Add(id);
        }

        await AddFromQuery(@"
            SELECT a.id FROM asset a
            INNER JOIN universe_asset ua ON ua.asset_id = a.id
            INNER JOIN universe u ON u.id = ua.universe_id AND u.root_asset_id = a.id
            LEFT JOIN (
                SELECT asset_id, COUNT(*) AS pc FROM asset_server_player GROUP BY asset_id
            ) p ON p.asset_id = a.id
            WHERE a.asset_type = 9 AND a.moderation_status = :mod
            ORDER BY COALESCE(p.pc, 0) DESC, a.id DESC
            LIMIT :lim
        ", new { mod = modOk, lim = CandidatePoolPerSource });

        await AddFromQuery(@"
            SELECT a.id FROM asset a
            INNER JOIN universe_asset ua ON ua.asset_id = a.id
            INNER JOIN universe u ON u.id = ua.universe_id AND u.root_asset_id = a.id
            INNER JOIN asset_place ap ON ap.asset_id = a.id
            WHERE a.asset_type = 9 AND a.moderation_status = :mod
            ORDER BY ap.visit_count DESC
            LIMIT :lim
        ", new { mod = modOk, lim = CandidatePoolPerSource });

        await AddFromQuery(@"
            SELECT a.id FROM asset a
            INNER JOIN universe_asset ua ON ua.asset_id = a.id
            INNER JOIN universe u ON u.id = ua.universe_id AND u.root_asset_id = a.id
            LEFT JOIN (
                SELECT asset_id, COUNT(*) FILTER (WHERE type = 1) AS up,
                                  COUNT(*) FILTER (WHERE type = 2) AS dn
                FROM asset_vote GROUP BY asset_id
            ) v ON v.asset_id = a.id
            LEFT JOIN (
                SELECT asset_id, COUNT(*) AS fav FROM asset_favorite GROUP BY asset_id
            ) f ON f.asset_id = a.id
            WHERE a.asset_type = 9 AND a.moderation_status = :mod
            ORDER BY (COALESCE(v.up,0) - COALESCE(v.dn,0) + COALESCE(f.fav,0)) DESC
            LIMIT :lim
        ", new { mod = modOk, lim = CandidatePoolPerSource });

        await AddFromQuery(@"
            SELECT DISTINCT ph.asset_id FROM asset_play_history ph
            INNER JOIN asset a ON a.id = ph.asset_id
            WHERE ph.user_id = :uid AND a.moderation_status = :mod AND a.asset_type = 9
            ORDER BY ph.asset_id DESC
            LIMIT 20
        ", new { uid = userId, mod = modOk });

        await AddFromQuery(@"
            SELECT DISTINCT ph.asset_id
            FROM user_friend uf
            INNER JOIN asset_play_history ph ON ph.user_id = uf.user_id_two
            INNER JOIN asset a ON a.id = ph.asset_id
            WHERE uf.user_id_one = :uid
              AND ph.created_at > NOW() - INTERVAL '30 days'
              AND a.moderation_status = :mod AND a.asset_type = 9
            LIMIT 20
        ", new { uid = userId, mod = modOk });

        await AddFromQuery(@"
            WITH my_plays AS (
                SELECT DISTINCT asset_id FROM asset_play_history WHERE user_id = :uid
            ),
            similar AS (
                SELECT ph.user_id
                FROM asset_play_history ph
                INNER JOIN my_plays mp ON mp.asset_id = ph.asset_id
                WHERE ph.user_id <> :uid
                GROUP BY ph.user_id
                HAVING COUNT(DISTINCT ph.asset_id) >= 2
                LIMIT 25
            )
            SELECT DISTINCT ph.asset_id
            FROM asset_play_history ph
            INNER JOIN similar s ON s.user_id = ph.user_id
            INNER JOIN asset a ON a.id = ph.asset_id
            WHERE ph.created_at > NOW() - INTERVAL '30 days'
              AND a.moderation_status = :mod AND a.asset_type = 9
            ORDER BY ph.asset_id DESC
            LIMIT 30
        ", new { uid = userId, mod = modOk });

        var recentlyPlayed = (await db.QueryAsync<long>(@"
            SELECT DISTINCT asset_id FROM asset_play_history
            WHERE user_id = :uid AND created_at > NOW() - INTERVAL '24 hours'
        ", new { uid = userId })).ToHashSet();

        foreach (var rid in recentlyPlayed) poolIds.Remove(rid);

        if (poolIds.Count == 0)
        {
            await db.ExecuteAsync("DELETE FROM user_game_recommendation WHERE user_id = :uid", new { uid = userId });
            return;
        }

        var idsArr = poolIds.ToArray();
        var candidates = (await db.QueryAsync<CandidateRow>(@"
            SELECT
                a.id AS AssetId,
                a.name AS Name,
                u.topic AS Topic,
                COALESCE(p.pc, 0) AS PlayerCount,
                COALESCE(ap.visit_count, 0) AS VisitCount,
                COALESCE(v.up, 0) AS UpVotes,
                COALESCE(v.dn, 0) AS DownVotes,
                COALESCE(f.fav, 0) AS FavoriteCount,
                COALESCE(fp.cnt, 0) AS FriendPlays,
                COALESCE(sp.cnt, 0) AS SimilarUserPlays
            FROM asset a
            INNER JOIN universe_asset ua ON ua.asset_id = a.id
            INNER JOIN universe u ON u.id = ua.universe_id AND u.root_asset_id = a.id
            LEFT JOIN asset_place ap ON ap.asset_id = a.id
            LEFT JOIN (SELECT asset_id, COUNT(*) AS pc FROM asset_server_player GROUP BY asset_id) p ON p.asset_id = a.id
            LEFT JOIN (
                SELECT asset_id, COUNT(*) FILTER (WHERE type = 1) AS up, COUNT(*) FILTER (WHERE type = 2) AS dn
                FROM asset_vote GROUP BY asset_id
            ) v ON v.asset_id = a.id
            LEFT JOIN (SELECT asset_id, COUNT(*) AS fav FROM asset_favorite GROUP BY asset_id) f ON f.asset_id = a.id
            LEFT JOIN (
                SELECT ph.asset_id, COUNT(*) AS cnt
                FROM asset_play_history ph
                INNER JOIN user_friend uf ON uf.user_id_two = ph.user_id
                WHERE uf.user_id_one = :uid AND ph.created_at > NOW() - INTERVAL '30 days'
                GROUP BY ph.asset_id
            ) fp ON fp.asset_id = a.id
            LEFT JOIN (
                WITH my_plays AS (
                    SELECT DISTINCT asset_id FROM asset_play_history WHERE user_id = :uid
                ),
                similar AS (
                    SELECT ph.user_id FROM asset_play_history ph
                    INNER JOIN my_plays mp ON mp.asset_id = ph.asset_id
                    WHERE ph.user_id <> :uid
                    GROUP BY ph.user_id HAVING COUNT(DISTINCT ph.asset_id) >= 2
                )
                SELECT ph.asset_id, COUNT(*) AS cnt
                FROM asset_play_history ph
                INNER JOIN similar s ON s.user_id = ph.user_id
                WHERE ph.created_at > NOW() - INTERVAL '30 days'
                GROUP BY ph.asset_id
            ) sp ON sp.asset_id = a.id
            WHERE a.id = ANY(:ids)
        ", new { uid = userId, ids = idsArr })).ToList();

        if (candidates.Count == 0)
        {
            await db.ExecuteAsync("DELETE FROM user_game_recommendation WHERE user_id = :uid", new { uid = userId });
            return;
        }

        double Log1p(double v) => v <= 0 ? 0.0 : Math.Log(1 + v);

        var baseScores = candidates.ToDictionary(c => c.AssetId, c =>
            0.30 * Log1p(c.PlayerCount * 10) +
            0.20 * Log1p(c.VisitCount) +
            0.20 * Log1p(Math.Max(0, c.UpVotes - c.DownVotes) + c.FavoriteCount) +
            0.15 * Log1p(c.FriendPlays) +
            0.10 * Log1p(c.SimilarUserPlays));

        var rerankSubset = candidates
            .OrderByDescending(c => baseScores[c.AssetId])
            .Take(LlmRerankSize)
            .ToList();

        var profile = (await db.QueryAsync<string>(@"
            SELECT DISTINCT u.topic
            FROM (
                SELECT ph.asset_id FROM asset_play_history ph
                WHERE ph.user_id = :uid
                ORDER BY ph.id DESC
                LIMIT 50
            ) recent
            INNER JOIN universe_asset ua ON ua.asset_id = recent.asset_id
            INNER JOIN universe u ON u.id = ua.universe_id
            WHERE u.topic IS NOT NULL AND length(u.topic) > 0
            LIMIT 15
        ", new { uid = userId })).ToList();

        var llmRanks = new Dictionary<long, int>();
        if (profile.Count > 0 && !string.IsNullOrEmpty(Roblox.Configuration.OpenRouterApiKey))
        {
            using var ai = ServiceProvider.GetOrCreate<OpenRouterService>(this);
            var safeProfile = string.Join(" | ", profile
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => Truncate(p, MaxProfileEntryLength)));
            var systemPrompt = AiPrompts.RecommendSystem.Replace("{PROFILE}", safeProfile);
            var payload = JsonSerializer.Serialize(rerankSubset.Select(c => new
            {
                id = c.AssetId,
                name = Truncate(c.Name ?? "", MaxCandidateNameLength),
                topic = Truncate(c.Topic ?? "", MaxCandidateTopicLength)
            }));
            var response = await ai.ChatAsync(systemPrompt, payload, online: false, maxTokens: 600);
            if (!string.IsNullOrWhiteSpace(response))
            {
                var idx = 0;
                foreach (var token in response.Split(new[] { ',', '\n', '\r', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var cleaned = new string(token.Where(char.IsDigit).ToArray());
                    if (cleaned.Length > 20) continue;
                    if (long.TryParse(cleaned, out var id) && baseScores.ContainsKey(id) && !llmRanks.ContainsKey(id))
                    {
                        llmRanks[id] = idx++;
                    }
                }
            }
        }

        var finalScores = candidates.ToDictionary(c => c.AssetId, c =>
        {
            var score = baseScores[c.AssetId];
            if (llmRanks.TryGetValue(c.AssetId, out var rank))
                score += (LlmRerankSize - rank) * 0.05;
            return score;
        });

        var top = candidates
            .OrderByDescending(c => finalScores[c.AssetId])
            .Take(FinalRecommendationCount)
            .ToList();

        await InTransaction(async _ =>
        {
            await db.ExecuteAsync("DELETE FROM user_game_recommendation WHERE user_id = :uid", new { uid = userId });
            for (var i = 0; i < top.Count; i++)
            {
                var row = top[i];
                await db.ExecuteAsync(@"
                    INSERT INTO user_game_recommendation (user_id, asset_id, score, position)
                    VALUES (:uid, :aid, :score, :pos)
                    ON CONFLICT (user_id, asset_id) DO UPDATE SET score = EXCLUDED.score, position = EXCLUDED.position
                ", new { uid = userId, aid = row.AssetId, score = finalScores[row.AssetId], pos = i });
            }
            return 0;
        });

        await InvalidateCacheAsync(userId);
        if (top.Count > 0)
        {
            await redis.StringSetAsync(CacheKey(userId), string.Join(',', top.Select(c => c.AssetId)), CacheTtl);
            await redis.StringSetAsync(HasKey(userId), "1", TimeSpan.FromHours(13));
        }
        else
        {
            await redis.StringSetAsync(HasKey(userId), "0", TimeSpan.FromMinutes(15));
        }
    }

    public static void StartPeriodicLoop()
    {
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(2));
            while (true)
            {
                try
                {
                    await RunOneCycleAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine("[warn] recommendation cron cycle failed: {0}", e.Message);
                }
                await Task.Delay(PeriodicInterval);
            }
        });
    }

    private static async Task RunOneCycleAsync()
    {
        using var svc = ServiceProvider.GetOrCreate<GameRecommendationService>();
        var activeUserIds = (await svc.db.QueryAsync<long>(@"
            SELECT DISTINCT user_id FROM asset_play_history
            WHERE created_at > NOW() - INTERVAL '7 days'
            ORDER BY user_id DESC
            LIMIT :lim
        ", new { lim = MaxUsersPerCronCycle })).ToList();

        Console.WriteLine("[info] recommendation cron: {0} active users", activeUserIds.Count);
        foreach (var uid in activeUserIds)
        {
            try
            {
                using var inner = ServiceProvider.GetOrCreate<GameRecommendationService>();
                await inner.RecomputeAsync(uid);
            }
            catch (Exception e)
            {
                Console.WriteLine("[warn] recompute failed for {0}: {1}", uid, e.Message);
            }
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }
    }

    public bool IsThreadSafe() => true;
    public bool IsReusable() => false;
}
