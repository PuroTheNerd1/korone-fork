using InfluxDB.Client.Core.Exceptions;
using MVC = Microsoft.AspNetCore.Mvc;

namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class GambleBot: ControllerBase
    {
        [BotAuthorization]
        [HttpGetBypass("bot/coinflip")]
        public async Task<dynamic> CoinFlip(string discordid, int amount)
        {
            Random random = new Random();
            Dto.Users.UserInfo userInfo;

            if (amount > 1000 || amount < 1)
            {
                return new { error = "Invalid amount" };
            }

            try 
            {
                userInfo = await services.users.GetUserByDiscordId(discordid);
            }
            catch (Exception e)
            {
                throw new BadRequestException("User not found");
            }

            var balance = await services.economy.GetUserBalance(userInfo.userId);
            long newBalance = balance.robux;
            //balance check
            if (newBalance < amount)
            {
                throw new BadRequestException("Insufficient balance");
            }
            //decrement currency here
            await services.economy.DecrementCurrency(Models.Assets.CreatorType.User, userInfo.userId, Models.Economy.CurrencyType.Robux, amount);
            //calculate if win
            int chance = random.Next(0, 101);
            bool isWinner = chance <= 40;
            int finalRobux = amount * 2;
            if (isWinner)
            {
                await services.economy.IncrementCurrency(Models.Assets.CreatorType.User, userInfo.userId, Models.Economy.CurrencyType.Robux, finalRobux);
                return new
                {
                    message = "You won!",
                    newBalance = newBalance + finalRobux
                };
            }
            else
            {
                return new
                {
                    message = "You lost!",
                    newBalance = newBalance + finalRobux
                };
            }
        }
    }
}