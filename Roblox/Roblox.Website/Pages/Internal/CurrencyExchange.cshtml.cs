using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Roblox.Website.Controllers;
using System.Threading.Tasks;

namespace Roblox.Website.Pages.Internal
{
    public class CurrencyExchange : RobloxPageModel
    {
        [BindProperty]
        public string? successMessage { get; set; }
        [BindProperty]
        public string? errorMessage { get; set; }
        [BindProperty]
        public long amount { get; set; }
        [BindProperty]
        public string currencyToSell { get; set; } = "Robux";

        public long robuxBalance { get; set; }
        public long tixBalance { get; set; }

        public async Task OnGet()
        {
            if (userSession == null)
            {
                return;
            }

            var balance = await services.economy.GetUserBalance(userSession.userId);
            robuxBalance = balance.robux;
            tixBalance = balance.tickets;
        }

        public async Task OnPost()
        {
            if (userSession == null)
            {
                return;
            }

            var balance = await services.economy.GetUserBalance(userSession.userId);
            robuxBalance = balance.robux;
            tixBalance = balance.tickets;

            if (amount < 10)
            {
                errorMessage = "The minimum amount you can exchange is 10.";
                return;
            }

            int conversionRate = 10;

            try
            {
                if (currencyToSell == "Tix")
                {
                    if (balance.tickets < amount)
                    {
                        errorMessage = "Insufficient tix balance.";
                        return;
                    }

                    long finalRobux = (long)Math.Round((decimal)amount / conversionRate, 0);
                    await services.economy.ChargeForConversion(userSession.userId, amount, finalRobux, Roblox.Models.Economy.ConversionType.TixToRobux);
                    successMessage = $"You have received {finalRobux} R$ from {amount} Tix.";
                }
                else
                {
                    if (balance.robux < amount)
                    {
                        errorMessage = "Insufficient robux balance.";
                        return;
                    }

                    long finalTix = (long)Math.Round((decimal)amount * conversionRate, 0);
                    await services.economy.ChargeForConversion(userSession.userId, amount, finalTix, Roblox.Models.Economy.ConversionType.RobuxToTix);
                    successMessage = $"You have received {finalTix} Tix from {amount} R$.";
                }
            }
            catch (Exception)
            {
                errorMessage = "Failed to exchange currency.";
            }
        }
    }
}
