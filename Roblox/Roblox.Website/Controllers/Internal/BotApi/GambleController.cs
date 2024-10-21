using InfluxDB.Client.Core.Exceptions;
using MVC = Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Gambling;
namespace Roblox.Website.Controllers
{


    [MVC.ApiController]
    [MVC.Route("/")]
    public class GambleBot: ControllerBase
    {
        [BotAuthorization]
        [HttpGetBypass("bot/coinflip")]
        public async Task<GamblingResponse> CoinFlip(string discordid, int amount)
        {
            Random random = new Random();
            Dto.Users.UserInfo userInfo;

            if (amount > 250 || amount < 1)
            {
                return new GamblingResponse
                {
                    message = "You have entered an invalid amount, please enter an amount between 1 and 250",
                    status = (int)GamblingStatus.InvalidAmount
                };
            }

            try 
            {
                userInfo = await services.users.GetUserByDiscordId(discordid);
            }
            catch (Exception)
            {
                return new GamblingResponse
                {
                    message = "Your account is not linked, please use the link command to link your account",
                    status = (int)GamblingStatus.UserNotFound
                };
            }

            var balance = await services.economy.GetUserBalance(userInfo.userId);
            long newBalance = balance.robux;
            //balance check
            if (newBalance < amount)
            {
                return new GamblingResponse
                {
                    message = "You do not have enough balance to gamble this amount",
                    status = (int)GamblingStatus.InsufficientBalance
                };
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
                return new GamblingResponse
                {
                    message = "You have flipped heads and won!",
                    submessage = $"\nYou have won **{finalRobux}** R$, your balance is updated to **{newBalance + finalRobux}**",
                    status = (int)GamblingStatus.Won, // Explicitly cast to int
                };
            }
            else
            {
                return new GamblingResponse
                {
                    message = "You have flipped tails and lost",
                    submessage = $"\nYou have lost **{amount}** R$, your balance is updated to **{newBalance - finalRobux}**",
                    status = (int)GamblingStatus.Lost, 
                };
            }
        }
    }
}