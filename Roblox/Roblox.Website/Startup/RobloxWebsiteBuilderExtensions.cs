using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.OpenApi.Models;
using Roblox;
using Roblox.Dto.Users;
using Roblox.Rendering;
using Roblox.Services;
using Roblox.Services.App.FeatureFlags;
using Roblox.Web.Infrastructure.Extensions;
using Roblox.Website.ExceptionHandlers;
using Roblox.Website.HostedServices;
using Roblox.Website.Middleware;

namespace Roblox.Website.Startup;

public static class RobloxWebsiteBuilderExtensions
{
    public static void InitializeLegacyConfiguration(this WebApplicationBuilder builder)
    {
        var configuration = builder.Configuration;

        Roblox.Services.Database.Configure(configuration.GetSection("Postgres").Value!);
        Roblox.Services.Cache.Configure(configuration.GetSection("Redis").Value!, configuration.GetSection("RedisAuthentication").Value!);

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
        Roblox.Configuration.RobloxClientId = configuration.GetSection("Roblox:ClientId").Value!;
        Roblox.Configuration.RobloxClientSecret = configuration.GetSection("Roblox:ClientSecret").Value!;
        Roblox.Configuration.DiscordClientId = configuration.GetSection("Discord:ClientId").Value!;
        Roblox.Configuration.DiscordClientSecret = configuration.GetSection("Discord:ClientSecret").Value!;
        Roblox.Configuration.DiscordGuildId = configuration.GetSection("Discord:GuildId").Value!;
        Roblox.Configuration.DiscordBotToken = configuration.GetSection("Discord:BotToken").Value!;
        Roblox.Configuration.DiscordLogChannelId = configuration.GetSection("Discord:LogChannelId").Value!;
        Roblox.Configuration.DiscordLockChannelId = configuration.GetSection("Discord:LockChannelId").Value!;
        Roblox.Configuration.DiscordApplicationCallback = Roblox.Configuration.BaseUrl + configuration.GetSection("Discord:ApplicationCallback").Value;
        Roblox.Configuration.DiscordLoginCallback = Roblox.Configuration.BaseUrl + configuration.GetSection("Discord:LoginCallback").Value;
        Roblox.Configuration.DiscordLinkCallback = Roblox.Configuration.BaseUrl + configuration.GetSection("Discord:LinkCallback").Value;
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

        var gameServerConfig = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("game-servers.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        Roblox.Configuration.GameServerIpAddresses = gameServerConfig.GetSection("GameServers").Get<IEnumerable<GameServerConfigEntry>>() ?? Enumerable.Empty<GameServerConfigEntry>();

        Roblox.Configuration.AssetValidationServiceUrl = configuration.GetSection("AssetValidation:BaseUrl").Value!;
        Roblox.Configuration.AssetValidationServiceAuthorization = configuration.GetSection("AssetValidation:Authorization").Value!;
        GameServerService.Configure(string.Join(Guid.NewGuid().ToString(), new int[16].Select(_ => Guid.NewGuid().ToString())));
        Roblox.Configuration.PackageShirtAssetId = long.Parse(configuration.GetSection("PackageShirtAssetId").Value!);
        Roblox.Configuration.PackagePantsAssetId = long.Parse(configuration.GetSection("PackagePantsAssetId").Value!);
        Roblox.Libraries.TwitterApi.TwitterApi.Configure(configuration.GetSection("Twitter:Bearer").Value!);
        Roblox.Configuration.SignupAssetIds = configuration.GetSection("SignupAssetIds").GetChildren().Select(assetIdStr => long.Parse(assetIdStr.Value!));
        Roblox.Configuration.SignupAvatarAssetIds = configuration.GetSection("SignupAvatarAssetIds").GetChildren().Select(c => long.Parse(c.Value!));

#if DEBUG
        Roblox.Configuration.RobloxAppPrefix = "rbxeconsimdev:";
#endif

        var ownerUserIds = configuration.GetSection("OwnerUserId").Get<List<long>>()!;
        Roblox.Website.Filters.StaffFilter.Configure(ownerUserIds);

        ApplicationGuardMiddleware.Configure(configuration.GetSection("Authorization").Value!);
        CsrfMiddleware.Configure(Guid.NewGuid() + Guid.NewGuid().ToString() + Guid.NewGuid());
        SessionMiddleware.Configure(configuration.GetSection("Jwt:Sessions").Value!);
        CommandHandler.Configure(configuration.GetSection("Render:BaseUrl").Value, configuration.GetSection("Render:Authorization").Value);
        Roblox.Services.Signer.SignService.Setup();

#if DEBUG
        RenderingHandler.Configure("game-renderer");
#else
        RenderingHandler.Configure("127.0.0.1");
#endif
    }

    public static IServiceCollection AddRobloxWebsiteServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddRazorPages();
        services.AddRobloxWebInfrastructure(configuration);
        services.AddRequestDecompression();
        services.AddControllers(options =>
            {
                options.InputFormatters.Add(new XmlSerializerInputFormatter(options));
                options.RespectBrowserAcceptHeader = true;
            })
            .AddJsonOptions(o =>
            {
                o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                o.JsonSerializerOptions.PropertyNamingPolicy = null;
            });

        services.Configure<FormOptions>(options =>
        {
            options.ValueLengthLimit = int.MaxValue;
            options.MultipartBodyLengthLimit = long.MaxValue;
        });

        services.AddSignalR();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
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
            var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
        });
        services.AddMvc(c => c.Conventions.Add(new ApiExplorerGetsOnlyConvention()));

        services.AddSingleton<R2StorageService>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        services.AddHostedService<FeatureFlagRefreshHostedService>();
        services.AddHostedService<GameRecommendationHostedService>();
        services.AddHostedService<GameTopicBackfillHostedService>();
        services.AddHostedService<AvatarThumbnailCleanupHostedService>();

        return services;
    }
}
