using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Users;
using Roblox.Services.Exceptions;
using Roblox.Website.Filters;
using MVC = Microsoft.AspNetCore.Mvc;

namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class CustomerSupport : ControllerBase
    {
        [BotAuthorization]
        [HttpGet("bot/resetpassword")]
        public async Task<dynamic> ResetPassword(string discordId, long userId)
        {
            UserInfo userDiscordInfo = null;
            // Why are we handling this?
            // Because GetUserById/GetUserByDiscordId will throw the RecordNotFound exception and then the message will be "NotFound" and that will be in the embed which we don't want
            try
            {
                userDiscordInfo = await services.users.GetUserByDiscordId(discordId);
            }
            // TODO: Make DiscordNotLinkedException
            catch (RecordNotFoundException)
            {
                return new
                {
                    success = false,
                    message = "Your account is not linked, please use the /linkaccount command to link your account",
                };
            }

            // First we check if the user who ran the command if he is a owner if they are not, then throw exception
            if (!StaffFilter.IsOwner(userDiscordInfo.userId))
                throw new RobloxException(403, 1, "You are not allowed to reset passwords");
            // This is a extra security check, let's check if the user who we are trying to reset is a staff member if they are then throw a exception
            if (await StaffFilter.IsStaff(userId))
                throw new RobloxException(403, 1, "You are not allowed to reset the password of this user");
            // Example: 19bcbfac216d46cbaeb826125d1bae42
            string randomlyGeneratedPassword = (Guid.NewGuid().ToString().Replace("-", "") + Guid.NewGuid().ToString().Replace("-", "")).Substring(0, 32);
            UserInfo userInfo = await services.users.GetUserById(userId);
            await services.users.UpdatePassword(userId, randomlyGeneratedPassword);
            return new
            {
                success = true,
                password = randomlyGeneratedPassword,
                message = $"The password has been successfully reset of **{userInfo.username}**.\nThe password has been sent in your DM's"
            };
        }
    }
}
