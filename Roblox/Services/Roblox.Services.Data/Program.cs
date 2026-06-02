using Roblox.ServiceDefaults;
using Roblox.Services.App.FeatureFlags;
using Roblox.Services.Data;
using Roblox.Services.Data.ExceptionHandlers;
using Roblox.Services.Data.HostedServices;
using Roblox.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddRobloxServiceDefaults("Roblox.Services.Data", ServiceExposure.InternalService);
DataServiceConfiguration.Initialize(builder.Configuration);
await FeatureFlags.RefreshOnceAsync();

builder.Services.AddHostedService<FeatureFlagRefreshHostedService>();
builder.Services.AddExceptionHandler<DataServiceExceptionHandler>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var app = builder.Build();

app.UseRobloxServiceDefaults(ServiceExposure.InternalService);
app.MapControllers();

app.Run();
