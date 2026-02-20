namespace Roblox.Models.AbuseReport;

public enum AbuseReportReason
{
    None = 1,
    BadChatMessagesInGame,
    BadPrivateMessage,
    BadGame,
    Bullying = 5,
    RacismHomophobiaOrDiscrimination = 6,
    Dating = 7,
    Underage = 8,
    BadAsset = 9,
    InappropriateContent = 10,
}

public enum AbuseReportStatus
{
    Pending = 1,
    Valid,
    InvalidGood,
    InvalidBad,
}