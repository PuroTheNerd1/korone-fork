using Microsoft.Extensions.Configuration;

namespace Roblox.ServiceDefaults;

public static class RobloxServiceInfrastructure
{
    private static int _initialized;

    public static void Initialize(IConfiguration configuration)
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
        {
            return;
        }

        var postgres = configuration.GetSection("Postgres").Value;
        if (!string.IsNullOrWhiteSpace(postgres))
        {
            Roblox.Services.Database.Configure(postgres);
        }

        var redis = configuration.GetSection("Redis").Value;
        if (!string.IsNullOrWhiteSpace(redis))
        {
            Roblox.Services.Cache.Configure(
                redis,
                configuration.GetSection("RedisAuthentication").Value);
        }
    }
}
