using MVC = Microsoft.AspNetCore.Mvc;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]   
    public class LauncherController : ControllerBase
    {
        private static string RBXversion = "version-03c7c0ace395d80b";
        private static string RBXPath = $"C:\\ProjectX\\services\\Roblox\\Setup\\{RBXversion}\\";
        [HttpGetBypass("/cdn/version")]
        public dynamic GetVersion()
        {
            return Ok(RBXversion);
        }
        [HttpGetBypass("cdn/{*path}")]
        public MVC.IActionResult GetCDNFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return NotFound();
            }

            string RBXFILE = Path.Combine(RBXPath, path);

            if (System.IO.File.Exists(RBXFILE))
            {
                return PhysicalFile(RBXFILE, "application/octet-stream");
            }
            else
            {
                return NotFound();
            }
        }        
    }    
}