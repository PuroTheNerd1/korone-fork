using System.Diagnostics;
using System.Text.RegularExpressions;
using Dapper;
using Dapper.Contrib.Extensions;
using InfluxDB.Client.Api.Domain;
using Roblox.Dto.Assets;
using Roblox.Dto.Forums;
using Roblox.Services.App.FeatureFlags;
using Roblox.Services.Exceptions;

namespace Roblox.Services;

public class PromocodesService : ServiceBase, IService
{
    public async Task AddPromocode(string promocode, long assetId)
    {
        await InsertAsync("asset_promocodes", new
        {
            asset_id = assetId,
            promocode,
        });
    }
    public async Task<long> GetAssetIdFromPromocode(string promocode)
    {
        return await db.QueryFirstOrDefaultAsync<long>("SELECT asset_id FROM asset_promocodes WHERE promocode = :promocode", new
        {
            promocode,
        });
    }
    public async Task<bool> IsPromocodeClaimed(string promocode, long userId)
    {
        return await db.QueryFirstOrDefaultAsync<bool>("SELECT 1 FROM user_asset_promocodes WHERE code = :promocode AND user_id = :userId", new
        {
            promocode,
            userId,
        });
    }
    public async Task<long> ClaimPromocode(string promocode, long userId)
    {
        long assetId = 0;
        await InTransaction(async (t) =>
        {
            if (await IsPromocodeClaimed(promocode, userId))
                throw new RecordNotFoundException("Promocode already claimed");
            // If this failes 
            await InsertAsync("user_asset_promocodes", new
            {
                user_id = userId,
                code = promocode,
            });
            assetId = await GetAssetIdFromPromocode(promocode);
            if (assetId == 0)
                throw new RecordNotFoundException("Invalid promocode");
            UsersService users = new UsersService();
            // Double check if the user already owns the asset
            var ownedCopies = (await users.GetUserAssets(userId, assetId)).ToList();
            if (ownedCopies.Count != 0)
                throw new RecordNotFoundException("Asset is already owned");
            var id = await db.QuerySingleOrDefaultAsync(
                "INSERT INTO user_asset (asset_id, user_id, serial) VALUES (:asset_id, :user_id, :serial) RETURNING user_asset.id", new
                {
                    asset_id = assetId,
                    user_id = userId,
                    serial = 0,
                });
            return 0;
        });
        return assetId;
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
