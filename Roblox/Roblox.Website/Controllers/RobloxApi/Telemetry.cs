
using MVC = Microsoft.AspNetCore.Mvc;
using Roblox.Website.Controllers.Internal;
using Microsoft.AspNetCore.Mvc;
namespace Roblox.Website.Controllers
{

    [MVC.ApiController]
    [MVC.Route("/")]
    public class Telementry : ControllerBase
    {
        [HttpGetBypass("client/pbe")]
        [HttpGetBypass("mobile/pbe")]
        public OkResult PBE()
        {
            return Ok();
        }
        [HttpPostBypass("v1/enrollments")]
        public dynamic Enrollments()
        {
            return new 
            {
                data = new[]
                {
                    new
                    {
                        SubjectType = "BrowserTracker",
                        SubjectTargetId = 63713166375,
                        ExperimentName = "AllUsers.DevelopSplashScreen.GreenStartCreatingButton",
                        Status = "Inactive",
                        Variation = (string)null
                    }
                }
            };
        }
        [HttpPostBypass("v1/get-enrollments")]
        public dynamic GetEnrollments()
        {
            return Array.Empty<object>();
        }
    }
}