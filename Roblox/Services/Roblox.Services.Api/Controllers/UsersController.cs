using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Roblox.Web.Infrastructure.Controllers;
using Roblox.Web.Infrastructure.Metadata;

namespace Roblox.Services.Api.Controllers;

[ApiController]
[Route("/")]
public class UsersController : RobloxControllerBase
{
    private readonly IReadOnlySet<long> _ownerUserIds;

    public UsersController(IConfiguration configuration)
    {
        _ownerUserIds = configuration
            .GetSection("OwnerUserId")
            .Get<List<long>>()?
            .ToHashSet() ?? new HashSet<long>();
    }

    [RequireRobloxSession]
    [HttpGet("users/account-info")]
    [HttpPost("users/account-info")]
    public async Task<dynamic> AccountInfo()
    {
        var userBalance = await services.economy.GetUserBalance(safeUserSession.userId);
        return new
        {
            UserId = safeUserSession.userId,
            Username = safeUserSession.username,
            DisplayName = safeUserSession.username,
            HasPasswordSet = true,
            Email = "korone@pekora.zip",
            MembershipType = 3,
            RobuxBalance = userBalance.robux,
            AgeBracket = 0,
            Roles = Array.Empty<string>(),
            EmailNotificationEnabled = false,
            PasswordNotifcationEnabled = false,
        };
    }

    [AllowRobloxAnonymous]
    [HttpGet("users/{userId:long}/canmanage/{placeId:long}")]
    public async Task<dynamic> CanManage(long userId, long placeId)
    {
        var canManage = IsOwner(userId) || await services.assets.CanUserModifyItem(placeId, userId);
        return new
        {
            Success = canManage,
            CanManage = canManage,
        };
    }

    [AllowRobloxAnonymous]
    [HttpGet("game/players/{userId:long}")]
    public dynamic GetGamePlayer(long userId)
    {
        return new
        {
            ChatFilter = IsOwner(userId) ? "whitelist" : "blacklist",
        };
    }

    private bool IsOwner(long userId)
    {
        return _ownerUserIds.Contains(userId);
    }
}
