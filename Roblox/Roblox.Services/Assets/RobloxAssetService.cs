using System.Configuration;
using System.Runtime.CompilerServices;

namespace Roblox.Services;

public class RobloxAssetService : ServiceBase, IService
{

    public async Task<string?> GetRobloxAssetLocationFromCache(long id)
    {
        return await redis.StringGetAsync($"robloxasset:{id}:data");
    }
    
    public async Task ProcessRobloxAssets(IEnumerable<dynamic> robloxResults, List<object> assets)
    {
        foreach (var robloxAsset in robloxResults)
        {
            if (robloxAsset.location == null)
                continue;

            assets.Add(new
            {
                location = robloxAsset.location,
                requestId = robloxAsset.requestId,
                IsHashDynamic = false,
                IsCopyrightProtected = false,
                IsArchived = false,
                assetTypeId = (int)Enum.Parse(typeof(Type), robloxAsset.assetType),
            });

            await SetRobloxAssetLocationInCache(robloxAsset.assetId, robloxAsset.location);
        }
    }
    public async Task SetRobloxAssetLocationInCache(long id, string location)
    {
        await redis.StringSetAsync($"robloxasset:{id}:data", location, TimeSpan.FromDays(10));
    }
    public bool IsReusable()
    {
        return true;
    }

    public bool IsThreadSafe()
    {
        return true;
    }
}