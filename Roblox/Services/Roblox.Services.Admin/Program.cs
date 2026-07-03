using Microsoft.AspNetCore.Http.Features;
using Roblox.ServiceDefaults;
using Roblox.Services.Admin.HostedServices;
using Roblox.Services.App.FeatureFlags;
using Roblox.Web.Infrastructure;
using Roblox.Web.Infrastructure.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.AddRobloxServiceDefaults("Roblox.Services.Admin", ServiceExposure.InternalService);
await FeatureFlags.RefreshOnceAsync();
builder.Services.AddSingleton<IAdminStaffAuthorizationService, AdminStaffAuthorizationService>();
builder.Services.AddSingleton<IAdminTwoFactorStore, AdminTwoFactorStore>();
builder.Services.AddHostedService<FeatureFlagRefreshHostedService>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
});

var app = builder.Build();

app.UseRobloxServiceDefaults(ServiceExposure.InternalService);
app.MapControllers();

app.Run();

public partial class Program
{
}
