using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using System;

namespace Roblox.Website.Controllers
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class BotAuthorizationAttribute : Attribute, IFilterFactory
    {
        public bool IsReusable => true;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new BotAuthorizationFilter();
        }
    }

    public class BotAuthorizationFilter : IActionFilter
    {
        private const string BotAuthKey = "ljbHjhLvOwPGasmd1qBoa4qkkbcqa1tT39BImr5SvZFbqQXi133GruGL2O2U06906ezZ8pmwEAv33SM5KmWk";

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Request.Headers.TryGetValue("PJX-BOTAUTH", out StringValues botKey))
            {
                if (botKey != BotAuthKey)
                {
                    context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                }
            }
            else
            {
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
    }
}
