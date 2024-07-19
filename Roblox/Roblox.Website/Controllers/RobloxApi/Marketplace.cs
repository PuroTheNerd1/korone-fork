using MVC = Microsoft.AspNetCore.Mvc;
using Roblox.Website.Controllers.Internal;
using CsvHelper;
using System.Xml;
using Roblox.Services.Exceptions;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class Marketplace: ControllerBase
    {
        [HttpGetBypass("marketplace/productinfo")]
        public async Task<dynamic> GetProductInfo(long assetId)
        {
            try
            {
                var details = await services.assets.GetAssetCatalogInfo(assetId);
                return new
                {
                    TargetId = details.id,
                    AssetId = details.id,
                    ProductId = details.id, 
                    Name = details.name,
                    Description = details.description,
                    AssetTypeId = (int)details.assetType,
                    IsForSale = details.isForSale,
                    IsPublicDomain = details.isForSale && details.price == 0,
                    Creator = new
                    {
                        Id = details.creatorTargetId,
                        Name = details.creatorName,
                    }
                };
            }
            catch (RecordNotFoundException)
            {
                return Redirect($"https://economy.roblox.com/v2/assets/{assetId}/details");
            }
        }
    }
}