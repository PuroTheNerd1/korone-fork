using Roblox.ApiProxy.Configuration;
using Roblox.ApiProxy.Middleware;
using Roblox.ServiceDefaults;
using Roblox.Web.Infrastructure.Admin;
using Roblox.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddRobloxServiceDefaults("Roblox.ApiProxy", ServiceExposure.PublicService);
builder.Services.AddSingleton<IAdminSessionResolver, AdminSessionResolver>();
builder.Services.AddSingleton<IAdminStaffAuthorizationService, AdminStaffAuthorizationService>();
builder.Services.AddSingleton<IAdminTwoFactorStore, AdminTwoFactorStore>();
builder.Services.Configure<AdminFrontendOptions>(
    builder.Configuration.GetSection(AdminFrontendOptions.SectionName));
builder.Services.Configure<FrontendProxyOptions>(
    builder.Configuration.GetSection(FrontendProxyOptions.SectionName));
builder.Services.AddHttpForwarder();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseRobloxServiceDefaults(ServiceExposure.PublicService);
app.UseMiddleware<AdminFrontendMiddleware>();
app.UseMiddleware<FrontendProxyMiddleware>();
app.MapReverseProxy();

app.Run();

public partial class Program
{
}
