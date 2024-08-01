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
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class UniversalAppConfiguration: ControllerBase
    {
        [HttpGetBypass("universal-app-configuration/v1/behaviors/app-patch/content")]
        public dynamic AppPatch()
        {
            List<long> CanaryUserIds = new List<long>();
            return new 
            {
                SchemeVersion = "1",
                CanaryUserIds,
                CanaryPercentage = 0,
            };
        }
        [HttpGetBypass("universal-app-configuration/v1/behaviors/app-policy/content")]
        public dynamic AppPolicy()
        {
            string policyContent = System.IO.File.ReadAllText(Configuration.JsonDataDirectory + "AppPolicy.json");
            dynamic? policyJson = JsonConvert.DeserializeObject(policyContent);
            return policyJson ?? "";
        }
    }
}