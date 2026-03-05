using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Users;
using Roblox.Exceptions;
using Roblox.Models.Users;
using Roblox.Exceptions.Services.Users;
using Roblox.Services.Exceptions;
using Roblox.Models;
using Roblox.Website.Filters;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/users/v1")]
[ApiExplorerSettings(GroupName = "UserV1")]
public class UsersControllerV1 : ControllerBase
{
    public List<CollectibleItemEntry> inventory { get; set; }
    public long totalRap { get; set; }

    [HttpGet("users/authenticated")]
    [SwaggerOperation(
        Tags = new[] { "Authentication", "Users" },
        Summary = "Gets the current authenticated user's session details.",
        Description = "Retrieves details of the currently authenticated user's session, such as their user ID, username, display name, and staff status."
    )]
    public async Task<dynamic> GetMySession()
    {
        if (userSession is null) throw new UnauthorizedException();
        return new
        {
            id = userSession.userId,
            name = userSession.username,
            displayName = userSession.username,
            isStaff = await StaffFilter.IsStaff(userSession.userId)
        };
    }

    [HttpGet("users/authenticated/ban")]
    public async Task<dynamic> GetMyBanData()
    {
        if (userSession is null) throw new UnauthorizedException();
        try
        {
            var ban = await services.users.GetBanData(userSession.userId);
            return new
            {
                reason = ban.reason,
                createdAt = ban.createdAt,
                expiredAt = ban.expiredAt,
                canUnlock = ban.canUnlock,
            };
        }
        catch (RecordNotFoundException)
        {
            var userInfo = await services.users.GetUserById(userSession.userId);
            if (userInfo.accountStatus == AccountStatus.Ok)
                throw new NotFoundException(0, "User is not banned");
            return new
            {
                reason = "Reason was not specified. Please contact us via our contact page.",
                createdAt = DateTime.UtcNow,
                expiredAt = (DateTime?)null,
                canUnlock = false,
            };
        }
    }

    [HttpPost("users/authenticated/ban/unlock")]
    public async Task<dynamic> UnlockMyAccount()
    {
        if (userSession is null) throw new UnauthorizedException();
        UserBanEntry ban;
        try
        {
            ban = await services.users.GetBanData(userSession.userId);
        }
        catch (RecordNotFoundException)
        {
            throw new BadRequestException(0, "No active ban found");
        }
        if (!ban.canUnlock)
            throw new BadRequestException(0, "Ban has not expired yet");
        await services.users.DeleteBan(userSession.userId);
        return new { success = true };
    }

    [HttpPost("users/{username}/details")]
    [HttpGet("users/{username}/details")]
    [SwaggerOperation(
        Tags = new[] { "Users", "Details" },
        Summary = "Retrieves detailed user information by username, including inventory RAP, friend counts, and game visits.",
        Description = "Retrieves detailed information about a user based on their username, including inventory RAP, friend counts, follower counts, and other user stats."
    )]
    [SwaggerResponse(200, "Returns the user details object")]
    [SwaggerResponse(404, "If the user is not found")]
    [SwaggerResponse(400, "If the request is malformed or invalid")]
    public async Task<dynamic> GetUserByUsername(string username)
    {
        var result = (await services.users.MultiGetUsersByUsername(new[] { username })).ToList();
        var info = await services.users.GetUserById(result[0].id);
        var canViewInventory = !info.IsDeleted() && await services.inventory.CanViewInventory(info.userId);

        if (canViewInventory)
        {
            totalRap = await services.inventory.GetInventoryRap(info.userId);
        }
        else
        {
            totalRap = 0;
        }

        return new
        {
            id = info.userId,
            name = info.username,
            displayName = info.username,
            description = info.description,
            created = info.created,
            isBanned = info.IsDeleted(),
            isInventoryPublic = canViewInventory,
            hasVerifiedBadge = info.isVerified,
            totalPlaceVisits = await services.games.GetTotalVisitsFromUser(info.userId),
            friendshipCount = await services.friends.CountFriends(info.userId),
            followingCount = await services.friends.CountFollowings(info.userId),
            followerCount = await services.friends.CountFollowers(info.userId),
            inventoryRap = totalRap
        };
    }

    [HttpPost("users/{userId:long}")]
    [HttpGet("users/{userId:long}")]
    [SwaggerOperation(
        Tags = new[] { "Users", "Details" },
        Summary = "Retrieves detailed user information by user ID.",
        Description = "retrieves detailed information about a user based on their user ID, including inventory RAP, ban status, and other relevant stats."
    )]
    
    [SwaggerResponse(200, "Returns the user details object")]
    [SwaggerResponse(404, "If the user is not found")]
    public async Task<dynamic> GetUserById(long userId)
    {
        var info = await services.users.GetUserById(userId);
        var canViewInventory = !info.IsDeleted() && await services.inventory.CanViewInventory(info.userId);

        if (canViewInventory)
        {
            totalRap = await services.inventory.GetInventoryRap(info.userId);
        }
        else
        {
            totalRap = 0;
        }


        return new
        {
            info.description,
            info.created,
            isBanned = info.IsDeleted(),
            hasVerifiedBadge = info.isVerified,
            isStaff = await StaffFilter.IsStaff(info.userId),
            id = info.userId,
            name = info.username,
            displayName = info.username,
            inventoryRap = totalRap
        };
    }

    [HttpPost("users")]
    [SwaggerOperation(
        Tags = new[] { "Users", "Bulk" },
        Summary = "Bulk retrieves user details by user IDs.",
        Description = "Allows retrieving user details for multiple user IDs in a single request."
    )]

    [ProducesResponseType(typeof(RobloxCollection<MultiGetEntry>), 200)]
    [SwaggerResponse(400, "Invalid IDs")]
    public async Task<RobloxCollection<MultiGetEntry>> MultiGetUsersById([Required, FromBody] MultiGetRequest request)
    {
        var ids = request.userIds.ToList();
        if (ids.Count > 200 || ids.Count < 1)
        {
            throw new BadRequestException(0, "Invalid IDs");
        }

        var result = await services.users.MultiGetUsersById(ids);
        return new RobloxCollection<MultiGetEntry>()
        {
            data = result,
        };
    }

    [HttpPost("usernames/users")]
    [SwaggerOperation(
        Tags = new[] { "Users", "Bulk" },
        Summary = "Bulk retrieves user details by usernames.",
        Description = "Retrieve user details for multiple usernames in a single request."
    )]
    [ProducesResponseType(typeof(RobloxCollection<MultiGetEntry>), 200)]
    [SwaggerResponse(400, "Invalid Usernames")]
    public async Task<RobloxCollection<MultiGetEntry>> MultiGetUsersByUsername([Required, FromBody] MultiGetByNameRequest request)
    {
        var names = request.usernames.ToList();
        if (names.Count > 200 || names.Count < 1)
        {
            throw new BadRequestException(0, "Invalid Usernames");
        }

        var result = await services.users.MultiGetUsersByUsername(request.usernames);
        return new RobloxCollection<MultiGetEntry>()
        {
            data = result,
        };
    }

    [HttpGet("users/{userId:long}/status")]
    [SwaggerOperation(
        Tags = new[] { "Users", "Status" },
        Summary = "Gets the status of a user by user ID.",
        Description = "Retrieve the status of a user based on their user id."
    )]
    [SwaggerResponse(200, "Returns the user status")]
    [SwaggerResponse(404, "If the user is not found")]
    public async Task<dynamic> GetUserStatus([Required] long userId)
    {
        var result = await services.users.GetUserStatus(userId);
        if (string.IsNullOrEmpty(result.status))
        {
            return new
            {
                status = (string?)null,
            };
        }

        return result;
    }

    [HttpPatch("users/{userId:long}/status")]
    [SwaggerOperation(
        Tags = new[] { "Users", "Status" },
        Summary = "Sets the status of a user by user ID.",
        Description = "Allows the status of a user based on their user ID."
    )]
    [SwaggerResponse(200)]
    [SwaggerResponse(400, "Invalid request")]
    public async Task SetUserStatus([Required, FromBody] SetStatusRequest request)
    {
        try
        {
            await services.users.SetUserStatus(safeUserSession.userId, services.filter.FilterText(request.status));
        }
        catch (Exception e) when (e is StatusTooLongException or StatusTooShortException)
        {
            throw new RobloxException(400, 2, "Invalid request");
        }
    }

    [HttpGet("users/{userId:long}/username-history")]
    [SwaggerOperation(
        Tags = new[] { "Users", "Username History" },
        Summary = "Retrieves the previous usernames of a user.",
        Description = "Retrieves the history of usernames associated with a user, including pagination options."
    )]
    [SwaggerResponse(400, "User is invalid or does not exist")]
    [ProducesResponseType(typeof(RobloxCollectionPaginated<Roblox.Website.WebsiteModels.Users.PreviousUsernameEntry>), 200)]
    public async Task<RobloxCollectionPaginated<Roblox.Website.WebsiteModels.Users.PreviousUsernameEntry>> GetPreviousUsernames([Required] long userId, int limit = 100, string? cursor = null)
    {
        var userInfo = await services.users.GetUserById(userId);
        if (userInfo.IsDeleted()) throw new RobloxException(400, 0, "User is invalid or does not exist");
        var entries = (await services.users.GetPreviousUsernames(userId)).Select(c => new WebsiteModels.Users.PreviousUsernameEntry(c.username));
        return new()
        {
            data = entries,
        };
    }
}
