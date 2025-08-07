using Roblox.Libraries.RobloxApi;
using Roblox.Logging;
namespace Roblox.Services;

public class RobloxAssetService : ServiceBase, IService
{
    private string GetAssetCacheKey(long id)
    {
        return "chloeassetcache_v5:" + id;
    }
    public async Task<string> GetAssetById(long id, long placeId = 0)
    {
        var location = await GetAssetInCacheById(id);
        if (location is not null)
            return location;
        // We didn't find the asset in cache, let's get it
        using var games = ServiceProvider.GetOrCreate<GamesService>(this);
        // Get the Roblox place ID for the given place ID this is for impersonation
        long robloxPlaceId = await games.GetRobloxPlaceIdForPlace(placeId);
        // Debug
        Writer.Info(LogGroup.AssetDelivery, "GetAssetById assetId: {0}, place id: {1}, impersonator place id: {2}", id, placeId, robloxPlaceId);
        // Now we request asset delivery for the asset with our roblox place id
        var assetDelivery = await RobloxApi.GetAssetById(id, robloxPlaceId);

        // Set the asset in cache
        await SetAssetInCacheById(id, assetDelivery.location);

        return assetDelivery.location;
    }
    // TODO: Caching
    public async Task<IEnumerable<AssetDeliveryV1BatchResponse>> GetAssetsInBulk(List<BatchAssetRequest> assets, long placeId)
    {
        using var games = ServiceProvider.GetOrCreate<GamesService>(this);
        long robloxPlaceId = await games.GetRobloxPlaceIdForPlace(placeId);

        return await RobloxApi.GetAssetsFromBatch(assets, robloxPlaceId);
    }

    private async Task<string?> GetAssetInCacheById(long id)
    {
        return await redis.StringGetAsync(GetAssetCacheKey(id));
    }
    private async Task SetAssetInCacheById(long id, string location)
    {
        // Cache for 2 days
        await redis.StringSetAsync(GetAssetCacheKey(id), location, TimeSpan.FromDays(2));
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