using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Users;
using Roblox.Exceptions;
using Roblox.Models;
using Roblox.Models.Assets;
using Roblox.Services.Exceptions;
using MultiGetEntry = Roblox.Dto.Assets.MultiGetEntry;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/inventory/v2")]
public class InventoryControllerV2 : ControllerBase
{
    [HttpGet("assets/{assetId:long}/owners")]
    public async Task<RobloxCollectionPaginated<OwnershipEntry>> GetAssetOwners(long assetId, string? cursor = null,
        int limit = 10, string sortOrder = "asc")
    {
        var offset = int.Parse(cursor ?? "0");
        if (limit is > 100 or < 1) limit = 10;
        if (sortOrder != "asc" && sortOrder != "desc") sortOrder = "asc";
        var result = (await services.inventory.GetOwners(assetId, sortOrder, offset, limit)).ToList();
        // skip private, terminated, etc
        var privacyData =
            (await services.inventory.MultiCanViewInventory(result
                    .Where(c => c.owner != null)
                    .Select(c => c.owner!.id), userSession?.userId ?? 0)
            ).ToList();
        foreach (var user in result)
        {
            var userPrivacy = user.owner == null ? null : privacyData.Find(c => c.userId == user.owner.id);
            if (userPrivacy is not { canView: true })
            {
                user.owner = null;
            }
        }

        return new(limit, offset, result);
    }

    [HttpDelete("inventory/asset/{assetId:long}")]
    [HttpDeleteBypass("/v2/inventory/asset/{assetId:long}")]
    public async Task DeleteAssetFromInventory(long assetId)
    {
        long userId = safeUserSession.userId;
        MultiGetEntry asset;
        try
        {
            asset = await services.assets.GetAssetCatalogInfo(assetId);
        }
        catch (RecordNotFoundException)
        {
            throw new NotFoundException(1, "This item does not exist.");
        }
        if ((asset.creatorType == CreatorType.User && asset.creatorTargetId == userId) || asset.itemRestrictions.Contains("Limited") || asset.itemRestrictions.Contains("LimitedUnique"))
            throw new ForbiddenException(3, "This item is not allowed to be deleted.");
        if (!await services.inventory.IsOwned(userId, assetId))
            throw new ForbiddenException(2, "You don't own the specified item.");
        
        await services.inventory.DeleteUserAssetId(userId, assetId);
    }
}