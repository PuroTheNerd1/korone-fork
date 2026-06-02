using Roblox.ApiProxy.Configuration;
using Roblox.ApiProxy.Middleware;
using Roblox.ServiceDefaults;
using Roblox.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddRobloxServiceDefaults("Roblox.ApiProxy", ServiceExposure.PublicService);
builder.Services.Configure<FrontendProxyOptions>(
    builder.Configuration.GetSection(FrontendProxyOptions.SectionName));
builder.Services.AddHttpForwarder();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseRobloxServiceDefaults(ServiceExposure.PublicService);
app.UseMiddleware<FrontendProxyMiddleware>();
app.MapReverseProxy();

app.Run();
