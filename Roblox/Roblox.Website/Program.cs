using Roblox.Website.Startup;

var domain = AppDomain.CurrentDomain;
domain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(5));

var builder = WebApplication.CreateBuilder(args);

builder.InitializeLegacyConfiguration();
builder.Services.AddRobloxWebsiteServices(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.RunDevelopmentBootstrapAsync();
app.UseRobloxWebsitePipeline();
app.MapRobloxWebsiteEndpoints();

app.Run();
