using MVC = Microsoft.AspNetCore.Mvc;
using Roblox.Website.Controllers.Internal;
using CsvHelper;
using System.Xml;
using Roblox.Services.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Roblox.Dto.Users;
using Roblox.Exceptions;
using Roblox.Services;
using Roblox.Services.App.FeatureFlags;
using Roblox.Services.Exceptions;
using BadRequestException = Roblox.Exceptions.BadRequestException;
using ServiceProvider = Roblox.Services.ServiceProvider;

using Roblox.Dto.Marketplace;
using Newtonsoft.Json;
using System.Dynamic;
using Roblox.Models;
using Roblox.Dto.Friends;
using Roblox.Models.Assets;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class GameListing: ControllerBase
    {
        [HttpGetBypass("v1/games/sorts")]
        public dynamic GameSort()
        {
            string rawJson = System.IO.File.ReadAllText(Configuration.JsonDataDirectory + "GameSort.json");
            dynamic? json = JsonConvert.DeserializeObject<ExpandoObject>(rawJson);
            return json ?? new ExpandoObject();
        }
        [HttpGetBypass("v1/games/list")]
        public async Task<dynamic> GetGamesList(string? sortToken, int maxRows = 10, Genre? genre = null, string? keyword = null)
        {
            if (maxRows is > 100 or < 1) maxRows = 10;
            var result = await services.games.GetGamesList(userSession?.userId, sortToken, maxRows, genre, keyword);
            return new
            {
                games = result,
            };
        }
    }
}