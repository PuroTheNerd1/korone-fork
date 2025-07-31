namespace Roblox.Services;

public class PlayerSecurityService :  ServiceBase, IService 
{
#region Tickets
    private string GetPlayerTicketKey(long userId)
    {
        return "PlayerTicket:" + userId;
    }
    // 15 minutes is the default timeout for player tickets
    public async Task CreatePlayerTicket(long userId, Guid jobId)
    {
        await redis.StringSetAsync(GetPlayerTicketKey(userId), jobId.ToString(), TimeSpan.FromMinutes(15));
    }
    private async Task<string?> GetPlayerTicket(long userId)
    {
        return await redis.StringGetAsync(GetPlayerTicketKey(userId));
    }
    private async Task DeletePlayerTicket(long userId)
    {
        await redis.KeyDeleteAsync(GetPlayerTicketKey(userId));
    }
    public async Task<bool> IsPlayerTicketValid(long userId, Guid jobId)
    {
        string? ticket = await GetPlayerTicket(userId);
        if (ticket == null)
        {
            return false;
        }
        var isTicketValid = ticket == jobId.ToString();
        // Remove the ticket so it cannot be reused
        await DeletePlayerTicket(userId);
        return isTicketValid;
    }
    #endregion

    public bool IsThreadSafe()
    {
        return true;
    }

    public bool IsReusable()
    {
        return true;
    }
}
