using Roblox.Libraries.DiscordApi;
using Roblox.Libraries.LeakCheckApi;
using Roblox.Libraries.RobloxApi;
using Roblox.Services;
using Roblox.Services.AI;
using Roblox.Services.Games;
using Roblox.Services.PlaceLauncher;
using Roblox.Services.Signer;

namespace Roblox.Web.Infrastructure.Services;

public class RobloxServiceAccessor : IDisposable
{
    private readonly List<IDisposable> _ownedServices = new();
    private bool _disposed;

    private AssetsService? _assets;
    private PromocodesService? _promocodes;
    private RobloxAssetService? _robloxAssetCache;
    private UsersService? _users;
    private AccountInformationService? _accountInformation;
    private AvatarService? _avatar;
    private FriendsService? _friends;
    private GamesService? _games;
    private PlayerSecurityService? _playerSecurity;
    private BadgesService? _badges;
    private GroupsService? _groups;
    private InventoryService? _inventory;
    private PrivateMessagesService? _privateMessages;
    private ThumbnailsService? _thumbnails;
    private TradesService? _trades;
    private GameServerService? _gameServer;
    private SetsService? _sets;
    private PlaceLauncherService? _placeLauncher;
    private SignService? _sign;
    private ForumsService? _forums;
    private CurrencyExchangeService? _currencyExchange;
    private AbuseReportService? _abuseReport;
    private EconomyService? _economy;
    private PurchaseAttestationService? _purchaseAttestation;
    private CooldownService? _cooldown;
    private FilterService? _filter;
    private ChatService? _chat;
    private OpenRouterService? _openRouter;
    private GameTopicService? _gameTopic;
    private GameRecommendationService? _gameRecommendation;
    private DataStoreService? _dataStore;
    private R2StorageService? _r2Storage;
    private RobloxApi? _robloxApi;
    private LeakCheckApi? _leakCheck;
    private DiscordBotApi? _discordBotApi;

    public AssetsService assets => GetService(ref _assets);
    public PromocodesService promocodes => GetService(ref _promocodes);
    public RobloxAssetService robloxAssetCache => GetService(ref _robloxAssetCache);
    public UsersService users => GetService(ref _users);
    public AccountInformationService accountInformation => GetService(ref _accountInformation);
    public AvatarService avatar => GetService(ref _avatar);
    public FriendsService friends => GetService(ref _friends);
    public GamesService games => GetService(ref _games);
    public PlayerSecurityService playerSecurity => GetService(ref _playerSecurity);
    public BadgesService badges => GetService(ref _badges);
    public GroupsService groups => GetService(ref _groups);
    public InventoryService inventory => GetService(ref _inventory);
    public PrivateMessagesService privateMessages => GetService(ref _privateMessages);
    public ThumbnailsService thumbnails => GetService(ref _thumbnails);
    public TradesService trades => GetService(ref _trades);
    public GameServerService gameServer => GetOwned(ref _gameServer, static () => new GameServerService());
    public SetsService sets => GetOwned(ref _sets, static () => new SetsService());
    public PlaceLauncherService placeLauncher => GetOwned(ref _placeLauncher, static () => new PlaceLauncherService());
    public SignService sign => GetOwned(ref _sign, static () => new SignService());
    public ForumsService forums => GetOwned(ref _forums, static () => new ForumsService());
    public CurrencyExchangeService currencyExchange => GetService(ref _currencyExchange);
    public AbuseReportService abuseReport => GetService(ref _abuseReport);
    public EconomyService economy => GetService(ref _economy);
    public PurchaseAttestationService purchaseAttestation => GetService(ref _purchaseAttestation);
    public CooldownService cooldown => GetService(ref _cooldown);
    public FilterService filter => GetService(ref _filter);
    public ChatService chat => GetService(ref _chat);
    public OpenRouterService openRouter => GetService(ref _openRouter);
    public GameTopicService gameTopic => GetService(ref _gameTopic);
    public GameRecommendationService gameRecommendation => GetService(ref _gameRecommendation);
    public DataStoreService dataStore => GetService(ref _dataStore);
    public R2StorageService r2Storage => GetService(ref _r2Storage);
    public RobloxApi robloxApi => _robloxApi ??= new RobloxApi();
    public LeakCheckApi leakCheck => _leakCheck ??= new LeakCheckApi(Roblox.Configuration.LeakCheckApiKey);
    public DiscordBotApi discordBotApi => _discordBotApi ??= new DiscordBotApi(Roblox.Configuration.DiscordBotToken);

    private T GetService<T>(ref T? field) where T : ServiceBase, IDisposable, IService, new()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (field != null)
        {
            return field;
        }

        field = ServiceProvider.GetOrCreate<T>();
        if (!field.IsReusable() || !field.IsThreadSafe())
        {
            _ownedServices.Add(field);
        }

        return field;
    }

    private T GetOwned<T>(ref T? field, Func<T> factory) where T : class, IDisposable
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (field != null)
        {
            return field;
        }

        field = factory();
        _ownedServices.Add(field);
        return field;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var service in _ownedServices.Distinct())
        {
            service.Dispose();
        }

        _ownedServices.Clear();
    }
}
