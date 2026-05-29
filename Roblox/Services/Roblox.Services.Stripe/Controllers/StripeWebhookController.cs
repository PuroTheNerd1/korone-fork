using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Roblox.Models.Assets;
using Roblox.Models.Economy;
using Roblox.Web.Infrastructure.Controllers;
using Roblox.Web.Infrastructure.Metadata;

using Stripe;
using StripeCheckout = Stripe.Checkout;

namespace Roblox.Services.Stripe.Controllers;

[ApiController]
[InternalServiceOnly]
[Route("stripe-api/webhook")]
public class StripeWebhookController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    : RobloxControllerBase
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();

    [HttpPost]
    [BrowserFacingEndpoint]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            var webhookSecret = configuration["Stripe:WebhookSecret"];
            var stripeSignature = Request.Headers["Stripe-Signature"];

            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as StripeCheckout.Session;

                string? userIdStr = null;
                var userIdField = session?.CustomFields?.FirstOrDefault(f => f.Key == "Korone User ID");
                if (userIdField != null)
                {
                    userIdStr = userIdField.Text.Value;
                }

                var currency = session?.Currency?.ToUpper() ?? "USD";
                if (currency != "USD" || string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out var userId))
                    return Ok();

                var amount = (session?.AmountTotal ?? 0) / 100.0;

                var clampedTier = GetClampedTier(amount);
                if (clampedTier == 0)
                    return Ok();

                await RewardUserAsync(userId, clampedTier);
                await SendDiscordNotificationAsync(userId, amount, currency);
            }

            return Ok();
        }
        catch (StripeException e)
        {
            return BadRequest(new { error = e.Message });
        }
        catch(Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
    
    private async Task SendDiscordNotificationAsync(long userId, double amount, string currency)
    {
        // Pull the Discord Webhook URL from appsettings.json
        var discordWebhookUrl = configuration["Discord:WebhookUrl"];

        var payload = new
        {
            embeds = new[]
            {
                new
                {
                    title = "🎉 New Donation Received!",
                    color = 3066993,
                    fields = new[]
                    {
                        new { name = "Donor", value = $"[{userId}](https://www.pekora.zip/users/{userId}/profile)", inline = true },
                        new { name = "Amount", value = $"{amount:F2} {currency}", inline = true }
                    },
                    timestamp = DateTime.UtcNow.ToString("o")
                }
            }
        };

        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        await _httpClient.PostAsync(discordWebhookUrl, content);
    }
    
    private static double GetClampedTier(double amount)
    {
        return amount switch
        {
            < 5.00 => 0,               // Less than $5 gets nothing
            >= 5.00 and < 10.00 => 5,  // $5.00 to $9.99 clamps to 5
            >= 10.00 and < 25.00 => 10, // $10.00 to $24.99 clamps to 10
            >= 25.00 and < 50.00 => 25, // $25.00 to $49.99 clamps to 25
            >= 50.00 => 50, // $50.00 or anything higher clamps to 50
            _ => 0
        };
    }

    private async Task RewardUserAsync(long userId, double tier)
    {
        if (tier == 0) 
            return;
        
        long rewardItem = tier switch
        {
            5.00 => 673140,
            10.00 => 673108,
            25.00 => 673098,
            50.00 => 673144,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
        };

        long rewardRobux = tier switch
        {
            5.00 => 550,
            10.00 => 800,
            25.00 => 1500,
            50.00 => 5500,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null)
        };

        await services.economy.IncrementCurrency(CreatorType.User, userId, CurrencyType.Robux, rewardRobux);
        await services.users.CreateUserAsset(userId, rewardItem);
    }
}