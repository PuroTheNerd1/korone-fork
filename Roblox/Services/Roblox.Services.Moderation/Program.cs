using Roblox.ServiceDefaults;
using Roblox.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddRobloxServiceDefaults("Roblox.Services.Moderation", ServiceExposure.InternalService);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var app = builder.Build();

app.UseRobloxServiceDefaults(ServiceExposure.InternalService);
app.MapControllers();

app.Run();
