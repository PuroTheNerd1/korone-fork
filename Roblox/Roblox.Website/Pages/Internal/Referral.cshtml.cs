using InfluxDB.Client.Api.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Dto.Users;
using Roblox.Services.App.FeatureFlags;
using Roblox.Services.Exceptions;

namespace Roblox.Website.Pages.Internal;

public class Referral : RobloxPageModel
{
    public Dto.Users.Referral? referral { get; set; }
    public long? count { get; set; }
    [BindProperty]
    public string? action { get; set; }
    public string? errorMessage { get; set; }
    public bool canCreateReferral => referral == null && userSession != null && userSession.userId != 0;
    private void FeatureCheck()
    {
        try
        {
            FeatureFlags.FeatureCheck(FeatureFlag.CreateInvitesEnabled, FeatureFlag.InvitesEnabled);
        }
        catch (RobloxException)
        {
            errorMessage = "Referrals are disabled at this time. Try again later.";
        }
    }
    private async Task OnPageLoad()
    {
        if (userSession == null)
            return;
        
        var userInfo = await services.users.GetUserById(userSession.userId);
        referral = await services.users.GetUserReferral(userSession.userId);
        count = await services.users.GetReferralCodeUseCount(userSession.userId);
        if (userInfo.created > DateTime.UtcNow.Subtract(TimeSpan.FromDays(1)))
        {
            errorMessage = "You cannot create an invite since your account is too new. Try again tomorrow.";
        }
    }
    
    public async Task OnGet()
    {
        FeatureCheck();
        if (errorMessage is null)
            await OnPageLoad();
    }

    public async Task<dynamic> OnPost()
    {
        FeatureCheck();
        if (errorMessage is null)
            await OnPageLoad();
        else
            return new PageResult();
        
        if (action == "CreateUserReferral")
        {
            if (userSession == null)
                return new PageResult();
            
            try
            {
                // messy!
                var referral = await services.users.GetUserReferral(userSession.userId);
                if (referral != null)
                {
                    referral = await services.users.GetUserReferral(userSession.userId);
                    return new PageResult();
                }

                // Create user refferal with username
                await services.users.CreateReferralCode(userSession.userId, userSession.username);
                // Reload sent invites
                referral = await services.users.GetUserReferral(userSession.userId);
                return new RedirectResult("/internal/referral?suc=true");
            }
            catch (RobloxException e)
            {
                errorMessage = e.errorMessage;
                return new PageResult();
            }
        }
        return new PageResult();
    }
}