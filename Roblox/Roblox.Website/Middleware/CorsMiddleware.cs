using Microsoft.AspNetCore.Http.Extensions;

namespace Roblox.Website.Middleware;

public class RobloxPlayerCorsMiddleware
{
    private RequestDelegate _next;
    public RobloxPlayerCorsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    private string GenerateCspHeader(bool isAuthenticated)
    {
        var connectSrc = "'self' https://*.pekora.zip wss://*.pekora.zip https://hcaptcha.com https://*.hcaptcha.com https://*.cdn.com https://*.archive.org/* https://web.archive.org https://challenges.cloudflare.com/* ws://localhost:*";

        var imgSrc = "'self' data:";
        if (isAuthenticated)
        {
            imgSrc += " https://*.pekora.zip https://*.cdn.com https://*.archive.org http://*.archive.org https://challenges.cloudflare.com/*";
        }

        var scriptSrc =
            "'unsafe-eval' 'self' https://challenges.cloudflare.com/turnstile/v0/api.js https://translate.google.com https://hcaptcha.com https://*.hcaptcha.com https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/js/bootstrap.bundle.min.js https://pekora.zip http://*.archive.org https://*.archive.org http://js.rbxcdn.com/46eace8231bf3c1ce64c55407d9ae60d.js";

        var fontSrc = "'self' https://fonts.gstatic.com http://www.pekora.zip https://pekora.zip https://*.pekora.zip https://www.pekora.zip/fonts/GothamSSmBold.woff2 https://www.pekora.zip/fonts/GothamSSmMedium.woff2 https://www.pekora.zip/fonts/GothamSSmBook.woff2";

        return "default-src 'self'; img-src " + imgSrc + "; child-src 'self'; script-src " + scriptSrc + "; frame-src 'self' https://hcaptcha.com https://challenges.cloudflare.com http://challenges.cloudflare.com https://*.archive.org; style-src 'unsafe-inline' 'self' http://*.archive.org https://fonts.googleapis.com https://hcaptcha.com https://*.hcaptcha.com https://pekora.zip https://www.pekora.zip https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css https://pekora.zip/fonts/gotham1.css http://*.pekora.zip; font-src " + fontSrc + "; connect-src " + connectSrc + "; worker-src 'self';";
    }


    public async Task InvokeAsync(HttpContext ctx)
    {
        var isAuthenticated = ctx.Items.ContainsKey(".ROBLOSECURITY");
        ctx.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        ctx.Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";
        //ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
        ctx.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        ctx.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        //ctx.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
        ctx.Response.Headers["Content-Security-Policy"] = GenerateCspHeader(isAuthenticated);
        await _next(ctx);
    }
}

public static class RobloxPlayerCorsMiddlewareExtensions
{
    public static IApplicationBuilder UseRobloxPlayerCorsMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RobloxPlayerCorsMiddleware>();
    }
}