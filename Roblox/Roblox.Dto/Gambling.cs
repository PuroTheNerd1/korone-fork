namespace Roblox.Dto.Gambling;
public enum GamblingStatus
{
    Won = 0,
    Lost,
    UserNotFound,
    InsufficientBalance,
    InvalidAmount,
    UnknownError,
}

public class GamblingResponse
{
    public string message { get; set; }
    public string? submessage { get; set; }
    public GamblingStatus status { get; set; }
}
