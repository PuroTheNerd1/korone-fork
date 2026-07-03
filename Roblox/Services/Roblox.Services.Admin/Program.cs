using Roblox.ServiceDefaults;
using Roblox.Web.Infrastructure;
using Roblox.Web.Infrastructure.Admin;

var builder = WebApplication.CreateBuilder(args);

builder.AddRobloxServiceDefaults("Roblox.Services.Admin", ServiceExposure.InternalService);
builder.Services.AddSingleton<IAdminStaffAuthorizationService, AdminStaffAuthorizationService>();
builder.Services.AddSingleton<IAdminTwoFactorStore, AdminTwoFactorStore>();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var app = builder.Build();

app.UseRobloxServiceDefaults(ServiceExposure.InternalService);
app.MapControllers();

app.Run();

public partial class Program
{
}
