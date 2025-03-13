using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Games;
using Roblox.Exceptions;
using Roblox.Models;
using Roblox.Models.Assets;
using Roblox.Models.Db;
using Roblox.Website.WebsiteModels.Catalog;

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
        var universe = await services.games.SafeGetUniverseInfo(safeUserSession.userId, universeId);
        
        if (await services.games.GetGameMediaCount(universe.rootPlaceId) == 10) {
            throw new BadRequestException(0, "Too many thumbnails on this Universe");
        }

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
            var balance = await services.economy.GetBalance(CreatorType.User, safeUserSession.userId);
            // check if has enough
            if (balance.robux < 10)
                throw new BadRequestException(0, "Not enough Robux for purchase");
            var readStream = file.OpenReadStream();
            if (readStream is null)
                throw new BadRequestException(0, "File provided is invalid");
            // TODO: actually make it deduct 10 roux robux
            // whenever CreateGameThumbnail returns, how do i make sure it actually succceeded, so that i can deduct after
            await services.economy.ChargeForGameThumbnailUpload(CreatorType.User, safeUserSession.userId);
            await services.assets.CreateGameThumbnail(universe.rootPlaceId, readStream);
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
        var place = await services.games.SafeGetUniverseInfo(safeUserSession.userId, universeId);
        await services.assets.ValidatePermissions(place.rootPlaceId, safeUserSession.userId);
        await services.assets.DeleteGameThumbnail(place.rootPlaceId, thumbnailAssetId);
        return Ok();
    }
    
    // TODO: do game icons use universes?
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
        if (details.assetType != Models.Assets.Type.GamePass) {
            throw new BadRequestException(1, "This endpoint is meant for updating gamepass assets only. Use assets/{assetId} for other assets.");
        }
        
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
}