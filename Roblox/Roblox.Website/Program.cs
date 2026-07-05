using Roblox.Website.Startup;
using Roblox.Web.Infrastructure.Http;

var domain = AppDomain.CurrentDomain;
domain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(5));

var builder = WebApplication.CreateBuilder(args);

builder.InitializeLegacyConfiguration();
await RobloxIpHasher.InitializeIpHashSetupAsync();
builder.Services.AddRobloxWebsiteServices(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.RunDevelopmentBootstrapAsync();
app.UseRobloxWebsitePipeline();
app.MapRobloxWebsiteEndpoints();

app.Run();
