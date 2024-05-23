using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Dto.Users;
using Roblox.Models.Users;
using System.Threading.Tasks;

namespace Roblox.Website.Pages.Internal
{
    public class TixExchange : RobloxPageModel
    {
        [BindProperty]
        public string? successMessage { get; set; }
        [BindProperty]
        public string? errorMessage { get; set; }
        [BindProperty]
        public long tix { get; set; }

        public async Task OnGet()
        {
            if (userSession == null)
            {
                return;
            }
        }

        public async Task OnPost()
        {
            if (userSession == null)
            {
                return;
            }

            if (tix <= 0)
            {
                errorMessage = "Invalid amount of tix.";
                return;
            }

            int conversionRate = 10;
            decimal roughRobux = tix / conversionRate;
            long finalRobux = (long)Math.Round(roughRobux, 0);

            try
            {
                var balance = await services.economy.GetUserBalance(userSession.userId);
                long newBalance = balance.tickets;

                if (newBalance < tix)
                {
                    errorMessage = "Insufficient tix balance.";
                    return;
                }

                await services.economy.DecrementCurrency(userSession.userId, Models.Economy.CurrencyType.Tickets, tix);
                await services.economy.IncrementCurrency(userSession.userId, Models.Economy.CurrencyType.Robux, finalRobux);

                successMessage = $"You have received {finalRobux} R$ from {tix} Tix.";
                return;
            }
            catch (Exception e)
            {
                errorMessage = "Failed to convert tix to robux.";
                return;
            }
        }
    }
}
