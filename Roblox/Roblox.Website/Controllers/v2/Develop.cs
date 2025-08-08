using Microsoft.AspNetCore.Mvc;
using Roblox.Models.Db;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/develop/v2")]
public class DevelopControllerV2 : ControllerBase
{
    [HttpGetBypass("/v2/assets/{assetId}/versions")]
    [HttpGet("assets/{assetId}/versions")]
    public async Task<dynamic> GetAssetVersions(long assetId, string? cursor, int limit = 10, SortOrder sortOrder = SortOrder.Desc)
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
                Id = c.assetVersionId,
                assetId = c.assetId,
                assetVersionNumber = c.versionNumber,
                creatorTargetId = c.creatorId,
                creatingUniverseId = (string?)null,
                created = c.createdAt,
                isEqualToCurrentPublishedVersion = c.contentUrl == versions.First().contentUrl,
                isPublished = true
            })
        };
    }
}