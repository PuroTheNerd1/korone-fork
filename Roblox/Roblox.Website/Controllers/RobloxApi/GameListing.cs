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
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class GameListing: ControllerBase
    {
        private static Regex numberRegex { get; } = new("([0-9]+)");
        [HttpGet("v1/games/multiget-playability-status")]
        public dynamic MultiGetPlayabilityStatus()
        {
            var ids = HttpContext.Request.QueryString.Value;
            return numberRegex.Matches(ids).Select(c => long.Parse(c.Value)).Distinct().Select(c => new
            {
                playabilityStatus = "Playable",
                isPlayable = true,
                universeId = c,
            });
        }
        [HttpGet("v1/games")]
        public async Task<dynamic> MultiGetUniverseInfo(string universeIds)
        {
            var sp = universeIds.Split(",").Select(long.Parse);
            var result = await services.games.MultiGetUniverseInfo(sp);
            return new
            {
                data = result,
            };
        }
        [HttpGetBypass("v1/games/sorts")]
        public dynamic GameSort()
        {
            string rawJson = System.IO.File.ReadAllText(Configuration.JsonDataDirectory + "GameSort.json");
            dynamic? json = JsonConvert.DeserializeObject<ExpandoObject>(rawJson);
            return json ?? new ExpandoObject();
        }
        [HttpGetBypass("v1/name-description/games/{universeId:long}")]
        public async Task<dynamic> GetGameDesc(long universeId)
        {
            var uni = (await services.games.MultiGetUniverseInfo(new[] {universeId})).FirstOrDefault();
            return new
            {
                data = new[]
                {
                    new
                    {
                        name = uni.name,
                        description = uni.description,
                        languageCode = "en"
                    }
                }
            };
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