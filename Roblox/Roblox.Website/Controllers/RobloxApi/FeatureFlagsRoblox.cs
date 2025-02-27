using MVC = Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;
using BadRequestException = Roblox.Exceptions.BadRequestException;
using Roblox.Services.Exceptions;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class FeatureFlagsRoblox: ControllerBase
    {
        [HttpPostBypass("Setting/Get/{type}")]
        [HttpPostBypass("Setting/QuietGet/{type}")]
        [HttpGetBypass("Setting/Get/{type}")]
        [HttpGetBypass("Setting/QuietGet/{type}")]
        [HttpPostBypass("v2/settings/application")]
        [HttpGetBypass("v2/settings/application")]
        [HttpPostBypass("v1/settings/application")]
        [HttpGetBypass("v1/settings/application")]
        public MVC.ActionResult<dynamic> GetAppSettingsNew(string? type, string? applicationName, string? apiKey)
        {
            applicationName = applicationName ?? type;
            if (applicationName == null)
                throw new BadRequestException(1, $"Bad Request");
            return GetFeatureFlags(applicationName, apiKey);
        }


        // For legacy clients
        private static readonly Dictionary<string, string> apiKeys = new Dictionary<string, string>()
        {
            { "9CE2063F-BB45-449B-89D4-65CD2ED806CD", "RCCServiceUJ38BA31M8F47VA76XZ1RYONSSTILA3F" }, // 2017L RCC
            { "D6925E56-BFB9-4908-AAA2-A5B1EC4B2D79", "ClientAppSettings2017" },
            { "D6925E56-BFB9-4908-AAA2-A5B1EC4B2D79", "StudioAppSettings" },
            { "08BF6621-8100-4484-B14C-87497E372160", "ClientAppSettings2017" }, // 2017L Studio + Client
            { "D6925E56-BFB9-4908-AAA2-A5B1EC4B2D7A", "RCCService2018" }, // 2018L RCC
            { "76E5A40C-3AE1-4028-9F10-7C62520BD94F", "ClientAppSettings2018" },
            { "19C0B314-AC23-4CD4-8A37-02C4140F7240", "ClientAppSettings2018" } // 2018L AppSettings
        };
        // For modern clients
        private static readonly HashSet<string> applicationNames = new HashSet<string>
        {
            "RCCService2019",
            "PCDesktopClient2019",
            "RCCService2020",
            "PCStudioApp",
            "PCStudio221",
            "RCCService2021",
            "PCDesktopClient",
            "PCDesktopClient2021",
            "AndroidApp",
            "iOSApp"
        };
        private string GetTypeForApiKey(string type, string apiKey)
        {
            apiKeys.TryGetValue(apiKey, out string? newType);

            if (newType == null || newType != type)
                throw new BadRequestException(0, $"Invalid API key: {apiKey}");

            return type;
        }
        private string GetFeatureFlags(string type, string? apiKey = null)
        {
            /*
                The legacy clients use an API key and a type to get the feature flags
                Modern clients only use the type.
                Here we do a few sanity checks to make sure the request is valid.
            */
            if (apiKey != null)
                type = GetTypeForApiKey(type, apiKey);
            else if (!applicationNames.TryGetValue(type, out type))
                throw new BadRequestException(1, $"Invalid application name: {type}");
            else 
                // Should never happen, but just in case
                throw new BadRequestException(1, $"Bad Request");

            string featureFlags = Path.Join(Configuration.JsonDataDirectory, $"{type}.json");
            
            // Also should never happen, but just in case
            if (!System.IO.File.Exists(featureFlags))
                return "{}";

            return System.IO.File.ReadAllText(featureFlags);
        }
    }
}