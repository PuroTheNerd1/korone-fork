using Microsoft.AspNetCore.Http;

namespace Roblox.Web.Infrastructure.Metadata;

public static class RobloxEndpointMetadataExtensions
{
    public static bool HasRobloxMetadata<T>(this Endpoint? endpoint) where T : Attribute
    {
        return endpoint?.Metadata.GetMetadata<T>() != null;
    }

    public static bool AllowsRobloxAnonymous(this Endpoint? endpoint)
    {
        return endpoint.HasRobloxMetadata<AllowRobloxAnonymousAttribute>();
    }

    public static bool RequiresRobloxSession(this Endpoint? endpoint)
    {
        return endpoint.HasRobloxMetadata<RequireRobloxSessionAttribute>();
    }

    public static bool IsInternalServiceOnly(this Endpoint? endpoint)
    {
        return endpoint.HasRobloxMetadata<InternalServiceOnlyAttribute>();
    }

    public static bool ShouldSkipRobloxCsrf(this Endpoint? endpoint)
    {
        return endpoint.HasRobloxMetadata<SkipRobloxCsrfAttribute>();
    }

    public static bool IsBrowserFacingEndpoint(this Endpoint? endpoint)
    {
        return endpoint.HasRobloxMetadata<BrowserFacingEndpointAttribute>();
    }
}
