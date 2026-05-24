using Microsoft.AspNetCore.Http;

namespace Roblox.Web.Infrastructure.Http;

public class RobloxRequestContextAccessor : IRobloxRequestContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RobloxRequestContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public RobloxRequestContext Current
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return new RobloxRequestContext();
            }

            var existing = httpContext.GetRobloxRequestContext();
            if (existing != null)
            {
                return existing;
            }

            var created = RobloxRequestContextFactory.CreateAnonymous(httpContext);
            httpContext.SetRobloxRequestContext(created);
            return created;
        }
    }

    public void SetCurrent(RobloxRequestContext context)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        httpContext.SetRobloxRequestContext(context);
    }
}
