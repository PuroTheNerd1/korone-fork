using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Roblox;
using Roblox.Dto.Users;
using Roblox.Rendering;
using Roblox.Services;
using Roblox.Services.App.FeatureFlags;
using Roblox.Website.Hubs;
using Roblox.Website.Middleware;
using System.Reflection;
using System.Text.Json.Serialization;
using Roblox.Website.ExceptionHandlers;

var domain = AppDomain.CurrentDomain;
// Set a timeout interval of 5 seconds.
domain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", TimeSpan.FromSeconds(5));

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var builder = WebApplication.CreateBuilder(args);

// DB
Roblox.Services.Database.Configure(configuration.GetSection("Postgres").Value!);
Roblox.Services.Cache.Configure(configuration.GetSection("Redis").Value!, configuration.GetSection("RedisAuthentication").Value!);

// Config
Roblox.Configuration.CdnBaseUrl = configuration.GetSection("CdnBaseUrl").Value!;
Roblox.Configuration.AssetDirectory = configuration.GetSection("Directories:Asset").Value!;
Roblox.Configuration.StorageDirectory = configuration.GetSection("Directories:Storage").Value!;
Roblox.Configuration.ThumbnailsDirectory = configuration.GetSection("Directories:Thumbnails").Value!;
Roblox.Configuration.GroupIconsDirectory = configuration.GetSection("Directories:GroupIcons").Value!;
Roblox.Configuration.PublicDirectory = configuration.GetSection("Directories:Public").Value!;
Roblox.Configuration.XmlTemplatesDirectory = configuration.GetSection("Directories:XmlTemplates").Value!;
Roblox.Configuration.JsonDataDirectory = configuration.GetSection("Directories:JsonData").Value!;
Roblox.Configuration.ScriptDirectory = configuration.GetSection("Directories:ScriptsData").Value!;
Roblox.Configuration.AdminBundleDirectory = configuration.GetSection("Directories:AdminBundle").Value!;
Roblox.Configuration.EconomyChatBundleDirectory = configuration.GetSection("Directories:EconomyChatBundle").Value!;
Roblox.Configuration.BaseUrl = configuration.GetSection("BaseUrl").Value!;
Roblox.Configuration.ShortBaseUrl = Roblox.Configuration.BaseUrl!.Replace("https", "http").Replace("http://www.", "");
Roblox.Configuration.HCaptchaPublicKey = configuration.GetSection("HCaptcha:Public").Value!;
Roblox.Configuration.HCaptchaPrivateKey = configuration.GetSection("HCaptcha:Private").Value!;
Roblox.Configuration.IsCdnEnabled = bool.Parse(configuration.GetSection("IsCdnEnabled").Value ?? "false");
Roblox.Configuration.HmacSecret = configuration.GetSection("HmacSecret").Value!;
Roblox.Configuration.R2AccountId = configuration.GetSection("CloudflareR2:AccountId").Value!;
Roblox.Configuration.R2AccessKey = configuration.GetSection("CloudflareR2:AccessKey").Value!;
Roblox.Configuration.R2SecretKey = configuration.GetSection("CloudflareR2:SecretKey").Value!;
Roblox.Configuration.R2BucketName = configuration.GetSection("CloudflareR2:BucketName").Value!;
// roblox oauth stuff
Roblox.Configuration.RobloxClientId = configuration.GetSection("Roblox:ClientId").Value!;
Roblox.Configuration.RobloxClientSecret = configuration.GetSection("Roblox:ClientSecret").Value!;
// Discord OAuth related Stuff
Roblox.Configuration.DiscordClientId = configuration.GetSection("Discord:ClientId").Value!;
Roblox.Configuration.DiscordClientSecret = configuration.GetSection("Discord:ClientSecret").Value!;
Roblox.Configuration.DiscordGuildId = configuration.GetSection("Discord:GuildId").Value!;
Roblox.Configuration.DiscordBotToken = configuration.GetSection("Discord:BotToken").Value!;
Roblox.Configuration.DiscordLogChannelId = configuration.GetSection("Discord:LogChannelId").Value!;
Roblox.Configuration.DiscordLockChannelId = configuration.GetSection("Discord:LockChannelId").Value!;
Roblox.Configuration.DiscordApplicationCallback = Roblox.Configuration.BaseUrl + configuration.GetSection("Discord:ApplicationCallback").Value;
Roblox.Configuration.DiscordLoginCallback = Roblox.Configuration.BaseUrl + configuration.GetSection("Discord:LoginCallback").Value;
Roblox.Configuration.DiscordLinkCallback = Roblox.Configuration.BaseUrl + configuration.GetSection("Discord:LinkCallback").Value;
// Leakcheck
Roblox.Configuration.LeakCheckApiKey = configuration.GetSection("LeakCheckApiKey").Value!;
Roblox.Configuration.GameServerAuthorization = configuration.GetSection("GameServerAuthorization").Value!;
Roblox.Configuration.BotAuthorization = configuration.GetSection("BotAuthorization").Value!;
Roblox.Configuration.RccAuthorization = configuration.GetSection("RccAuthorization").Value!;
Roblox.Configuration.RobloxAuthorization = configuration.GetSection("RobloxAuthorization").Value!;
Roblox.Configuration.ArbiterAuthorization = configuration.GetSection("ArbiterAuthorization").Value!;
Roblox.Configuration.GameServerIp = configuration.GetSection("GameServerIp").Value!;
Roblox.Configuration.UserAgentBypassSecret = configuration.GetSection("UserAgentBypassSecret").Value!;
Roblox.Configuration.InvisibleTurnstileSiteKey = configuration.GetSection("InvisibleTurnstile:SiteKey").Value ?? "";
Roblox.Configuration.InvisibleTurnstileSecretKey = configuration.GetSection("InvisibleTurnstile:SecretKey").Value ?? "";
Roblox.Configuration.OpenRouterApiKey = configuration.GetSection("AI:OpenRouterAPIKey").Value ?? "";
Roblox.Configuration.VerificationSecret = configuration.GetSection("VerificationSecret").Value!;
Roblox.Configuration.LuaScriptsDirectory = configuration.GetSection("Directories:RCCLuaScripts").Value!;
IConfiguration gameServerConfig = new ConfigurationBuilder()
    .AddJsonFile("game-servers.json", optional: true)
    .AddEnvironmentVariables()
    .Build();
Roblox.Configuration.GameServerIpAddresses = gameServerConfig.GetSection("GameServers").Get<IEnumerable<GameServerConfigEntry>>() ?? Enumerable.Empty<GameServerConfigEntry>();
Roblox.Configuration.AssetValidationServiceUrl =
    configuration.GetSection("AssetValidation:BaseUrl").Value!;
Roblox.Configuration.AssetValidationServiceAuthorization =
    configuration.GetSection("AssetValidation:Authorization").Value!;
GameServerService.Configure(string.Join(Guid.NewGuid().ToString(), new int [16].Select(_ => Guid.NewGuid().ToString()))); // More TODO: If we every load balance, this will break
Roblox.Configuration.PackageShirtAssetId = long.Parse(configuration.GetSection("PackageShirtAssetId").Value!);
Roblox.Configuration.PackagePantsAssetId = long.Parse(configuration.GetSection("PackagePantsAssetId").Value!);
Roblox.Libraries.TwitterApi.TwitterApi.Configure(configuration.GetSection("Twitter:Bearer").Value!);
// Sign up asset ids
var assetIdsStart = configuration.GetSection("SignupAssetIds").GetChildren().Select(assetIdStr => long.Parse(assetIdStr.Value!));
Roblox.Configuration.SignupAssetIds = assetIdsStart;
Roblox.Configuration.SignupAvatarAssetIds =
    configuration.GetSection("SignupAvatarAssetIds").GetChildren().Select(c => long.Parse(c.Value!));
#if DEBUG
Roblox.Configuration.RobloxAppPrefix = "rbxeconsimdev:";
#endif
FeatureFlags.StartUpdateFlagTask();
Roblox.Services.Games.GameRecommendationService.StartPeriodicLoop();
Roblox.Services.Games.GameTopicService.StartBackfillLoop();
var ownerUserIdConfig = configuration.GetSection("OwnerUserId");
List<long> ownerUserIds = ownerUserIdConfig.Get<List<long>>()!;
Roblox.Website.Filters.StaffFilter.Configure(ownerUserIds!);
//Roblox.Website.Controllers.ThumbnailsControllerV1.StartThumbnailFixLoop();

builder.Services.AddRazorPages();
builder.Services.AddRequestDecompression();
builder.Services.AddControllers(options =>
{
    options.InputFormatters.Add(new XmlSerializerInputFormatter(options));
    options.RespectBrowserAcceptHeader = true;
})
.AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.JsonSerializerOptions.PropertyNamingPolicy = null;
});
// needed for datastores, values can be over 2048
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
});

builder.Services.AddSignalR();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.IgnoreObsoleteActions();
    c.IgnoreObsoleteProperties();
    c.CustomSchemaIds(type => type.FullName);
    c.EnableAnnotations();
    c.SwaggerDoc("UserV1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Users Api v1",
    });
    c.SchemaGeneratorOptions.SchemaIdSelector = type => type.ToString();
    c.OperationFilter<SwaggerFileOperationFilter>();
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});
builder.Services.AddMvc(c =>
    c.Conventions.Add(new ApiExplorerGetsOnlyConvention())
);

builder.Services.AddSingleton<Roblox.Services.R2StorageService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(); // required for the middleware pipeline

//if (Configuration.IsCdnEnabled)
//  builder.Services.AddHostedService<Roblox.Website.R2MigrationWorker>();

var app = builder.Build();
app.UseRouting();
app.UseSwaggerUI(c =>
{
    c.ShowCommonExtensions();

    c.SwaggerEndpoint("/swagger/UserV1/swagger.json", "UserV1");
});

var prepareResponseForCache = (StaticFileResponseContext ctx) =>
{
    const int durationInSeconds = 86400 * 365;
    ctx.Context.Response.Headers[HeaderNames.CacheControl] = "public,max-age=" + durationInSeconds;
    ctx.Context.Response.Headers.Remove(HeaderNames.LastModified);
};
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Roblox.Configuration.PublicDirectory + "css/Roblox/"),
    RequestPath = "/css",
    OnPrepareResponse = prepareResponseForCache,
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Roblox.Configuration.PublicDirectory + "js/"),
    RequestPath = "/js",
    OnPrepareResponse = prepareResponseForCache,
});
// Should be public
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Roblox.Configuration.PublicDirectory + "UnsecuredContent/"),
    RequestPath = "/UnsecuredContent",
    OnPrepareResponse = prepareResponseForCache,
});

// CdnBaseUrl is empty on dev servers
if (string.IsNullOrWhiteSpace(Roblox.Configuration.CdnBaseUrl))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Roblox.Configuration.ThumbnailsDirectory),
        RequestPath = "/images/thumbnails",
        OnPrepareResponse = prepareResponseForCache,
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Roblox.Configuration.GroupIconsDirectory),
        RequestPath = "/images/groups",
        OnPrepareResponse = prepareResponseForCache,
    });
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Roblox.Configuration.PublicDirectory + "img/"),
    RequestPath = "/img",
    OnPrepareResponse = prepareResponseForCache,
});

#if FALSE
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Roblox.Configuration.EconomyChatBundleDirectory),
    RequestPath = "/chat",
    ServeUnknownFileTypes = false,
    OnPrepareResponse = prepareResponseForCache,
});
#endif

app.UseRobloxSessionMiddleware();
if(!Configuration.IsCdnEnabled)
    app.UseMiddleware<ThumbnailMiddleware>(Roblox.Configuration.ThumbnailsDirectory);
//app.UseMiddleware<RobloxLoggingMiddleware>();
app.UseRobloxPlayerCorsMiddleware(); // cors varies depending on authentication status, so it must be after session middleware

app.UseRobloxCsrfMiddleware();
app.UseApplicationGuardMiddleware();
Roblox.Website.Middleware.ApplicationGuardMiddleware.Configure(configuration.GetSection("Authorization").Value!);
Roblox.Website.Middleware.CsrfMiddleware.Configure(Guid.NewGuid().ToString() + Guid.NewGuid().ToString() + Guid.NewGuid().ToString()); // TODO: This would break if we ever load balance

app.UseSwagger();
app.UseSwaggerUI();

app.UseMiddleware<FrontendProxyMiddleware>();
//app.UseMiddleware<RobloxLoggingMiddleware>();
//app.UseRobloxLoggingMiddleware();

//app.UseExceptionHandler("/error");
app.UseExceptionHandler();

// neva - unhardcoded for docker suppport
CommandHandler.Configure(configuration.GetSection("Render:BaseUrl").Value, configuration.GetSection("Render:Authorization").Value); // will be removed soon

#if DEBUG
if(app.Environment.IsDevelopment())
{
    var usersService = new Roblox.Services.UsersService();
    if(await usersService.CountCreatedUsers(null) == 0)
    {
        const string username = "ROBLOX";
        const string password = "roblox_dev_pass";
        string applicationId;
        string? joinId;
        try
        {
            applicationId = await usersService.CreateApplication(new CreateUserApplicationRequest
            {
                about = "Signed up",
                socialPresence = "",
                discordId = "95672431410151424",
                discordUsername = "bruteforcing",
                isVerified = true,
                verifiedUrl = null,
                verifiedId = null,
                verificationPhrase = "",
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
            });

            joinId = await usersService.ProcessApplication(applicationId, Configuration.AiUserId, UserApplicationStatus.Approved);
        }
        catch(Exception)
        {
            joinId = Guid.NewGuid().ToString();
        }
        var result = await usersService.CreateUser(username, password, Roblox.Models.Users.Gender.Male);
        await usersService.SetApplicationUserIdByJoinId(joinId!, result.userId);
        await usersService.InsertOrUpdateMembership(result.userId, Roblox.Models.Users.MembershipType.Premium);   
        Console.WriteLine("Created dev account: {0}:{1}", username, password);
    }
}
RenderingHandler.Configure("game-renderer");
#else
RenderingHandler.Configure("127.0.0.1");
#endif

SessionMiddleware.Configure(configuration.GetSection("Jwt:Sessions").Value!);
Roblox.Services.Signer.SignService.Setup();
app.UseTimerMiddleware(); // Must always be last
app.MapControllers();
app.MapRazorPages();
app.UseWebSockets();
app.UseRequestDecompression();
app.MapHub<MessageRouterHub>("/v1/router/signalr");
app.Run();


//_ = Task.Run(async () =>
//{
//    using var assets = Roblox.Services.ServiceProvider.GetOrCreate<AssetsService>();
//    await assets.FixAssetImagesWithoutMetadata();
//});
_ = Task.Run(async () =>
{
   AvatarService.StartTimerClear3D();
});
