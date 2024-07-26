using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
namespace Roblox.Website.Middleware;
public class ThumbnailMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _basePath;

    public ThumbnailMiddleware(RequestDelegate next, string basePath)
    {
        _next = next;
        _basePath = Path.GetFullPath(basePath);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestPath = context.Request.Path.Value;

        if (requestPath.StartsWith("/images/thumbnails", StringComparison.OrdinalIgnoreCase))
        {
            var fileName = requestPath.Substring("/images/thumbnails".Length).TrimStart('/');
            var filePath = Path.Combine(_basePath, fileName);

            filePath = Path.GetFullPath(filePath);
            if (!filePath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            {
                await _next(context); 
                return;
            }

            if (File.Exists(filePath))
            {
                var fileExtension = Path.GetExtension(filePath);
                if (string.IsNullOrEmpty(fileExtension))
                {
                    filePath = Path.ChangeExtension(filePath, ".png");
                }

                if (File.Exists(filePath))
                {
                    context.Response.ContentType = "image/png"; 
                    await context.Response.SendFileAsync(filePath);
                    return;
                }
            }
        }

        await _next(context);
    }
}
