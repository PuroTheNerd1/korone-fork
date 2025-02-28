using System.Text;
using System.IO.Compression;
using MVC = Microsoft.AspNetCore.Mvc;
using Roblox.Libraries.Assets;
using Roblox.Services.Exceptions;
using BadRequestException = Roblox.Exceptions.BadRequestException;
using Roblox.Models.Assets;
using JsonSerializer = System.Text.Json.JsonSerializer;
using MultiGetEntry = Roblox.Dto.Assets.MultiGetEntry;
using Type = Roblox.Models.Assets.Type;
using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Assets;
using Roblox.Website.Middleware;
using Roblox.Libraries.RobloxApi;
using Roblox.Logging;
namespace Roblox.Website.Controllers;
[ApiController]
[Route("/")]
public class Asset : ControllerBase
{
    [HttpGetBypass("v1/asset")]
    [HttpPostBypass("v1/asset")]
    [HttpGetBypass("asset")]
    [HttpPostBypass("asset")]
    public async Task<MVC.ActionResult> GetAssetById(long? playerId, long id, long? assetversion = null, long? assetversionid = null)
    {
        /*
        This is from corescripts from 2017 for more context

        local CUSTOM_ICONS = {	-- Admins with special icons
        ['7210880'] = 'rbxassetid://134032333', -- Jeditkacheff
        ['13268404'] = 'rbxassetid://113059239', -- Sorcus
        ['261'] = 'rbxassetid://105897927', -- shedlestky
        ['20396599'] = 'rbxassetid://161078086', -- Robloxsai
        }
        if (playerId == 20396599)
            id = 10812;
        if(id == 161078086){
            id = 10812;
        }

        */
        var placeIdHeader = Request.Headers["Roblox-Place-Id"].ToString();
        long.TryParse(placeIdHeader,  out long placeId);
        HttpContext.Response.Headers.Add("Cache-Control", "no-cache, no-store");
        HttpContext.Response.Headers.Add("Pragma", "no-cache");
        HttpContext.Response.Headers.Add("Expires", "-1");
        HttpContext.Response.Headers.Add("ExpiresAbsolute", "0");
        // TODO: This endpoint needs to be updated to return a URL to the asset, not the asset itself.
        // The reason for this is so that cloudflare can cache assets without caching the response of this endpoint, which might be different depending on the client making the request (e.g. under 18 user, over 18 user, rcc, etc).
        if(id == 507766388)
        {
            return PhysicalFile(@"C:\ProjectX\services\Roblox\FixJitter\507766388.rbxm", "application/octet-stream");
        }
        else if(id == 507766666)
        {
            return PhysicalFile(@"C:\ProjectX\services\Roblox\FixJitter\507766666.rbxm", "application/octet-stream");
        }
        // If assetversionid isnt null, set id to assetveresionid
        id = assetversionid ?? id;

        var assetId = id;
        var invalidIdKey = "InvalidAssetIdForConversionV1:" + assetId;
        // Opt
        if (Services.Cache.distributed.StringGetMemory(invalidIdKey) != null)
            throw new RobloxException(400, 0, "Asset is invalid or does not exist");

        var isBotRequest = Request.Headers["bot-auth"].ToString() == Roblox.Configuration.BotAuthorization;

        MultiGetEntry details;
        try
        {
            details = await services.assets.GetAssetCatalogInfo(assetId);
        }
        catch (RecordNotFoundException)
        {
            try
            {
                assetId = await services.assets.GetAssetIdFromRobloxAssetId(assetId);
                details = await services.assets.GetAssetCatalogInfo(assetId);
            }
            catch (RecordNotFoundException)
            {
                string key = "chloeassetcachev1:" + id;
                string? location = await Services.Cache.distributed.StringGetAsync(key);
                if (location == null)
                {
                    if (!isRoblox)
                        throw new RecordNotFoundException();
                    Writer.Info(LogGroup.AssetDelivery, "Asset {0} not found in cache, fetching from Roblox", id);
                    location = await services.robloxApi.GetAssetLocation(id);

                    // Asset is OK!
                    if (location != "BAD")
                    {
                        Writer.Info(LogGroup.AssetDelivery, "Caching asset {0}", id);
                        await Services.Cache.distributed.StringSetAsync(key, location, TimeSpan.FromDays(9));
                    }
                    // We probaly hit a rate limit of a 403 just redirect to Roblox
                    else
                    {
                        Writer.Info(LogGroup.AssetDelivery, "Asset {0} is bad, redirecting to Roblox", id);
                        location = $"https://assetdelivery.roblox.com/v1/asset/?id={id}";  
                    }
                    return Redirect(location);
                }
                else
                {
                    Writer.Info(LogGroup.AssetDelivery, "Using cached asset {0}", id);
                    return Redirect(location);
                }

            }
        }

        // TODO: Fix for this is using a diffrent access key for rendering
        if (!IsAssetApproved(details) && !isBotRequest && !isRCC)
            throw new RobloxException(403, 0, "Asset not approved for requester");
        dynamic assetVersion = assetversion != null ? await services.assets.GetSpecificAssetVersion(assetId, (long)assetversion) : await services.assets.GetLatestAssetVersion(assetId);

        Stream? assetContent = null;
        switch (details.assetType)
        {
            // Special types
            case Roblox.Models.Assets.Type.TeeShirt:
                return new MVC.FileContentResult(Encoding.UTF8.GetBytes(ContentFormatters.GetTeeShirt(assetVersion.contentId)), "application/binary");
            case Models.Assets.Type.Shirt:
                return new MVC.FileContentResult(Encoding.UTF8.GetBytes(ContentFormatters.GetShirt(assetVersion.contentId)), "application/binary");
            case Models.Assets.Type.Pants:
                return new MVC.FileContentResult(Encoding.UTF8.GetBytes(ContentFormatters.GetPants(assetVersion.contentId)), "application/binary");
            // Types that require no authentication and aren't encrypted
            case Models.Assets.Type.Image:
            case Models.Assets.Type.Special:
            // Types that require no authentication
            case Models.Assets.Type.Audio:
            case Models.Assets.Type.Mesh:
            case Models.Assets.Type.Hat:
            case Models.Assets.Type.Model:
            case Models.Assets.Type.Decal:
            case Models.Assets.Type.Head:
            case Models.Assets.Type.Face:
            case Models.Assets.Type.Gear:
            case Models.Assets.Type.Badge:
            case Models.Assets.Type.EmoteAnimation:
            case Models.Assets.Type.Animation:
            case Models.Assets.Type.Torso:
            case Models.Assets.Type.RightArm:
            case Models.Assets.Type.LeftArm:
            case Models.Assets.Type.RightLeg:
            case Models.Assets.Type.LeftLeg:
            case Models.Assets.Type.Package:
            case Models.Assets.Type.GamePass:
            case Models.Assets.Type.Plugin: // TODO: do plugins need auth?
            case Models.Assets.Type.MeshPart:
            case Models.Assets.Type.HairAccessory:
            case Models.Assets.Type.FaceAccessory:
            case Models.Assets.Type.NeckAccessory:
            case Models.Assets.Type.ShoulderAccessory:
            case Models.Assets.Type.FrontAccessory:
            case Models.Assets.Type.BackAccessory:
            case Models.Assets.Type.WaistAccessory:
            case Models.Assets.Type.ClimbAnimation:
            case Models.Assets.Type.DeathAnimation:
            case Models.Assets.Type.FallAnimation:
            case Models.Assets.Type.IdleAnimation:
            case Models.Assets.Type.JumpAnimation:
            case Models.Assets.Type.RunAnimation:
            case Models.Assets.Type.SwimAnimation:
            case Models.Assets.Type.WalkAnimation:
            case Models.Assets.Type.PoseAnimation:
            case Models.Assets.Type.SolidModel:
            case Models.Assets.Type.Video:
                if (details.assetType == Type.Audio)
                    Console.WriteLine($"[info] got audio asset request AUD: {assetId}");
                if (assetVersion.contentUrl != null)
                    assetContent = await services.assets.GetAssetContent(assetVersion.contentUrl);
                break;
                // anything else requires auth
            default:
                var isAuthorized = false;
                if (isRCC)
                    isAuthorized = await ValidateRCCRequest(details, placeId, assetId);
                // It's not RCC making the request. are we authorized?
                else
                    // Use current user as access check
                    isAuthorized = IsUserAuthorizedForAsset(details, assetId, safeUserSession.userId); 
                if (isAuthorized && assetVersion.contentUrl != null)
                    assetContent = await services.assets.GetAssetContent(assetVersion.contentUrl);
                break;
        }

        if (assetContent == null)
        {
            Console.WriteLine("[info] got BadRequest on /asset/ endpoint");
            throw new BadRequestException();
        }

        return File(assetContent, "application/binary");
    }
    // TODO : Unhardcode
    [HttpPostBypass("v2/asset")]
    [HttpGetBypass("v2/asset")]
    public dynamic GetAssetByIdV2(long id)
    {
        return new 
        {
            locations = new 
            {
                assetFormat = "source",
                loation = $"https://assetdelivery.{Configuration.ShortBaseUrl}/v1/asset/?id={id}"
            },
            requestId = Guid.NewGuid().ToString(),
            IsHashDynamic = false,
            IsCopyRightProtected = false,
            isArchived = false,
            assetTypeId = 1,
        };
    }
    
    [HttpPostBypass("asset/batch")]
    [HttpPostBypass("v1/assets/batch")]
    public async Task<IActionResult> AssetBatch()
    {
        List<BatchAssetRequest>? requestData = JsonSerializer.Deserialize<List<BatchAssetRequest>>(await GetRequestBody());
        if (requestData == null)
            throw new BadRequestException();

        var assets = new List<object>();

        //assets.Add(CreateAssetResponse(info.assetType, asset.requestId, info.id, $"{Configuration.BaseUrl}/v1/asset/?id={asset.assetId}"));
        var details = await services.assets.MultiGetInfoById(requestData.Select(a => a.assetId));
        var existingAssetIds = details.Select(d => d.id).ToList();

        assets.AddRange(details.SelectMany(d =>
        {
            var matchingRequests = requestData.Where(r => r.assetId == d.id);
            return matchingRequests.Select(req =>
            {
                var requestId = req?.requestId ?? Guid.NewGuid().ToString();
                return CreateAssetResponse(d.assetType, requestId, d.id, $"{Configuration.BaseUrl}/v1/asset/?id={d.id}");
            });
        }));

        var robloxAssetRequest = requestData.Where(r => !existingAssetIds.Contains(r.assetId)).ToList();
        if (robloxAssetRequest.Count > 0)
        {
            var robloxAssets = await services.robloxApi.GetAssetsFromBatch(robloxAssetRequest);
            assets.AddRange(robloxAssets.Select(d =>
            {
                long assetId = robloxAssetRequest.FirstOrDefault(r => r.requestId == d.requestId)?.assetId ?? 0;
                return CreateAssetResponse((Type)d.assetTypeId, d.requestId, assetId, d.location ?? $"{Configuration.BaseUrl}/v1/asset/?id={assetId}");
            }));
            assets.AddRange(robloxAssets.SelectMany(d =>
            {
                var matchingRequests = requestData.Where(r => r.requestId == d.requestId);
                return matchingRequests.Select(req =>
                {
                    long assetId = robloxAssetRequest.FirstOrDefault(r => r.requestId == d.requestId)?.assetId ?? 0;
                    return CreateAssetResponse((Type)d.assetTypeId, d.requestId, assetId, d.location ?? $"{Configuration.BaseUrl}/v1/asset/?id={assetId}");
                });
            }));
        }


        return Content(JsonSerializer.Serialize(assets), "application/json");
    }
    private async Task ProcessRobloxAssetsAsync(IEnumerable<dynamic> robloxResults, List<object> robloxAssets, List<object> assets)
    {
        foreach (var robloxAsset in robloxResults)
        {
            if (robloxAsset.location == null)
                continue;

            // Get assetId from assets list
            var asset = robloxAssets.FirstOrDefault(a => ((dynamic)a).requestId == robloxAsset.requestId);
            long assetId = ((dynamic)asset).assetId;
            assets.Add(new
            {
                location = robloxAsset.location,
                requestId = robloxAsset.requestId,
                IsHashDynamic = false,
                IsCopyrightProtected = false,
                IsArchived = false,
                assetTypeId = robloxAsset.assetTypeId
            });

            await services.robloxassets.SetRobloxAssetLocationInCache(assetId, robloxAsset.location);
        }
    }
    private static object CreateAssetResponse(Type assetType, string requestId, long assetId, string location)
    {
        return new
        {
            location,
            requestId = requestId,
            IsHashDynamic = false,
            IsCopyrightProtected = false,
            IsArchived = false,
            assetTypeId = (int)assetType
        };
    }
    private async Task<bool> ValidateRCCRequest(MultiGetEntry details, long placeId, long assetId)
    {
        var isAuthorized = false;   
        // if rcc is trying to access current place, allow through
        isAuthorized = placeId == assetId;
        // If game server is trying to load a new place (current placeId is empty), then allow it
        if (!isAuthorized && details.assetType == Type.Place && placeId == 0)
            // Game server is trying to load, so allow it
            isAuthorized = true;
        // If rcc is making the request, but it's not for a place, validate the request:
        if (!isAuthorized)
        {
            // Check permissions
            var placeDetails = await services.assets.GetAssetCatalogInfo(placeId);

            if (placeDetails.creatorType == details.creatorType &&
                placeDetails.creatorTargetId == details.creatorTargetId)
            {
                // We are authorized
                isAuthorized = true;
            }
        }
        return isAuthorized;
    }
    private bool IsAssetApproved(MultiGetEntry details)
    {
        return details.moderationStatus == ModerationStatus.ReviewApproved || details.moderationStatus == ModerationStatus.AwaitingModerationDecision;
    }
    private bool IsUserAuthorizedForAsset(MultiGetEntry details, long assetId, long userId)
    {
        return services.assets.CanUserModifyItem(assetId, userId).Result || details.creatorType == CreatorType.User && details.creatorTargetId == 1;;
    }
}