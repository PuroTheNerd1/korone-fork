using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Dto.Users;
using Roblox.Models.AbuseReport;
using Roblox.Services;
using ServiceProvider = Roblox.Services.ServiceProvider;

namespace Roblox.Website.Pages.Internal;

public class ReportAbuse : RobloxPageModel
{
    public string? failureMessage { get; set; }
    public string? successMessage { get; set; }

    [BindProperty]
    public AbuseReportReason reportReason { get; set; }
    [BindProperty]
    public string? reportMessage { get; set; }
    public long? reportedAssetId { get; set; }
    public long? reportedUserId { get; set; }

    public void OnGet()
    {
        var id = HttpContext.Request.Query["reportedId"].ToString();
        var type = HttpContext.Request.Query["reportedType"].ToString();
        if (!string.IsNullOrEmpty(id) && long.TryParse(id, out var parsedId))
        {
            if (type == "user")
                reportedUserId = parsedId;
            else
                reportedAssetId = parsedId;
        }
    }

    private readonly Regex _alphaNumericRegex = new("[a-zA-Z]+", RegexOptions.Compiled);

    private static readonly HashSet<AbuseReportReason> allowedReasons = new()
    {
        AbuseReportReason.BadPrivateMessage,
        AbuseReportReason.Bullying,
        AbuseReportReason.RacismHomophobiaOrDiscrimination,
        AbuseReportReason.Dating,
        AbuseReportReason.Underage,
        AbuseReportReason.BadAsset,
        AbuseReportReason.InappropriateContent,
    };

    public async Task OnPost()
    {
        if (userSession == null)
        {
            failureMessage = "Not logged in.";
            return;
        }

        if (!Enum.IsDefined(reportReason) || !allowedReasons.Contains(reportReason))
        {
            failureMessage = "Invalid report reason.";
            return;
        }

        if (string.IsNullOrWhiteSpace(reportMessage))
        {
            failureMessage = "Report message be at least 10 characters. Please try again.";
            return;
        }

        // check that it is at least 10 alpha characters
        var reportLen = string.Join("",
            _alphaNumericRegex.Match(reportMessage).Groups.Values.Select(c => c.Value).ToArray());
        if (reportLen.Length > 10)
        {
            failureMessage = "Report message be at least 10 characters. Please try again.";
            return;
        }

        long? submittedAssetId = null;
        long? submittedUserId = null;
        if (long.TryParse(HttpContext.Request.Form["reportedAssetId"].ToString(), out var parsedAssetId))
            submittedAssetId = parsedAssetId;
        if (long.TryParse(HttpContext.Request.Form["reportedUserId"].ToString(), out var parsedUserId))
            submittedUserId = parsedUserId;

        using var ar = ServiceProvider.GetOrCreate<AbuseReportService>();
        if (!await services.cooldown.TryCooldownCheck($"AbuseReportV1_Cooldown:{userSession.userId}", TimeSpan.FromMinutes(20)))
        {
            failureMessage = "Please wait 20 minutes before sending another report.";
            return;
        }

        await ar.InsertReport(userSession.userId, reportReason, reportMessage, submittedAssetId, submittedUserId);
        successMessage = "Your report has been sent successfully.";

        reportReason = AbuseReportReason.None;
        reportMessage = null;
    }
}
