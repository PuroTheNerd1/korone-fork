using MVC = Microsoft.AspNetCore.Mvc;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]   
    public class LauncherController : ControllerBase
    {
        private static string RBXversion = "version-622e5002057946a";
        private static string RBXversionstudio = "version-cbge7ed28c0dc9d2";
        private static string CDN = $"C:\\ProjectX\\services\\Roblox\\Setup\\";
        private static string RBXClientPath = $"C:\\ProjectX\\services\\Roblox\\Setup\\Client\\{RBXversion}";
        private static string RBXStudioPath = $"C:\\ProjectX\\services\\Roblox\\Setup\\Studio\\{RBXversionstudio}";
        [HttpGetBypass("/v2/client-version/WindowsPlayer/channel/LIVE")]
        public dynamic GetVersionV2()
        {
            dynamic json = new
            {
                clientVersionUpload = RBXversion,
                bootstrapperVersion = "1, 7, 0, 0"
            };
            return Ok(json);
        }
        [HttpGetBypass("/cdn/version")]
        public dynamic GetVersion()
        {
            return Ok(RBXversion);
        }
        [HttpGetBypass("/cdn/ProjectXPlayerLauncher.exe")]
        public MVC.IActionResult GetLauncher()
        {
            return PhysicalFile("C:\\ProjectX\\services\\Roblox\\Setup\\ProjectXPlayerLauncher1.exe", "application/octet-stream");
        }

        [HttpGetBypass("/cdn/versionQTStudio")]
        public dynamic GetVersionQT()
        {
            return Ok(RBXversionstudio);
        }

        [HttpGetBypass("cdn/{*file}")]
        public MVC.IActionResult GetCDNFile(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return NotFound();
            }
            string NormalCDN = Path.Combine(CDN, file);
            string ClientStrapper = Path.Combine(RBXClientPath, file);
            string StudioStrapper = Path.Combine(RBXStudioPath, file);

            if (System.IO.File.Exists(NormalCDN))
            {
                return PhysicalFile(NormalCDN, "application/octet-stream");
            }
            else if (System.IO.File.Exists(ClientStrapper))
            {
                return PhysicalFile(ClientStrapper, "application/octet-stream");
            }
            else if (System.IO.File.Exists(StudioStrapper))
            {
                return PhysicalFile(StudioStrapper, "application/octet-stream");
            }
            else
            {
                return NotFound();
            }
        }        
    }    
}