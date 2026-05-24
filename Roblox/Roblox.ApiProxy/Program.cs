using Roblox.ServiceDefaults;
using Roblox.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddRobloxServiceDefaults("Roblox.ApiProxy", ServiceExposure.PublicService);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseRobloxServiceDefaults(ServiceExposure.PublicService);
app.MapReverseProxy();

app.Run();
