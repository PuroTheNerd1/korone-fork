using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using Roblox.Libraries.OpenRouterApi;
using Roblox.Logging;
using Roblox.Models.AbuseReport;
using Roblox.Models.Users;

namespace Roblox.Services;

public static class AiReportReviewService
{
    private const int MaxReportMessageLength = 4000;
    private const int MaxChatMessagesLength = 8000;

    private static readonly Lazy<string> _systemPrompt = new(LoadSystemPromptFromAssembly);

    private static readonly HashSet<string> ValidPunishments = new(StringComparer.OrdinalIgnoreCase)
    {
        "warn", "1d", "3d", "1w", "2w", "1m", "1y", "permanent"
    };

    private static string LoadSystemPromptFromAssembly()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(n => n.EndsWith("AiInstructions.md", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static DateTime? PunishmentToExpiration(string punishment)
    {
        return punishment.ToLower() switch
        {
            "1d" => DateTime.UtcNow.AddDays(1),
            "3d" => DateTime.UtcNow.AddDays(3),
            "1w" => DateTime.UtcNow.AddDays(7),
            "2w" => DateTime.UtcNow.AddDays(14),
            "1m" => DateTime.UtcNow.AddDays(30),
            "1y" => DateTime.UtcNow.AddDays(365),
            "warn" or "permanent" => throw new InvalidOperationException("PunishmentToExpiration called with non-expiring punishment"),
            _ => throw new InvalidOperationException("Unrecognized punishment value"),
        };
    }

    private static string SanitizeBanNote(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;
        var stripped = Regex.Replace(value, "<[^>]*>", string.Empty);
        return stripped.Length > 255 ? stripped.Substring(0, 255) : stripped;
    }

    private static string BuildInternalReason(string? rawReason, string reportId)
    {
        var url = $" | /admin-api/api/chat-messages/{reportId}";
        var reason = rawReason ?? string.Empty;
        var maxReasonLength = 414 - url.Length;
        if (reason.Length > maxReasonLength)
            reason = reason.Substring(0, maxReasonLength);
        return reason + url;
    }

    private static HashSet<long> ExtractChatUserIds(string? messagesXml)
    {
        var ids = new HashSet<long>();
        if (string.IsNullOrWhiteSpace(messagesXml))
            return ids;
        foreach (Match m in Regex.Matches(messagesXml, @"userID=""(\d+)"""))
        {
            if (long.TryParse(m.Groups[1].Value, out var uid))
                ids.Add(uid);
        }
        return ids;
    }

    private static async Task ApplyBan(long targetUserId, string punishment, string banNote, string internalReason, long aiUserId)
    {
        var db = Database.connection;

        var existingStatus = await db.QuerySingleOrDefaultAsync<int?>(
            "SELECT status FROM \"user\" WHERE id = :id LIMIT 1",
            new { id = targetUserId });

        if (existingStatus == null || (existingStatus != (int)AccountStatus.Ok && existingStatus != (int)AccountStatus.Suppressed && existingStatus != (int)AccountStatus.MustValidateEmail))
        {
            Writer.Info(LogGroup.AiReportReview, "User {0} not bannable (status={1})", targetUserId, existingStatus);
            return;
        }

        var isPermanent = string.Equals(punishment, "permanent", StringComparison.OrdinalIgnoreCase);
        var expiresAt = isPermanent ? (DateTime?)null : PunishmentToExpiration(punishment);
        var newStatus = isPermanent ? AccountStatus.Deleted : AccountStatus.Suppressed;
        var sanitizedNote = SanitizeBanNote(banNote);

        await db.ExecuteAsync(
            "INSERT INTO user_ban (user_id, reason, author_user_id, expired_at, internal_reason) VALUES (:user_id, :reason, :author, :expires, :internal_reason)",
            new
            {
                user_id = targetUserId,
                reason = sanitizedNote,
                author = aiUserId,
                expires = expiresAt,
                internal_reason = internalReason,
            });

        await db.ExecuteAsync(
            "INSERT INTO moderation_ban (user_id, actor_id, reason, internal_reason, expired_at) VALUES (:user_id, :author, :reason, :internal_reason, :expires)",
            new
            {
                user_id = targetUserId,
                author = aiUserId,
                reason = sanitizedNote,
                internal_reason = internalReason,
                expires = expiresAt,
            });

        await db.ExecuteAsync(
            "UPDATE \"user\" SET status = :status WHERE id = :id",
            new { status = newStatus, id = targetUserId });

        await db.ExecuteAsync(
            "UPDATE user_asset SET price = 0 WHERE price != 0 AND user_id = :user_id",
            new { user_id = targetUserId });

        Writer.Info(LogGroup.AiReportReview, "Banned user {0} with punishment {1}", targetUserId, punishment);
    }

    private static async Task ReviewReport(string reportId, OpenRouterApi client, string systemPrompt)
    {
        var db = Database.connection;

        var report = await db.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT id, user_id, report_reason, report_status, report_message, reported_user_id FROM abuse_report WHERE id = :id AND report_status = :status LIMIT 1",
            new { id = reportId, status = AbuseReportStatus.Pending });

        if (report == null)
            return;

        var chatMessages = await db.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT messages FROM abuse_report_messages WHERE abuse_id = :id LIMIT 1",
            new { id = reportId });

        var allowedUserIds = new HashSet<long>();
        if (report.user_id != null) allowedUserIds.Add((long)report.user_id);
        if (report.reported_user_id != null) allowedUserIds.Add((long)report.reported_user_id);

        string? chatMessagesText = chatMessages?.messages;
        foreach (var uid in ExtractChatUserIds(chatMessagesText))
            allowedUserIds.Add(uid);

        var reportMessage = (string)(report.report_message ?? string.Empty);
        if (reportMessage.Length > MaxReportMessageLength)
            reportMessage = reportMessage.Substring(0, MaxReportMessageLength);

        var userContent = $"<report_id>{report.id}</report_id>\n<reported_user_id>{report.reported_user_id}</reported_user_id>\n<report_reason>{report.report_reason}</report_reason>\n<report_message>{reportMessage}</report_message>";

        if (chatMessagesText != null)
        {
            var trimmedChat = chatMessagesText.Length > MaxChatMessagesLength
                ? chatMessagesText.Substring(0, MaxChatMessagesLength)
                : chatMessagesText;
            userContent += $"\n<chat_messages>{trimmedChat}</chat_messages>";
        }

        var results = await client.ReviewReport(systemPrompt, userContent);
        if (results == null)
        {
            Writer.Info(LogGroup.AiReportReview, "AI returned null for report {0}, skipping", reportId);
            return;
        }

        var aiUserId = Configuration.AiUserId;
        var anyAccepted = false;

        foreach (var result in results)
        {
            if (!result.accept)
                continue;

            if (!allowedUserIds.Contains(result.userId))
            {
                Writer.Info(LogGroup.AiReportReview, "AI returned userId {0} not in report {1} participants, rejecting entry", result.userId, reportId);
                continue;
            }

            if (!ValidPunishments.Contains(result.punishment ?? string.Empty))
            {
                Writer.Info(LogGroup.AiReportReview, "AI returned unknown punishment '{0}' for user {1} in report {2}, skipping entry", result.punishment, result.userId, reportId);
                continue;
            }

            anyAccepted = true;

            if (string.Equals(result.punishment, "warn", StringComparison.OrdinalIgnoreCase))
            {
                Writer.Info(LogGroup.AiReportReview, "Report {0} - user {1} warned, no ban applied", reportId, result.userId);
                continue;
            }

            var internalReason = BuildInternalReason(result.internalReason, reportId);

            try
            {
                await ApplyBan(result.userId, result.punishment, result.banNote, internalReason, aiUserId);
            }
            catch (Exception ex)
            {
                Writer.Info(LogGroup.AiReportReview, "Failed to ban user {0} from report {1}: {2} {3}", result.userId, reportId, ex.GetType().Name, ex.Message);
            }
        }

        var reportStatus = anyAccepted ? AbuseReportStatus.Valid : AbuseReportStatus.InvalidGood;
        await db.ExecuteAsync(
            "UPDATE abuse_report SET report_status = :status, updated_at = now(), author_id = :author WHERE id = :id",
            new { status = reportStatus, author = aiUserId, id = reportId });

        Writer.Info(LogGroup.AiReportReview, "Report {0} finalized as {1}", reportId, reportStatus);
    }

    public static async Task StartReviewLoop()
    {
        var apiKey = Configuration.OpenRouterApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Writer.Info(LogGroup.AiReportReview, "OpenRouterApiKey is not configured, AI review disabled");
            return;
        }

        var client = new OpenRouterApi(apiKey);
        var systemPrompt = _systemPrompt.Value;

        while (true)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60));

                var db = Database.connection;
                var staleReports = await db.QueryAsync<string>(
                    "SELECT id FROM abuse_report WHERE report_status = :status AND created_at <= now() - interval '10 minutes' ORDER BY created_at LIMIT 10",
                    new { status = AbuseReportStatus.Pending });

                foreach (var reportId in staleReports)
                {
                    try
                    {
                        await ReviewReport(reportId, client, systemPrompt);
                    }
                    catch (Exception ex)
                    {
                        Writer.Info(LogGroup.AiReportReview, "Error reviewing report {0}: {1} {2}", reportId, ex.GetType().Name, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Writer.Info(LogGroup.AiReportReview, "Error in review loop: {0} {1}", ex.GetType().Name, ex.Message);
            }
        }
    }
}
