using Microsoft.AspNetCore.Mvc;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/badges/v1")]
public class BadgesControllerV1
{
    // base: https://apidocs.sixteensrc.zip/badges/docs.html#/
    
    // Gets badge information by the badge id.
    [HttpGet("badges/{badgeId:long}")]
    public dynamic GetBadgeDetails(long badgeId)
    {
        return new
        {
            nextPageCursor = (string?) null,
            previousPageCursor = (string?) null,
            data = new List<int>(),
        };
    }
    
    // Updates badge configuration.
    [HttpPatch("badges/{badgeId:long}")]
    public dynamic UpdateBadgeConfig(long badgeId)
    {
        return new
        {
            nextPageCursor = (string?) null,
            previousPageCursor = (string?) null,
            data = new List<int>(),
        };
    }
    
    // Gets badge by their awarding game.
    [HttpGet("universes/{universeId:long}/badges")]
    public dynamic GetUniverseBadges(long universeId)
    {
        return new
        {
            nextPageCursor = (string?) null,
            previousPageCursor = (string?) null,
            data = new List<int>(),
        };
    }
    
    // Gets a list of badges a user has been awarded.
    [HttpGet("users/{userId:long}/badges")]
    public dynamic GetBadges(long userId)
    {
        return new
        {
            nextPageCursor = (string?) null,
            previousPageCursor = (string?) null,
            data = new List<int>(),
        };
    }
    
    // Gets timestamps for when badges were awarded to a user.
    [HttpGet("users/{userId:long}/badges/awarded-dates")]
    public dynamic GetBadgeTimestamps(long userId)
    {
        return new
        {
            nextPageCursor = (string?) null,
            previousPageCursor = (string?) null,
            data = new List<int>(),
        };
    }
    
    // Award a badge to a user.
    [HttpPost("users/{userId:long}/badges/{badgeId:long}/award-badge")]
    public dynamic AwardBadge(long userId, long badgeId)
    {
        return new
        {
            nextPageCursor = (string?) null,
            previousPageCursor = (string?) null,
            data = new List<int>(),
        };
    }
    
    // Removes a badge from a user.
    [HttpDelete("users/{userId:long}/badges/{badgeId:long}")]
    public dynamic RemoveBadgeFromUser(long userId, long badgeId)
    {
        return new
        {
            nextPageCursor = (string?) null,
            previousPageCursor = (string?) null,
            data = new List<int>(),
        };
    }
    
    // Removes a badge from the authenticated user.
    [HttpDelete("users/badges/{badgeId:long}")]
    public dynamic RemoveBadgeFromSelf(long badgeId)
    {
        return new
        {
            nextPageCursor = (string?) null,
            previousPageCursor = (string?) null,
            data = new List<int>(),
        };
    }
}