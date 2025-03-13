using MVC = Microsoft.AspNetCore.Mvc;
using CsvHelper;
using System.Xml;
using Roblox.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Roblox.Dto.Marketplace;
using Newtonsoft.Json;
using System.Dynamic;
using Roblox.Models;
using Roblox.Dto.Friends;
using Roblox.Models.Assets;
using System.Text.RegularExpressions;
using Roblox.Dto.Games;
using System.ComponentModel.DataAnnotations;
using Roblox.Exceptions;
namespace Roblox.Website.Controllers
{
    [MVC.ApiController]
    [MVC.Route("/")]
    public class Inventory: ControllerBase
    {
        [HttpGetBypass("/v1/users/{userId}/items/{itemType}/{itemTargetId}")]
        public async Task <RobloxCollectionPaginated<dynamic>> GetOwnedItemsOfSpecificType(long userId, string itemType, long itemTargetId)
        {
            bool canViewItems = userId == safeUserSession.userId;
            var assetType = services.assets.GetTypeFromPluralString(itemType);
            if (!canViewItems && (isRCC && assetType == Models.Assets.Type.GamePass))
            {
                canViewItems = true;
            }
            else
            {
                throw new BadRequestException();
            }
            var inventory = await services.inventory.GetInventory(userId, assetType, "asc", 100, 0);
            
            return new RobloxCollectionPaginated<dynamic>
            {
                previousPageCursor = (string?)null,
                nextPageCursor = (string?)null,
                data = inventory.Where(c => c.assetId == itemTargetId || c.assetTypeId == assetType).Select(c => new
                {
                    Id = c.assetId,
                    Name = c.name,
                    Type = (int)c.assetTypeId,
                    InstanceId = 0
                })
            };
        }
    }
}