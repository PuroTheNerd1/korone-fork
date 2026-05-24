using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Roblox.Web.Infrastructure.Auth;
using Roblox.Web.Infrastructure.Configuration;
using Roblox.Web.Infrastructure.Http;

namespace Roblox.Web.Infrastructure.Extensions;

public static class RobloxWebInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddRobloxWebInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IRobloxRequestContextAccessor, RobloxRequestContextAccessor>();
        services.AddOptions<RobloxWebInfrastructureOptions>().Configure(options =>
        {
            options.Authorization = configuration["Authorization"];
            options.SessionJwtKey = configuration["Jwt:Sessions"];
            options.InternalServiceHosts = configuration.GetSection("InternalServiceHosts").Get<List<string>>() ?? new List<string>();
        });

        var sessionJwtKey = configuration["Jwt:Sessions"];
        if (!string.IsNullOrWhiteSpace(sessionJwtKey))
        {
            RobloxSessionTokenCodec.Configure(sessionJwtKey);
        }

        return services;
    }
}
