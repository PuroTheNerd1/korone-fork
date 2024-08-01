using MVC = Microsoft.AspNetCore.Mvc;
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
    public class WellKnown: ControllerBase
    {
        [HttpGetBypass(".well-known/discord")]
        public string WellKnownDiscord()
        {
            string hostName = HttpContext.Request.Host.Host;
            
            if (hostName.Contains("phil564"))
            {
                return "dh=20873664bf98d2f31ebdd1c2df0ddd62821db03b";
            }
            return "dh=20873664bf98d2f31ebdd1c2df0ddd62821db03b";
        }
    }
}
