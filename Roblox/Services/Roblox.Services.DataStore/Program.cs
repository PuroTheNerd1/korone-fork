using Roblox.ServiceDefaults;
using Roblox.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddRobloxServiceDefaults("Roblox.Services.DataStore", ServiceExposure.InternalService);
builder.Services.AddControllers();

var app = builder.Build();

app.UseRobloxServiceDefaults(ServiceExposure.InternalService);
app.MapControllers();

app.Run();
