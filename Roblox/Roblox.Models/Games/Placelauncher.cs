namespace Roblox.Models.Games;
public class PlaceLaunchRequest
{
    public string request { get; set; }
    public long placeId { get; set; }
    public string? gameId { get; set; } 
    public bool isPartyLeader { get; set; }
    public bool isTeleport { get; set; }
    public string? accessCode { get; set; }
    public string? linkCode { get; set; }
    public string? privateGameMode { get; set; }
    public string? username { get; set; } = null;
    public long? userId { get; set; } = null;
    public bool? special { get; set; } = false;
}
