using Dapper;
using Roblox.Dto.Assets;
using Roblox.Dto.Thumbnails;
using Roblox.Logging;
using Roblox.Models.Assets;
using Roblox.Models.Thumbnails;
using Roblox.Services.Exceptions;
using Type = Roblox.Models.Assets.Type;

namespace Roblox.Services;

public class ThumbnailsService : ServiceBase, IService
{
    public async Task<IEnumerable<long>> GetUserIdsWithBrokenThumbnails()
    {
        var t = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5));
        var query = await db.QueryAsync(
            "SELECT u.id FROM \"user\" u LEFT JOIN user_avatar ua on u.id = ua.user_id WHERE ua.headshot_thumbnail_url IS NULL OR ua.thumbnail_url IS NULL");
        return query.Select(c => (long)c.id);
    }

    public async Task<IEnumerable<Dto.Assets.AssetIdWithType>> GetPlacesWithOutOfDateAutoGenThumbnails()
    {
        var query = (await db.QueryAsync<Dto.Assets.AssetIdWithType>(
            "SELECT asset.id as assetId, asset.asset_type as assetType FROM asset INNER JOIN asset_thumbnail at on asset.id = at.asset_id WHERE (select asset_version.updated_at from asset_version where asset_id = asset.id ORDER BY id DESC LIMIT 1) > at.updated_at AND asset_type = 9")).ToArray();
        Writer.Info(LogGroup.FixBrokenThumbnails, "There are {0} out of date places", query.Length);
        return query;
    }

    public async Task<IEnumerable<Dto.Assets.AssetIdWithType>> GetAssetIdsWithoutThumbnail()
    {
        var outOfDate = await GetPlacesWithOutOfDateAutoGenThumbnails();
        var t = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(5));
        var query = await db.QueryAsync<Dto.Assets.AssetIdWithType>(
            "SELECT asset.id as assetId, asset.asset_type as assetType FROM asset LEFT JOIN asset_thumbnail t on asset.id = t.asset_id WHERE t.content_url IS NULL AND asset.updated_at <= :dt AND asset.asset_type = ANY(:ids)", new
            {
                ids = new List<Models.Assets.Type>()
                {
                    // Focus on user-generated content for now. We might add hats/games in the future.
                    Models.Assets.Type.TeeShirt,
                    Models.Assets.Type.Shirt,
                    Models.Assets.Type.Pants,
                    Models.Assets.Type.Image,
                    // >:D
                    // Models.Assets.Type.Hat,
                    // Models.Assets.Type.Gear,
                    // Models.Assets.Type.HairAccessory,
                    // Models.Assets.Type.FaceAccessory,
                    // Models.Assets.Type.NeckAccessory,
                    // Models.Assets.Type.ShoulderAccessory,
                    // Models.Assets.Type.FrontAccessory,
                    // Models.Assets.Type.BackAccessory,
                    // Models.Assets.Type.WaistAccessory,
                    // Models.Assets.Type.Image,

                    //Models.Assets.Type.Face,
                    //Models.Assets.Type.Place,
                    //Models.Assets.Type.Mesh,
                    //Models.Assets.Type.MeshPart,
                }.Select(c => (int)c).ToList(),
                dt = t,
            });
        var response = new List<AssetIdWithType>();
        response.AddRange(outOfDate);
        response.AddRange(query);
        return response;
    }

    public async Task<IEnumerable<ThumbnailEntry>> GetUserHeadshots(IEnumerable<long> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return Array.Empty<ThumbnailEntry>();
        var query = new SqlBuilder();
        var t = query.AddTemplate(
            "SELECT user_id as targetId, headshot_thumbnail_url as imageUrl FROM user_avatar /**where**/");
        query.OrWhereMulti("user_id = $1", ids);

        var entries = await db.QueryAsync<ThumbnailEntry>(t.RawSql, t.Parameters);
        var results = new List<ThumbnailEntry>();
        foreach (var c in entries)
        {
            c.state = c.imageUrl == null ? ThumbnailState.Pending : ThumbnailState.Completed;
            if (c.imageUrl != null)
                c.imageUrl = await GetOrMigrateThumbnailUrlAsync(c.imageUrl);
            results.Add(c);
        }
        return results;
    }

    public async Task<IEnumerable<ThumbnailEntry>> GetUserThumbnails(IEnumerable<long> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new ThumbnailEntry[] { };
        var query = new SqlBuilder();
        var t = query.AddTemplate(
            "SELECT user_id as targetId, thumbnail_url as imageUrl FROM user_avatar /**where**/");
        query.OrWhereMulti("user_id = $1", ids);

        var entries = await db.QueryAsync<ThumbnailEntry>(t.RawSql, t.Parameters);
        var results = new List<ThumbnailEntry>();
        foreach (var c in entries)
        {
            c.state = c.imageUrl == null ? ThumbnailState.Pending : ThumbnailState.Completed;
            if (c.imageUrl != null)
                c.imageUrl = await GetOrMigrateThumbnailUrlAsync(c.imageUrl);
            results.Add(c);
        }

        return results;
    }
    
    public async Task<IEnumerable<ThumbnailEntry>> GetUserThumbnails3D(IEnumerable<long> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new ThumbnailEntry[] { };
        // var query = new SqlBuilder();
        // var t = query.AddTemplate(
        //     "SELECT user_id as targetId, thumbnail_3d_url as imageUrl FROM user_avatar /**where**/");
        // query.OrWhereMulti("user_id = $1", ids);
        var q = await db.QueryAsync<ThumbnailEntry>(@"
                SELECT 
                    user_id as targetId,
                    thumbnail_3d_url as imageUrl
                FROM user_avatar WHERE user_id = ANY(:userIds)
            ", new { userIds = ids.ToList() });

        var results = new List<ThumbnailEntry>();
        foreach (var c in q)
        {
            c.state = c.imageUrl == null ? ThumbnailState.Pending : ThumbnailState.Completed;
            if (c.imageUrl != null)
                c.imageUrl = await GetOrMigrateThumbnailUrlAsync(c.imageUrl);
            results.Add(c);
        }

        return results;
    }

    public async Task<IEnumerable<ThumbnailEntry>> GetAssetThumbnails(IEnumerable<long> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new ThumbnailEntry[] { };
        var query = new SqlBuilder();
        var t = query.AddTemplate(
            "SELECT asset.id as targetId, asset.asset_type as type, at.content_url as imageUrl, asset.moderation_status as moderationStatus FROM asset LEFT JOIN asset_thumbnail at ON at.asset_id = asset.id /**where**/");
        query.OrWhereMulti("asset.id = $1", ids);

        var entries = await db.QueryAsync<AssetThumbnailEntryDb>(t.RawSql, t.Parameters);
        var results = new List<ThumbnailEntry>();
        foreach (var c in entries)
        {
             if (c.moderationStatus == ModerationStatus.Declined)
            {
                c.imageUrl = "/img/blocked.png";
            }
            else if (c.moderationStatus != ModerationStatus.ReviewApproved)
            {
                c.imageUrl = null;
            }
            else
            {
                switch (c.type)
                {
                    case Type.Audio:
                        c.imageUrl = "/img/Audio.png";
                        break;

                    case Type.Animation:
                        c.imageUrl = "/img/Animation2.png";
                        break;

                    case Type.Video:
                        c.imageUrl = "/img/Video.png";
                        break;
                }

                if (!string.IsNullOrEmpty(c.imageUrl) && !c.imageUrl.Contains("images/thumbnails") && !c.imageUrl.Contains("/img/"))
                {
                    c.imageUrl = "/images/thumbnails/" + c.imageUrl + ".png";
                }
            }

            if (!string.IsNullOrEmpty(c.imageUrl))
            {
                c.imageUrl = await GetOrMigrateThumbnailUrlAsync(c.imageUrl);
            }

            results.Add(new ThumbnailEntry
            {
                targetId = c.targetId,
                imageUrl = c.imageUrl,
                state = c.imageUrl == null ? ThumbnailState.Pending : c.moderationStatus == ModerationStatus.Declined ? ThumbnailState.Blocked : ThumbnailState.Completed,
            });
        }

        return results;
    }

    public async Task<IEnumerable<ThumbnailEntry>> GetUserOutfitThumbnails(IEnumerable<long> outfitIds)
    {
        var ids = outfitIds.Distinct().ToList();
        if (ids.Count == 0) return new ThumbnailEntry[] { };
        var query = new SqlBuilder();
        var t = query.AddTemplate(
            "SELECT id as targetId, thumbnail_url as imageUrl FROM user_outfit /**where**/");
        query.OrWhereMulti("id = $1", ids);

        var entries = await db.QueryAsync<ThumbnailEntry>(t.RawSql, t.Parameters);
        var results = new List<ThumbnailEntry>();
        foreach (var c in entries)
        {
            c.state = c.imageUrl == null ? ThumbnailState.Pending : ThumbnailState.Completed;
            if (c.imageUrl != null)
                c.imageUrl = await GetOrMigrateThumbnailUrlAsync(c.imageUrl);
            results.Add(c);
        }

        return results;
    }

    public async Task<IEnumerable<ThumbnailEntry>> GetGroupIcons(IEnumerable<long> groupIds)
    {
        var ids = groupIds.Distinct().ToList();
        if (ids.Count == 0) return new ThumbnailEntry[] { };
        var query = new SqlBuilder();
        var t = query.AddTemplate(
            "SELECT group_id as targetId, CASE WHEN is_approved = 1 THEN name END imageUrl FROM group_icon /**where**/");
        query.OrWhereMulti("group_id = $1", ids);

        var entries = await db.QueryAsync<ThumbnailEntry>(t.RawSql, t.Parameters);
        var results = new List<ThumbnailEntry>();
        foreach (var c in entries)
        {
            c.state = c.imageUrl == null ? ThumbnailState.Pending : ThumbnailState.Completed;
            if (!string.IsNullOrEmpty(c.imageUrl))
            {
                c.imageUrl = "/images/groups/" + c.imageUrl;
            }
            if (c.imageUrl != null)
                c.imageUrl = await GetOrMigrateThumbnailUrlAsync(c.imageUrl, false);
            results.Add(c);
        }

        return results;
    }
    public async Task<IEnumerable<ThumbnailEntryRBX>> GetGameIconsRBX(IEnumerable<long> universeIds)
    {
        var ids = universeIds.Distinct().ToList();
        if (ids.Count == 0) return new ThumbnailEntryRBX[] { };
        var query = new SqlBuilder();
        var t = query.AddTemplate(
            "SELECT universe_id as targetId, content_url as imageUrl, moderation_status as moderationStatus FROM universe_asset INNER JOIN asset_icon ai ON ai.asset_id = universe_asset.asset_id /**where**/");
        query.OrWhereMulti("universe_id = $1", ids);

        var entries = await db.QueryAsync<AssetThumbnailEntryDb>(t.RawSql, t.Parameters);
        var results = new List<ThumbnailEntryRBX>();
        foreach (var c in entries)
        {
            if (c.moderationStatus != ModerationStatus.ReviewApproved)
            {
                c.imageUrl = null;
            }


            //if (universeIds.Count() == 1)
            //{
            //why? if studio requests only 1 game icon it will keep looping and never getting the gameicon
            // throw new RobloxException(401, 1, "Not authorized");
            //}
            if (c.moderationStatus == ModerationStatus.Declined)
            {
                c.imageUrl = "/img/blocked.png";
            }
            if (c.imageUrl != null)
                c.imageUrl = await GetOrMigrateThumbnailUrlAsync(c.imageUrl);

            results.Add(new ThumbnailEntryRBX()
            {
                //targetId = c.targetId,
                TargetId = c.targetId,
                Url = c.imageUrl,
                State = c.imageUrl == null ? ThumbnailState.Pending : c.moderationStatus == ModerationStatus.Declined ? ThumbnailState.Blocked : ThumbnailState.Completed,
            });
        }

        return results;
    }
    public async Task<IEnumerable<ThumbnailEntry>> GetUniverseIcons(IEnumerable<long> universeIds)
    {
        var ids = universeIds.Distinct().ToList();
        if (ids.Count == 0) return new ThumbnailEntry[] { };
        var query = new SqlBuilder();
        var t = query.AddTemplate(
            @"SELECT 
                u.id AS targetId, 
                ai.content_url AS imageUrl, 
                ai.moderation_status AS moderationStatus 
              FROM universe u
              INNER JOIN asset_icon ai ON ai.asset_id = u.root_asset_id 
              /**where**/"
        );
        query.OrWhereMulti("u.id = $1", ids);

        var entries = await db.QueryAsync<AssetThumbnailEntryDb>(t.RawSql, t.Parameters);
        var results = new List<ThumbnailEntry>();
        foreach (var c in entries)
        {
            if (c.imageUrl is not null)
                c.imageUrl = await GetOrMigrateThumbnailUrlAsync(c.imageUrl);

            if (c.moderationStatus != ModerationStatus.ReviewApproved)
                c.imageUrl = "/img/placeholder.png";

            if (c.moderationStatus == ModerationStatus.Declined)
                c.imageUrl = "/img/blocked.png";

            results.Add(new ThumbnailEntry()
            {
                targetId = c.targetId,
                imageUrl = c.imageUrl,
                state = c.imageUrl == null ? ThumbnailState.Pending : c.moderationStatus == ModerationStatus.Declined ? ThumbnailState.Blocked : ThumbnailState.Completed,
            });
        }
        return results;
    }    
    public async Task<IEnumerable<ThumbnailEntry>> GetPlaceIcons(IEnumerable<long> placeIds)
    {
        var ids = placeIds.Distinct().ToList();
        if (ids.Count == 0) return new ThumbnailEntry[] { };
        var query = new SqlBuilder();
        var t = query.AddTemplate(
            @"SELECT 
                asset_id AS targetId, 
                content_url AS imageUrl, 
                moderation_status AS moderationStatus 
              FROM asset_icon
              /**where**/
        ");
        query.OrWhereMulti("asset_id = $1", ids);

        var entries = await db.QueryAsync<AssetThumbnailEntryDb>(t.RawSql, t.Parameters);
        var results = new List<ThumbnailEntry>();
        foreach (var c in entries)
        {
            if (c.imageUrl is not null)
                c.imageUrl = await GetOrMigrateThumbnailUrlAsync(c.imageUrl);

            if (c.moderationStatus != ModerationStatus.ReviewApproved)
                c.imageUrl = "/img/placeholder.png";

            if (c.moderationStatus == ModerationStatus.Declined)
                c.imageUrl = "/img/blocked.png";

            results.Add(new ThumbnailEntry()
            {
                targetId = c.targetId,
                imageUrl = c.imageUrl,
                state = c.imageUrl == null ? ThumbnailState.Pending : c.moderationStatus == ModerationStatus.Declined ? ThumbnailState.Blocked : ThumbnailState.Completed,
            });
        }
        return results;
    }

    private static async Task<string> GetOrMigrateThumbnailUrlAsync(string fileName, bool isThumbnails = true)
    {
        // really shitty work but this accounts for most cases.
        if(fileName.StartsWith('/'))
            fileName = fileName[1..];
        if(fileName.StartsWith("images/"))
            fileName = fileName[7..];
        if(fileName.StartsWith("groups/"))
            fileName = fileName[7..];
        if(fileName.StartsWith("thumbnails/"))
            fileName = fileName[11..];
        var contentType = "image/png";
        if(fileName.EndsWith(".json"))
            contentType = "application/json";
        else if(!fileName.EndsWith(".png"))
            fileName += ".png";
        
        var r2Service = ServiceProvider.GetOrCreate<R2StorageService>();
        var r2Key = (isThumbnails ? "images/thumbnails/" : "images/groups/") + fileName;
        var localPath = (isThumbnails ? Configuration.ThumbnailsDirectory : Configuration.GroupIconsDirectory) + fileName;
        var markerPath = localPath + ".migrated";

        if (!File.Exists(markerPath))
        {
            if (!await r2Service.FileExistsAsync(r2Key))
            {
                if (File.Exists(localPath))
                {
                    using var file = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, 0, FileOptions.Asynchronous);
                    await r2Service.UploadFileAsync(r2Key, file, contentType);
                }
            }
            try { File.Create(markerPath).Close(); } catch { }
        }

        return R2StorageService.GetPublicUrl(r2Key);
    }

    public bool IsThreadSafe()
    {
        return true;
    }

    public bool IsReusable()
    {
        return false;
    }
}