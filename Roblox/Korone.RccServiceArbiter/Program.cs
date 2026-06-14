using Korone.RccServiceArbiter.Configuration;
using Korone.RccServiceArbiter.Processes;
using Korone.RccServiceArbiter.Rcc;
using Korone.RccServiceArbiter.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Roblox.ServiceDefaults;
using Roblox.Web.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddRobloxServiceDefaults("Korone.RccServiceArbiter", ServiceExposure.InternalService);
builder.Services.Configure<HealthCheckServiceOptions>(options =>
{
    var dependencies = options.Registrations.FirstOrDefault(registration => registration.Name == "dependencies");
    if (dependencies != null)
    {
        options.Registrations.Remove(dependencies);
    }
});
builder.Services.AddHealthChecks()
    .AddCheck("arbiter", () => HealthCheckResult.Healthy(), tags: new[] { "ready" });
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddOptions<ArbiterOptions>()
    .Bind(builder.Configuration.GetSection("Arbiter"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton<IArbiterClock, SystemArbiterClock>();
builder.Services.AddSingleton<IPortAllocator, PortAllocator>();
builder.Services.AddSingleton<IRccProcessLauncher, RccProcessLauncher>();
builder.Services.AddSingleton<IRccReadinessProbe, TcpRccReadinessProbe>();
builder.Services.AddHttpClient<IRccSoapClientFactory, RccSoapClientFactory>();
builder.Services.AddSingleton<IRccJsonPayloadFactory, RccJsonPayloadFactory>();
builder.Services.AddSingleton<IArbiterPostStartQueue, ArbiterPostStartQueue>();
builder.Services.AddSingleton<IRccProcessPool, RccProcessPool>();
builder.Services.AddHostedService<RccProcessCleanupService>();
builder.Services.AddHostedService<ArbiterPostStartWorker>();

var app = builder.Build();

app.UseRobloxServiceDefaults(ServiceExposure.InternalService);
app.MapControllers();

app.Run();

public partial class Program
{
}
