namespace Roblox.Services;

public class SetsService : ServiceBase
{
    private static HttpClient sharedClient = new()
    {
        BaseAddress = new Uri("https://sets.pizzaboxer.xyz/Game/Tools/InsertAsset.ashx?"),
    };
    private async Task<dynamic> RequestSetData(HttpClient httpClient, string uri)
    {
        using HttpResponseMessage response = await httpClient.GetAsync(uri);
        string SetData = await response.Content.ReadAsStringAsync();
        return SetData;
    }
    private async Task<dynamic> FetchSet(long? setId, bool bypassCache = false)
    {
        if (!bypassCache)
        {
            string cachedSetData = await redis.StringGetAsync($"set:{setId}:data");
            if (cachedSetData != null)
            {
                return cachedSetData; 
            }
        }

        string setData = await RequestSetData(sharedClient, $"sid={setId}");
        await redis.StringSetAsync($"set:{setId}:data", setData, TimeSpan.FromDays(10));

        return setData; 
    }
    private async Task<dynamic> FetchUserSet(long? nsets = 20, string? type = "user", long? userId = 1, bool bypassCache = false)
    {
        if (!bypassCache)
        {
            string cachedSetData = await redis.StringGetAsync($"set:{userId}:data");
            if (cachedSetData != null)
            {
                return cachedSetData; 
            }
        }    
        string setData = await RequestSetData(sharedClient, $"nsets={nsets}&type={type}&userid={userId}");
        await redis.StringSetAsync($"set:{userId}:data", setData, TimeSpan.FromDays(10));
        return setData; 
    }
    public async Task<string> GrabSet(long? sid, long? nsets, string? type, long? userId)
    {
        if(sid == null){
            if(nsets == null || type == null || userId == null)
                return null;
            return await FetchUserSet(nsets, type, userId);
        };
        return await FetchSet(sid);
    }
}