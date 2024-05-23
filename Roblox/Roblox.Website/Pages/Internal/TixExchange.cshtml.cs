using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Dto.Users;
using Roblox.Models.Users;

namespace Roblox.Website.Pages.Internal;

public class TixExchange : RobloxPageModel
{
    public string? successMessage { get; set; }
    public string? errorMessage { get; set; }
    public long tix { get; set; }
    public async Task OnGet()
    {
        if (userSession == null)
            return;
    }


    public async Task OnPost()
    {
        if (tix <= 0)
        {
            errorMessage = "Invalid amount of tix";
            return;
        }

        int conversionRate = 10;
        decimal roughRobux = tix / conversionRate;
        long finalRobux = (long)Math.Round(roughRobux, 0);


        long newBalance = (await services.economy.GetUserBalance(userSession.userId)).tickets;

        if (newBalance < tix)
        {
            errorMessage = "Insufficient tix balance.";
            return;
        }

        try
        {
            await services.economy.DecrementCurrency(userSession.userId, Models.Economy.CurrencyType.Tickets, tix);
            await services.economy.IncrementCurrency(userSession.userId, Models.Economy.CurrencyType.Robux, finalRobux);
        }
        catch(Exception e)
        {
            errorMessage = "Failed to convert tix to robux";
            return;
        }
        successMessage = "You have received" + finalRobux + "R$ from" + tix + "Tix";
    }
}