using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Games;
using Roblox.Exceptions;
using Roblox.Exceptions.Services.Assets;
using Roblox.Libraries.Cursor;
using Roblox.Libraries.Exceptions;
using Roblox.Models;
using Roblox.Models.Assets;
using Roblox.Models.Db;
using Roblox.Website.WebsiteModels.Catalog;
using Type = Roblox.Models.Assets.Type;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/develop/v1")]
public class DevelopControllerV1 : ControllerBase
{
    private static int pendingThumbnailsUploads { get; set; } = 0;
    private static readonly Mutex pendingThumbnailUploadsMux = new();

    [HttpGet("user/is-verified-creator")]
    public dynamic IsVerifiedCreator()
    {
        return new
        {
            isVerifiedCreator = true,
        };
    }

    [HttpGet("assets/genres")]
    public RobloxCollection<Models.Assets.Genre> GetAssetGenres()
    {
        return new RobloxCollection<Models.Assets.Genre>()
        {
            data = Enum.GetValues<Models.Assets.Genre>(),
        };
    }

    [HttpGet("assets")]
    public async Task<dynamic> MultiGetAssetInfo(string assetIds)
    {
        var splitIds = assetIds.Split(",").Select(long.Parse).ToList();
        if (splitIds.Count > 100) throw new BadRequestException();
        var details = await services.assets.MultiGetAssetDeveloperDetails(splitIds);
        return new
        {
            data = details,
        };
    }

    [HttpGetBypass("/v1/assets/{assetId}/latest-saved-version")]
    [HttpGet("assets/{assetId}/latest-saved-version")]
    public async Task<dynamic> GetLatestSavedVersion(long assetId)
    {
        await services.assets.ValidatePermissions(assetId, safeUserSession.userId);
        var version = await services.assets.GetLatestAssetVersion(assetId);
        return new
        {
            assetId = version.assetId,
            assetVersionNumber = version.versionNumber,
            creatorTargetId = version.creatorId,
            creatingUniverseId = 0,
            created = version.createdAt,
            isEqualToCurrentPublishedVersion = true,
            isPublished = true
        };
    }

    [HttpGetBypass("/v1/assets/{assetId}/saved-versions")]
    [HttpGetBypass("/v1/assets/{assetId}/published-versions")]
    [HttpGet("assets/{assetId}/published-versions")]
    public async Task<dynamic> GetPublishedVersions(long assetId, string? cursor, int limit = 10, SortOrder sortOrder = SortOrder.Desc)
    {
        await services.assets.ValidatePermissions(assetId, safeUserSession.userId);
        if (limit is < 1 or > 100) limit = 10;
        int offset = !string.IsNullOrWhiteSpace(cursor) ? int.Parse(cursor) : 0;
        var versions = (await services.assets.GetAssetVersions(assetId, offset, limit, sortOrder)).ToList();
        return new
        {
            previousPageCursor = offset >= limit ? (offset - limit).ToString() : null,
            nextPageCursor = versions.Count >= limit ? (offset + limit).ToString() : null,
            data = versions.Select(c => new
            {
                assetId = c.assetId,
                assetVersionNumber = c.versionNumber,
                creatorTargetId = c.creatorId,
                creatingUniverseId = 0,
                created = c.createdAt,
                isEqualToCurrentPublishedVersion = c.contentUrl == versions.First().contentUrl,
                isPublished = true
            })
        };
    }

    [HttpGetBypass("/v1/assets/{assetId}/versions/{versionNumber}")]
    [HttpGet("assets/{assetId}/versions/{versionNumber}")]
    public async Task<dynamic> GetPublishedAssetVersion(long assetId, long versionNumber)
    {
        await services.assets.ValidatePermissions(assetId, safeUserSession.userId);
        var version = await services.assets.GetSpecificAssetVersion(assetId, versionNumber);
        var latestVersion = await services.assets.GetLatestAssetVersion(assetId);
        return new
        {
            assetId = version.assetId,
            assetVersionNumber = version.versionNumber,
            creatorTargetId = version.creatorId,
            creatingUniverseId = 0,
            created = version.createdAt,
            isEqualToCurrentPublishedVersion = version.contentUrl == latestVersion.contentUrl,
            isPublished = version.contentUrl == latestVersion.contentUrl,
        };
    }
    [HttpPostBypass("/v1/assets/{assetId}/revert-version")]
    [HttpPost("assets/{assetId}/revert-version")]
    public async Task<dynamic> RevertAssetVersion(long assetId, long assetVersionNumber)
    {
        if (!await services.cooldown.TryCooldownCheck("Place:RevertVersion:StartUserId:" + safeUserSession.userId, TimeSpan.FromSeconds(5)) || !await services.cooldown.TryCooldownCheck("Place:RevertVersion:StartIp:" + GetIP(), TimeSpan.FromSeconds(5))
            throw new TooManyRequestsException(0, "Too many requests");
        
        await services.assets.ValidatePermissions(assetId, safeUserSession.userId);
        if (assetVersionNumber < 1)
            throw new BadRequestException(0, "Version number must be greater than 0");
        var assetInfo = await services.assets.GetAssetCatalogInfo(assetId);
        if (assetInfo.assetType != Type.Model && assetInfo.assetType != Type.Place)
            throw new BadRequestException(1, "This endpoint is meant for models and places only. Use assets/{assetId} for other assets.");
        var version = await services.assets.GetSpecificAssetVersion(assetId, assetVersionNumber);

        await services.assets.CreateAssetVersion(assetId, safeUserSession.userId, version.contentUrl!);
        return Ok();
    }
    [HttpPost("assets/upload-gameicon")]
    public async Task<dynamic> UploadGameIcon(long placeId, [Required, FromForm] IFormFile file)
    {
        if (!await services.cooldown.TryCooldownCheck("Place:GameIcon:StartUserId:" + safeUserSession.userId, TimeSpan.FromSeconds(5)) || !await services.cooldown.TryCooldownCheck("Place:GameIcon:StartIp:" + GetIP(), TimeSpan.FromSeconds(5)))
        {
            throw new TooManyRequestsException(0, "Too many requests");
        }
        await services.assets.ValidatePermissions(placeId, safeUserSession.userId);
        var details = await services.assets.GetAssetCatalogInfo(placeId);
        if (details.assetType != Models.Assets.Type.Place) {
            throw new BadRequestException(1, "Cannot upload a game icon for a non place");
        }

        await services.assets.CreateGameIcon(placeId, file.OpenReadStream());
        return Ok();
    }
    
    [HttpPost("assets/upload-thumbnail")]
    public async Task<dynamic> UploadGameThumbnail(long universeId, [Required, FromForm] IFormFile file)
    {
        if (!await services.cooldown.TryCooldownCheck("Universe:ThumbnailUpload:StartUserId:" + safeUserSession.userId, TimeSpan.FromSeconds(5)) || !await services.cooldown.TryCooldownCheck("Universe:ThumbnailUpload:StartIp:" + GetIP(), TimeSpan.FromSeconds(5)))
        {
            throw new TooManyRequestsException(0, "Too many requests");
        }
        // have to do this retarded check because we dont wanna charge people until the thumbnail has been created
        var balance = await services.economy.GetBalance(CreatorType.User, safeUserSession.userId);
        if (balance.robux < 10)
            throw new BadRequestException(0, "Not enough Robux for purchase");
        var universe = await services.games.SafeGetUniverseInfo(safeUserSession.userId, universeId);
        
        if (await services.games.GetGameMediaCount(universe.rootPlaceId) >= 10)
            throw new BadRequestException(0, "Too many thumbnails on this Universe");

        lock (pendingThumbnailUploadsMux)
        {
            if (pendingThumbnailsUploads >= 5)
            {
                throw new TooManyRequestsException(0, "Too many pending uploads");
            }
            pendingThumbnailsUploads++;
        }
        try
        {
            var readStream = file.OpenReadStream();
            if (readStream is null)
                throw new BadRequestException(0, "File provided is invalid");
            await services.assets.CreateGameThumbnail(universe.rootPlaceId, readStream);
            try
            {
                await services.economy.ChargeForGameMediaUpload(CreatorType.User, safeUserSession.userId);
            }
            catch (LogicException) 
            {
                throw new BadRequestException(0, "Not enough Robux for purchase");
            }
        }
        finally
        {
            lock (pendingThumbnailUploadsMux)
            {
                pendingThumbnailsUploads--;
            }
        }
        
        return Ok();
    }
    
    [HttpPost("universes/{universeId}/thumbnails/auto-generated")]
    public async Task<dynamic> UploadAutoGenThumbnail(long universeId)
    {
        if (!await services.cooldown.TryCooldownCheck("Universe:ThumbnailUpload:StartUserId:" + safeUserSession.userId, TimeSpan.FromSeconds(5)) || !await services.cooldown.TryCooldownCheck("Universe:ThumbnailUpload:StartIp:" + GetIP(), TimeSpan.FromSeconds(5)))
        {
            throw new TooManyRequestsException(0, "Too many requests");
        }
        var universe = await services.games.SafeGetUniverseInfo(safeUserSession.userId, universeId);
        
        if (await services.games.GetGameMediaCount(universe.rootPlaceId) == 10) {
            throw new BadRequestException(0, "Too many thumbnails on this Universe");
        }

        await services.assets.CreateAutoGeneratedGameThumbnail(universe.rootPlaceId);
        return Ok();
    }
    
    [HttpPost("universes/{universeId}/thumbnails/{thumbnailAssetId}")]
    public async Task<dynamic> DeleteGameThumbnail(long universeId, long thumbnailAssetId)
    {
        var universe = await services.games.SafeGetUniverseInfo(safeUserSession.userId, universeId);

        var gameMedia = await services.games.GetSpecificGameMedia(thumbnailAssetId);
        if (gameMedia is null || gameMedia.assetId != universe.rootPlaceId)
        {
            throw new NotFoundException(0, "Thumbnail not found");
        }
        
        await services.assets.DeleteGameThumbnail(universe.rootPlaceId, thumbnailAssetId);
        return Ok();
    }
    
    [HttpPost("places/{placeId}/game-icons/auto-generated")]
    public async Task<dynamic> UploadAutoGenGameIcon(long placeId, [FromForm] IFormFile? file = null)
    {
        if (!await services.cooldown.TryCooldownCheck("Place:GameIcon:StartUserId:" + safeUserSession.userId, TimeSpan.FromSeconds(5)) || !await services.cooldown.TryCooldownCheck("Place:GameIcon:StartIp:" + GetIP(), TimeSpan.FromSeconds(5)))
        {
            throw new TooManyRequestsException(0, "Too many requests");
        }
        await services.assets.ValidatePermissions(placeId, safeUserSession.userId);
        var details = await services.assets.GetAssetCatalogInfo(placeId);
        if (details.assetType != Models.Assets.Type.Place) {
            throw new BadRequestException(1, "Cannot upload a game thumbnail for a non place");
        }

        await services.assets.CreateAutoGeneratedGameIcon(placeId);
        return Ok();
    }

    [HttpPatch("assets/{assetId:long}")]
    public async Task UpdateAsset(long assetId, [Required, FromBody] UpdateAssetRequest request)
    {
        await services.assets.ValidatePermissions(assetId, safeUserSession.userId);
        
        await services.assets.UpdateAsset(assetId, request.description, request.name, request.genres.First(),
            request.isCopyingAllowed, request.enableComments, request.isForSale);
    }

    [HttpPatch("assets/update-gamepass/{assetId:long}")]
    public async Task UpdateGamePassAsset(long assetId, [Required, FromForm] UpdateGamePassAssetRequest request) 
    {
        await services.assets.ValidatePermissions(assetId, safeUserSession.userId);
        
        var details = await services.assets.GetAssetCatalogInfo(assetId);
        if (details.assetType != Models.Assets.Type.GamePass)
            throw new BadRequestException(1, "This endpoint is meant for updating gamepass assets only. Use assets/{assetId} for other assets.");
        
        await services.assets.UpdateAsset(assetId, request.description, request.name, request.genres.First(),
            false, request.enableComments, request.isForSale, request.file != null ? request.file.OpenReadStream() : null);
    }
    
    [HttpPatch("universes/{universeId:long}/set-year")]
    public async Task SetYear(long universeId, [Required, FromBody] SetYearRequest request)
    {
        var place = await services.games.GetRootPlaceId(universeId);
        await services.assets.ValidatePermissions(place, safeUserSession.userId);
        await services.games.SetYear(place, request.year);
    }
    [HttpPatch("universes/{universeId:long}/max-player-count")]
    public async Task SetMaxPlayerCount(long universeId, [Required, FromBody] SetMaxPlayerCountRequest request)
    {
        var place = await services.games.GetRootPlaceId(universeId);
        await services.assets.ValidatePermissions(place, safeUserSession.userId);
        await services.games.SetMaxPlayerCount(place, request.maxPlayers);
    }

    [HttpPatch("places/{placeId}/roblox-place-id")]
    public async Task UpdateRobloxPlaceId(long placeId, [Required, FromBody] SetRobloxPlaceIdRequest request)
    {
        await services.assets.ValidatePermissions(placeId, safeUserSession.userId);
        if (request.robloxPlaceId < 0)
            throw new BadRequestException(0, "Roblox Place ID cannot be negative");

        await services.games.SetRobloxPlaceId(placeId, request.robloxPlaceId);
    }
    // Developer Products
    // TODO: this needs a rewrite bad and the ability for staff to review it and stuff

    // get universe's products
    [HttpGetBypass("/v1/universes/{universeId}/developerproducts")]
    [HttpGet("universes/{universeId:long}/developerproducts")]
    public async Task<dynamic> GetDeveloperProducts(long universeId, long pageNumber, long? pageSize = 10) 
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);
        long parsedSize = (pageSize > 50 || pageSize < 1) ? 10 : (pageSize ?? 10);
        if (pageNumber > 100 || pageSize < 1) pageNumber = 1;
        var offset = parsedSize * (pageNumber == 0 ? 0 : pageNumber - 1);
        var products = await services.games.GetDeveloperProducts(universeId, parsedSize * 1, offset * 1);
        return products.Select(c => new
        {
            id = c.id,
            name = c.name,
            Description = c.description,
            iconImageAssetId = c.iconImageAssetId,
            shopId = c.shopId,
        });
    }
    
    // create developer product
    // https://apidocs.sixteensrc.zip/develop/docs.html#!/DeveloperProducts/post_v1_universes_universeId_developerproducts
    [HttpPost("universes/{universeId:long}/developerproducts")]
    public async Task<dynamic> CreateDeveloperProduct(long universeId, string name, string description, long priceInRobux, long iconImageAssetId) 
    {
        await services.games.CanManageUniverse(safeUserSession.userId, universeId);

        // this god awful code is presented to you by the fact i barely know how
        // developer products work so i gotta stick as close to the roblox api as possible
        // You're Welcome!
        if (priceInRobux < 0 || priceInRobux > 1000000) {
            throw new BadRequestException(0, "Price in Robux can not be negative or above 1 million.");
        }


        var developerProductCount = await services.games.GetDeveloperProductCount(universeId);
        if (developerProductCount >= 25) 
            throw new BadRequestException(0, "Too many developer products for this universe.");
        
        var asset = await services.assets.GetAssetCatalogInfo(iconImageAssetId);

        // ?? no idea why roblox does that
        if (asset.creatorTargetId != safeUserSession.userId) 
        { 
            throw new ForbiddenException(2, "Icon Asset is created by another user.");
        }


        long prodId = await services.games.CreateDeveloperProduct(safeUserSession.userId, universeId, name, description, priceInRobux,
            iconImageAssetId);

        return new
        {
            productId = prodId
        };
    }
    
    // update developer product
    // https://apidocs.sixteensrc.zip/develop/docs.html#!/DeveloperProducts/post_v1_universes_universeId_developerproducts_productId_update
    [HttpPost("universes/{universeId:long}/developerproducts/{productId}/update")]
    public async Task UpdateDeveloperProduct(long universeId, long productId, [FromBody] UpdateDevProductRequest request) 
    {
        // this god awful code is presented to you by the fact i barely know how
        // developer products work so i gotta stick as close to the roblox api as possible
        // You're Welcome!

        await services.games.CanManageUniverse(safeUserSession.userId, universeId);


        if (request.PriceInRobux < 0 || request.PriceInRobux > 1000000) {
            throw new BadRequestException(0, "Price in robux can not be negative or above 1 million.");
        }
        
        var product = await services.games.GetDeveloperProductInfoFull(productId);

        if (product.name == request.Name &&
            product.description == request.Description &&
            product.iconImageAssetId == request.IconImageAssetId &&
            product.price == request.PriceInRobux) {
            // nothing changed lal
            return;
        }

        if (product.universeId != universeId)
            throw new BadRequestException(0, "Developer Product does not belong to this Universe.");
        if (product.creatorId != safeUserSession.userId)
            throw new ForbiddenException(2, "You are not the creator of this Developer Product.");

        var asset = await services.assets.GetAssetCatalogInfo(request.IconImageAssetId);

        // ?? no idea why roblox does that
        if (asset.creatorTargetId != safeUserSession.userId)
            throw new ForbiddenException(2, "Icon Asset is created by another user.");
        if (asset.assetType != Type.Image)
            throw new BadRequestException(0, "Icon asset type is not an image.");

        var oldImage = await services.assets.GetAssetModerationStatus(product.iconImageAssetId);
        if (oldImage != ModerationStatus.ReviewApproved) 
            throw new BadRequestException(0, "You must wait until your Developer Product's former icon is approved by moderators.");
        
        
        // dont want people to use this to test and exploit the chat filter
        request.Name = services.filter.FilterText(request.Name);
        request.Description = services.filter.FilterText(request.Description);
        
        if (string.IsNullOrEmpty(request.Name)) 
            throw new AssetNameTooShortException();
        if (request.Name.Length > Rules.NameMaxLength)
            throw new AssetNameTooLongException();
        if (request.Description is { Length: > Rules.DescriptionMaxLength })
            throw new AssetDescriptionTooLongException();
        
        await services.games.UpdateDeveloperProduct(productId, request.Name, request.Description, request.PriceInRobux,
            request.IconImageAssetId);
    }
}