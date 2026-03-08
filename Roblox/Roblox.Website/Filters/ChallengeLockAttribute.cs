using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Roblox.Dto.Users;
using Roblox.Models.Sessions;
using Roblox.Website.Services;

namespace Roblox.Website.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class ChallengeLockAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.ActionArguments.Values.OfType<PurchaseRequest>().FirstOrDefault();
        var assetId = (long)context.RouteData.Values["assetId"]!;
        var session = context.HttpContext.Items[Middleware.SessionMiddleware.CookieName] as UserSession;

        if (request is null || session is null)
        {
            Reject(context);
            return;
        }

        var nonceData = await ChallengeLockService.ConsumeNonce(request.nonce);
        if (nonceData is null || nonceData.userId != session.userId || nonceData.assetId != assetId || nonceData.price != request.expectedPrice)
        {
            Reject(context);
            return;
        }

        if (Math.Abs((DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(nonceData.timestamp)).TotalSeconds) > 5)
        {
            Reject(context);
            return;
        }

        try
        {
            var expected = ChallengeLockService.ComputeHmac(assetId, request.expectedPrice, request.nonce, nonceData.timestamp);
            if (!CryptographicOperations.FixedTimeEquals(
                    Convert.FromBase64String(expected),
                    Convert.FromBase64String(request.signature)))
            {
                Reject(context);
                return;
            }
        }
        catch (FormatException)
        {
            Reject(context);
            return;
        }

        await next();
    }

    private static void Reject(ActionExecutingContext context)
    {
        context.Result = new ObjectResult(new { error = "Challenge verification failed" }) { StatusCode = 403 };
    }
}
