using Dapper;
using Roblox.Dto.Admin;
using Roblox.Dto.Assets;
using Roblox.Dto.Economy;
using Roblox.Dto.Users;
using Roblox.Libraries.DiscordApi;
using Roblox.Models.Assets;
using Roblox.Models.Economy;
using Roblox.Models.Staff;
using Roblox.Services.Exceptions;
using AssetMultiGetEntry = Roblox.Dto.Assets.MultiGetEntry;
using Type = Roblox.Models.Assets.Type;

namespace Roblox.Services.AdminApi;

public class AdminApiService : ServiceBase
{
    private AssetsService? _assets;
    private CooldownService? _cooldown;
    private EconomyService? _economy;
    private GameServerService? _gameServer;
    private UsersService? _users;
    private DiscordBotApi? _discordBotApi;

    private AssetsService assets => _assets ??= ServiceProvider.GetOrCreate<AssetsService>(this);
    private CooldownService cooldown => _cooldown ??= ServiceProvider.GetOrCreate<CooldownService>(this);
    private EconomyService economy => _economy ??= ServiceProvider.GetOrCreate<EconomyService>(this);
    private GameServerService gameServer => _gameServer ??= ServiceProvider.GetOrCreate<GameServerService>(this);
    private UsersService users => _users ??= ServiceProvider.GetOrCreate<UsersService>(this);
    private DiscordBotApi discordBotApi => _discordBotApi ??= new DiscordBotApi(Roblox.Configuration.DiscordBotToken);

    public async Task<string> GetOrMigrateImageUrlAsync(string fileName, bool isThumbnails = true)
    {
        if (!Roblox.Configuration.IsCdnEnabled)
            return Roblox.Configuration.CdnBaseUrl + fileName;

        if (fileName.StartsWith('/'))
            fileName = fileName[1..];
        if (fileName.StartsWith("images/"))
            fileName = fileName[7..];
        if (fileName.StartsWith("groups/"))
        {
            isThumbnails = false;
            fileName = fileName[7..];
        }
        if (fileName.StartsWith("thumbnails/"))
            fileName = fileName[11..];

        const string contentType = "image/png";
        var r2Service = ServiceProvider.GetOrCreate<R2StorageService>(this);
        var r2Key = (isThumbnails ? "images/thumbnails/" : "images/groups/") + fileName;
        var localPath = (isThumbnails ? Roblox.Configuration.ThumbnailsDirectory : Roblox.Configuration.GroupIconsDirectory) + fileName;
        var markerPath = localPath + ".migrated";

        if (!File.Exists(markerPath))
        {
            if (!await r2Service.FileExistsAsync(r2Key) && File.Exists(localPath))
            {
                using var file = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, 0, FileOptions.Asynchronous);
                await r2Service.UploadFileAsync(r2Key, file, contentType);
            }

            try
            {
                File.Create(markerPath).Close();
            }
            catch
            {
            }
        }

        return R2StorageService.GetPublicUrl(r2Key);
    }

    public async Task<IReadOnlyCollection<PendingGroupIconEntry>> GetPendingIconsAsync()
    {
        var result = (await db.QueryAsync<PendingGroupIconEntry>(
            "SELECT group_icon.group_id as group_id, group_icon.name, group_icon.user_id as user_id, u.username as creatorName FROM group_icon INNER JOIN \"user\" u ON u.id = group_icon.user_id WHERE is_approved = 0 ORDER BY group_id"))
            .ToList();

        foreach (var item in result)
        {
            item.name = await GetOrMigrateImageUrlAsync("/images/groups/" + item.name, false);
        }

        return result;
    }

    public async Task GiftUsersAsync(GiftUsersRequest request, long actorUserId, bool actorIsOwner)
    {
        if (!actorIsOwner)
            throw new StaffException("You are not allowed to do that");

        if (!await cooldown.TryIncrementBucketCooldown("GiftGlobalLimitV3", 100, TimeSpan.FromHours(12)))
            throw new StaffException("You hit the global rate limit");
        if (!await cooldown.TryIncrementBucketCooldown($"SameGiftV2:{request.giftId}", 1, TimeSpan.FromHours(12)))
            throw new StaffException("You already gifted the same item");

        var details = await assets.GetAssetCatalogInfo(request.assetId);
        if (details.itemRestrictions.Contains("LimitedUnique") || details.itemRestrictions.Contains("Limited"))
            throw new StaffException("This item is a limited");

        var giftOwners = await db.QueryAsync<CollectibleUserAssetEntry>(
            "SELECT id AS userAssetId, asset_id AS assetId, user_id AS userId, price, serial, created_at AS createdAt, updated_at AS updatedAt FROM user_asset WHERE asset_id = :giftId",
            new
            {
                giftId = request.giftId,
            });
        var assetInfo = await assets.GetAssetCatalogInfo(request.assetId);
        foreach (var owner in giftOwners)
        {
            long? serial = null;
            var userAssetId = await db.QuerySingleOrDefaultAsync<long>(
                "INSERT INTO user_asset (asset_id, user_id, serial) VALUES (:assetId, :userId, :serial) RETURNING id",
                new
                {
                    assetId = request.assetId,
                    userId = owner.userId,
                    serial,
                });

            await economy.InsertTransaction(new AssetPurchaseTransaction(owner.userId, assetInfo.creatorType,
                assetInfo.creatorTargetId, CurrencyType.Robux, assetInfo.price ?? 0, assetInfo.id, userAssetId));
            await economy.InsertTransaction(new AssetSaleTransaction(owner.userId, assetInfo.creatorType,
                assetInfo.creatorTargetId, CurrencyType.Robux, assetInfo.price ?? 0, assetInfo.id, userAssetId));

            await assets.IncrementSaleCount(request.assetId);
        }
    }

    public async Task<PendingAssetEntry> GetModerationDetailsAsync(long assetId, Func<long, bool> isOwnerUserId)
    {
        var item = await db.QuerySingleOrDefaultAsync<PendingAssetEntry>(
            "SELECT asset.id, asset.name, asset_thumbnail.content_url, asset.asset_type as assetType FROM asset LEFT JOIN asset_thumbnail ON asset_thumbnail.asset_id = asset.id WHERE asset.id = :id",
            new
            {
                id = assetId,
            });
        if (item == null)
            throw new StaffException("Asset ID is invalid");

        var assetInfo = await assets.GetAssetCatalogInfo(assetId);
        var latestVersion = await TryGetLatestAssetVersionAsync(assetId);
        var (creatorId, creatorName) = await ResolveCreatorAsync(assetInfo, latestVersion);
        item.creatorId = creatorId;
        item.creatorName = creatorName;

        if (item.content_url == null && item.assetType != Type.Audio && item.assetType != Type.Video)
        {
            assets.RenderAsset(item.id, item.assetType);
        }
        else if (item.content_url != null)
        {
            item.content_url = await GetOrMigrateImageUrlAsync("/images/thumbnails/" + item.content_url + ".png");
        }

        return item;
    }

    public async Task<Stream> GetPendingAssetStreamAsync(long assetId, long actorUserId, bool actorIsOwner)
    {
        var assetInfo = await assets.GetAssetCatalogInfo(assetId);
        if (assetInfo.moderationStatus != ModerationStatus.AwaitingApproval && !actorIsOwner)
            throw new StaffException("Item is not pending: " + assetInfo.moderationStatus);
        if (assetInfo.assetType != Type.Audio && assetInfo.assetType != Type.Video && assetInfo.assetType != Type.Model)
            throw new StaffException("Only videos/audios are allowed");

        var version = await assets.GetLatestAssetVersion(assetId);
        if (version.contentUrl == null)
            throw new StaffException("Unsupported action");

        return await assets.GetAssetContent(version.contentUrl);
    }

    public async Task<IReadOnlyCollection<PendingAssetEntry>> GetPendingAssetsAsync(long actorUserId, bool actorIsOwner, Func<long, bool> isOwnerUserId)
    {
        var offset = 0;
        var result = new List<PendingAssetEntry>();

        while (result.Count < 10)
        {
            var query = new SqlBuilder();
            var template = query.AddTemplate(
                "SELECT asset.id, asset.name, asset_thumbnail.content_url, asset.asset_type as assetType FROM asset LEFT JOIN asset_thumbnail ON asset_thumbnail.asset_id = asset.id /**where**/ ORDER BY asset.id LIMIT 10 OFFSET :offset");
            query.OrWhereMulti("(asset.moderation_status = :status AND asset.asset_type = $1)", new[]
            {
                Type.Image,
                Type.Decal,
                Type.Audio,
                Type.Face,
                Type.Mesh,
                Type.Lua,
                Type.Model,
                Type.Package,
                Type.Place,
                Type.Plugin,
                Type.Mesh,
                Type.SolidModel,
                Type.Video,
                Type.GamePass,
                Type.Badge,
            });
            query.AddParameters(new
            {
                status = ModerationStatus.AwaitingApproval,
                offset,
            });

            var firstPass = (await db.QueryAsync<PendingAssetEntry>(template.RawSql, template.Parameters)).ToList();
            if (firstPass.Count == 0)
                return result;

            offset += firstPass.Count;
            foreach (var item in firstPass)
            {
                var details = await assets.GetAssetCatalogInfo(item.id);
                var latest = await TryGetLatestAssetVersionAsync(item.id);
                var (creatorId, creatorName) = await ResolveCreatorAsync(details, latest);
                item.creatorId = creatorId;
                if (item.creatorId == actorUserId && !actorIsOwner)
                    continue;

                item.creatorName = creatorName;

                if (item.content_url == null && item.assetType != Type.Audio && item.assetType != Type.Video)
                {
                    assets.RenderAsset(item.id, item.assetType);
                    continue;
                }

                if (item.content_url != null)
                {
                    item.content_url = await GetOrMigrateImageUrlAsync("/images/thumbnails/" + item.content_url + ".png");
                }

                result.Add(item);
            }
        }

        return result;
    }

    public async Task ModerateAssetAsync(ModerateAssetRequest request, long actorUserId, bool actorIsOwner, Func<long, bool> isOwnerUserId)
    {
        var details = await db.QuerySingleOrDefaultAsync<AssetModerationStatus>(
            "SELECT moderation_status as moderationStatus, roblox_asset_id as robloxAssetId FROM asset WHERE asset.id = :id",
            new
            {
                id = request.assetId,
            });
        if (details == null)
            throw new StaffException("Asset ID is invalid");

        if (!await cooldown.TryIncrementBucketCooldown($"ModerateApprovedItem_Hour:{actorUserId}:{request.assetId}",
                3, TimeSpan.FromHours(1)))
        {
            await discordBotApi.SendMessageInChannel(Roblox.Configuration.DiscordLogChannelId,
                $"## POSSIBLE ABUSE\nStaff member {actorUserId} has accepted Asset Id {request.assetId} more than three times in one hour.");
            throw new StaffException("Moderation of same asset rate limit exceeded, please wait and try again later.");
        }

        var currentStatus = details.moderationStatus;
        if (currentStatus == ModerationStatus.ReviewApproved && !request.isApproved && !actorIsOwner)
        {
            if (!await cooldown.TryIncrementBucketCooldown($"ModerateApprovedItem_Hour:{actorUserId}", 250, TimeSpan.FromHours(1)))
                throw new StaffException("Moderation of already approved item rate limit exceeded (hour). Contact an administrator.");
            if (!await cooldown.TryIncrementBucketCooldown($"ModerateApprovedItem_Day:{actorUserId}", 500, TimeSpan.FromDays(1)))
                throw new StaffException("Moderation of already approved item rate limit exceeded (day). Contact an administrator.");
            if (!await cooldown.TryIncrementBucketCooldown("ModerateApprovedItem_Day_Global", 5000, TimeSpan.FromDays(1)))
                throw new StaffException("Moderation of already approved item rate limit exceeded (day). Contact an administrator.");
        }

        var latest = await assets.GetLatestAssetVersion(request.assetId);
        var assetInfo = await assets.GetAssetCatalogInfo(request.assetId);

        if (!request.isApproved)
        {
            var isOwnerCreatedAsset = assetInfo.creatorType == CreatorType.User && isOwnerUserId(assetInfo.creatorTargetId);
            var minCreationTime = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
            if (isOwnerCreatedAsset)
                throw new StaffException("You do not have permission to delete items created by an owner");
            if (assetInfo.createdAt < minCreationTime)
                throw new StaffException("This asset cannot be deleted since it was created too long ago");
        }

        if (latest.creatorId == actorUserId && !actorIsOwner)
            throw new StaffException("You cannot moderate your own assets");

        if (assetInfo.assetType == Type.Audio && details.canEarnRobuxFromApproval)
        {
            await AwardCommissionForModerationAsync(actorUserId);
        }

        var newStatus = request.isApproved ? ModerationStatus.ReviewApproved : ModerationStatus.Declined;
        await db.ExecuteAsync("UPDATE asset SET moderation_status = :status, is_18_plus = :is_18_plus WHERE id = :id",
            new
            {
                is_18_plus = request.is18Plus,
                status = newStatus,
                id = request.assetId,
            });

        if (newStatus == ModerationStatus.ReviewApproved)
        {
            await db.ExecuteAsync("UPDATE asset_media SET is_approved = TRUE WHERE media_asset_id = :id",
                new
                {
                    id = request.assetId,
                });
        }

        if (assetInfo.assetType == Type.Place && newStatus != ModerationStatus.ReviewApproved)
        {
            var gameServers = await gameServer.GetGameServersForPlace(assetInfo.id);
            foreach (var server in gameServers)
            {
                await gameServer.ShutDownServerAsync(server.id);
            }
        }

        await assets.InsertAssetModerationLog(request.assetId, actorUserId, newStatus);
        var children = (await db.QueryAsync<AssetIdRow>(
            "SELECT DISTINCT asset_id as assetId FROM asset_version WHERE content_id = :id",
            new
            {
                id = request.assetId,
            })).ToArray();
        foreach (var item in children)
        {
            await db.ExecuteAsync("UPDATE asset SET moderation_status = :status, is_18_plus = :is_18_plus WHERE id = :id",
                new
                {
                    is_18_plus = request.is18Plus,
                    status = newStatus,
                    id = item.assetId,
                });
            await assets.InsertAssetModerationLog(item.assetId, actorUserId, newStatus);
        }

        if (details.robloxAssetId != null && details.robloxAssetId != 0)
        {
            var duplicates = await db.QueryAsync<AssetIdRow>(
                "SELECT id as assetId FROM asset WHERE roblox_asset_id = :id",
                new
                {
                    id = details.robloxAssetId.Value,
                });
            foreach (var duplicate in duplicates)
            {
                await db.ExecuteAsync("UPDATE asset SET moderation_status = :status, is_18_plus = :is_18_plus WHERE id = :id",
                    new
                    {
                        is_18_plus = request.is18Plus,
                        status = newStatus,
                        id = duplicate.assetId,
                    });
                await assets.InsertAssetModerationLog(duplicate.assetId, actorUserId, newStatus);
            }
        }
    }

    public async Task ModerateAndDeleteItemAsync(ModerateAssetRequest request, long actorUserId, bool actorIsOwner, Func<long, bool> isOwnerUserId)
    {
        if (!actorIsOwner)
        {
            if (!await cooldown.TryIncrementBucketCooldown("DeleteAssetV1_Hour", 250, TimeSpan.FromHours(1)))
                throw new StaffException("Asset deletion rate limit exceeded (hour). Contact an administrator.");
            if (!await cooldown.TryIncrementBucketCooldown("DeleteAssetV1_Day", 500, TimeSpan.FromDays(1)))
                throw new StaffException("Asset deletion rate limit exceeded (day). Contact an administrator.");
            if (!await cooldown.TryIncrementBucketCooldown("DeleteAssetV1_Global", 5000, TimeSpan.FromDays(1)))
                throw new StaffException("Asset deletion rate limit exceeded (global). Contact an administrator.");
        }

        await ModerateAssetAsync(request, actorUserId, actorIsOwner, isOwnerUserId);
        if (!request.isApproved)
        {
            await assets.DeleteAsset(request.assetId);
        }
    }

    public async Task<IReadOnlyCollection<PendingAssetIconEntry>> GetPendingAssetIconsAsync()
    {
        var firstPass = (await db.QueryAsync<PendingAssetIconEntry>(
            "SELECT asset_icon.id, asset.name, asset_icon.content_url, asset_icon.asset_id as asset_id FROM asset_icon INNER JOIN asset ON asset.id = asset_icon.asset_id WHERE asset_icon.moderation_status = :status ORDER BY asset.id LIMIT 10",
            new
            {
                status = ModerationStatus.AwaitingApproval,
            })).ToList();
        if (firstPass.Count == 0)
            return firstPass;

        foreach (var item in firstPass)
        {
            try
            {
                var latest = await assets.GetLatestAssetVersion(item.asset_id);
                item.creatorId = latest.creatorId;
                item.creatorName = (await users.GetUserById(latest.creatorId)).username;
            }
            catch (Exception)
            {
                item.creatorId = 1;
                item.creatorName = "ROBLOX";
            }

            if (item.content_url != null)
            {
                item.content_url = await GetOrMigrateImageUrlAsync("/images/thumbnails/" + item.content_url + ".png");
            }
        }

        return firstPass;
    }

    public async Task ModerateIconAsync(ModerateIconRequest request, bool actorIsOwner)
    {
        var details = await db.QuerySingleOrDefaultAsync<AssetIconModerationRow>(
            "SELECT moderation_status, content_url, asset_id FROM asset_icon WHERE asset_icon.id = :id",
            new
            {
                id = request.iconId,
            });
        if (details == null)
            throw new StaffException("Asset ID is invalid");
        if (details.moderation_status != ModerationStatus.AwaitingApproval && !actorIsOwner)
        {
            throw new StaffException("You can only moderate items in a pending state. This item was already approved or declined.");
        }

        if (request.isApproved)
        {
            await db.ExecuteAsync("UPDATE asset_icon SET moderation_status = :status WHERE id = :id",
                new
                {
                    id = request.iconId,
                    status = ModerationStatus.ReviewApproved,
                });

            if (request.is18Plus)
            {
                await db.ExecuteAsync("UPDATE asset SET is_18_plus = true WHERE id = :id",
                    new
                    {
                        id = details.asset_id,
                    });
            }
        }
        else
        {
            await db.ExecuteAsync("UPDATE asset_icon SET moderation_status = :status WHERE id = :id",
                new
                {
                    status = ModerationStatus.Declined,
                    id = request.iconId,
                });

            if (!string.IsNullOrWhiteSpace(details.content_url))
            {
                await assets.DeleteAssetContent(details.content_url, Roblox.Configuration.ThumbnailsDirectory);
            }
        }
    }

    public async Task ToggleGroupIconAsync(IconToggleRequest request, long actorUserId)
    {
        request.name = NormalizeGroupIconName(request.name);

        var affected = await db.ExecuteAsync(
            "UPDATE group_icon SET is_approved = :approved WHERE group_id = :gid AND name = :name",
            new
            {
                request.approved,
                gid = request.groupId,
                request.name,
            });
        if (affected == 0)
        {
            throw new StaffException("The icon URL is no longer valid. Maybe the group owner created a new icon before the previous one was approved?");
        }

        await AwardCommissionForModerationAsync(actorUserId);

        if (request.approved == 2 && !string.IsNullOrWhiteSpace(request.name))
        {
            await assets.DeleteAssetContent(request.name, Roblox.Configuration.GroupIconsDirectory);
        }
    }

    private async Task AwardCommissionForModerationAsync(long actorUserId)
    {
        await economy.IncrementCurrency(CreatorType.User, actorUserId, CurrencyType.Robux, 5);
        await users.InsertAsync("user_transaction", new
        {
            type = PurchaseType.Commission,
            currency_type = CurrencyType.Robux,
            amount = 5,
            sub_type = TransactionSubType.StaffAssetModeration,
            user_id_one = actorUserId,
            user_id_two = 1,
        });
    }

    private static string NormalizeGroupIconName(string? iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
            throw new StaffException("Invalid icon");

        if (iconName.IndexOf("/", StringComparison.Ordinal) != -1)
        {
            var location = iconName.LastIndexOf("/", StringComparison.Ordinal) + 1;
            iconName = iconName[location..];
        }

        if (iconName.IndexOf("/", StringComparison.Ordinal) != -1 || iconName.IndexOf("\\", StringComparison.Ordinal) != -1)
            throw new StaffException("Invalid filename: " + iconName);

        return iconName;
    }

    private async Task<AssetVersionEntry?> TryGetLatestAssetVersionAsync(long assetId)
    {
        try
        {
            return await assets.GetLatestAssetVersion(assetId);
        }
        catch (RecordNotFoundException)
        {
            return null;
        }
    }

    private async Task<(long creatorId, string creatorName)> ResolveCreatorAsync(AssetMultiGetEntry assetInfo, AssetVersionEntry? latestVersion)
    {
        if (latestVersion != null)
        {
            try
            {
                var userInfo = await users.GetUserById(latestVersion.creatorId);
                return (latestVersion.creatorId, userInfo.username);
            }
            catch (RecordNotFoundException)
            {
            }
        }

        var creatorId = assetInfo.creatorType == CreatorType.User ? assetInfo.creatorTargetId : 1;
        var creatorName = string.IsNullOrWhiteSpace(assetInfo.creatorName) ? "ROBLOX" : assetInfo.creatorName;
        return (creatorId, creatorName);
    }

    private sealed class AssetIdRow
    {
        public long assetId { get; set; }
    }

    private sealed class AssetIconModerationRow
    {
        public ModerationStatus moderation_status { get; set; }
        public string? content_url { get; set; }
        public long asset_id { get; set; }
    }

    private sealed class StaffException : RobloxException
    {
        public StaffException(string errorMessage = "") : base(500, 0, errorMessage)
        {
        }
    }
}
