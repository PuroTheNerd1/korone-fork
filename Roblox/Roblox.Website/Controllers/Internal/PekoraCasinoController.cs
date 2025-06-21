using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Roblox.EconomyChat;
using Roblox.EconomyChat.Models;
using Roblox.Exceptions;
using Roblox.Models.Economy;
using Roblox.Services.Exceptions;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/")]
public class PekoraCasino : ControllerBase
{
    public class TixRequest
    {
        public int amount { get; set; }
        public long userId { get; set; }
    }
    [HttpPostBypass("pekora-casino/increment")]
    public async Task<dynamic> IncrementTix([FromBody] TixRequest request)
    {
        await CheckForAuthorization(HttpContext);
        if (request.amount < 0 || request.amount > 5000)
            throw new BadRequestException(1, "Invalid amount specified.");

        if (request.userId <= 0)
            throw new BadRequestException(1, "Invalid user ID specified.");

        await using var currencyLock = await services.economy.AcquireEconomyLock(Models.Assets.CreatorType.User, request.userId);

        var balance = await services.economy.GetBalance(Models.Assets.CreatorType.User, request.userId);

        await services.discordBotApi.SendMessageInChannel("1309550239560110170",
            $"Incrementing Tix for user {request.userId} by {request.amount}. Current balance: {balance.tickets}");

        await services.economy.IncrementCurrency(Models.Assets.CreatorType.User, request.userId, CurrencyType.Tickets, request.amount);
        return new
        {
            success = true,
            message = $"Successfully incremented Tix for user {request.userId} by {request.amount}.",
            newBalance = (await services.economy.GetBalance(Models.Assets.CreatorType.User, request.userId)).tickets
        };
    }
    [HttpPostBypass("pekora-casino/decrement")]
    public async Task<dynamic> DecrementTix([FromBody] TixRequest request)
    {
        await CheckForAuthorization(HttpContext);
        if (request.amount < 0 || request.amount > 5000)
            throw new BadRequestException(1, "Invalid amount specified.");

        if (request.userId <= 0)
            throw new BadRequestException(1, "Invalid user ID specified.");

        await using var currencyLock = await services.economy.AcquireEconomyLock(Models.Assets.CreatorType.User, request.userId);

        var balance = await services.economy.GetBalance(Models.Assets.CreatorType.User, request.userId);

        if (balance.tickets < request.amount)
            throw new BadRequestException(1, "Insufficient Tix balance.");

        await services.discordBotApi.SendMessageInChannel("1309550239560110170",
            $"Decrementing Tix for user {request.userId} by {request.amount}. Current balance: {balance.tickets}");

        await services.economy.DecrementCurrency(Models.Assets.CreatorType.User, request.userId, CurrencyType.Tickets, request.amount);
        return new
        {
            success = true,
            message = $"Successfully decremented Tix for user {request.userId} by {request.amount}.",
            newBalance = (await services.economy.GetBalance(Models.Assets.CreatorType.User, request.userId)).tickets
        };
    }
    [HttpGetBypass("pekora-casino/balance/{userId:long}")]
    public async Task<dynamic> GetBalance(long userId)
    {
        await CheckForAuthorization(HttpContext);
        if (userId <= 0)
            throw new BadRequestException(1, "Invalid user ID specified.");

        await using var currencyLock = await services.economy.AcquireEconomyLock(Models.Assets.CreatorType.User, userId);

        var balance = await services.economy.GetBalance(Models.Assets.CreatorType.User, userId);
        return new
        {
            success = true,
            balance = balance.tickets
        };
    }

    private async Task CheckForAuthorization(HttpContext ctx)
    {
        if (GetRequesterIpRaw(ctx) != Configuration.GameServerIp)
            throw new UnauthorizedException(0, "Unauthorized access to Pekora Casino API.");

        if (!ctx.Request.Headers.TryGetValue("Authorization", out var authHeader) || string.IsNullOrEmpty(authHeader) || authHeader != Configuration.GameServerAuthorization)
            throw new UnauthorizedException(1, "Invalid or missing Authorization header for Pekora Casino API.");
    }
}
