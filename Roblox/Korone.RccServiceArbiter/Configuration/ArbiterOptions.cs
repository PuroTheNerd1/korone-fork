using System.ComponentModel.DataAnnotations;

namespace Korone.RccServiceArbiter.Configuration;

public sealed class ArbiterOptions
{
    [Required]
    public string PublicIp { get; set; } = "127.0.0.1";

    [Required]
    public string BaseUrl { get; set; } = "http://www.pekora.zip";

    public string ServiceUrl { get; set; } = string.Empty;

    [Required]
    public string RccServiceRoot { get; set; } = "RCCService";

    [Required]
    public string QuilkinPath { get; set; } = "quilkin.exe";

    public bool ForcedFilteringEnabled { get; set; }

    public string GlobalMessageTopic { get; set; } = "GlobalMessage_KRNE";

    [Range(0, 300)]
    public int PostStartDelaySeconds { get; set; } = 15;

    public string GameServerApiKey { get; set; } = string.Empty;

    public string PlaceVisitAccessKey { get; set; } = string.Empty;

    [Required]
    public ArbiterPortOptions Ports { get; set; } = new();

    [Required]
    public ArbiterProcessOptions Processes { get; set; } = new();
}

public sealed class ArbiterPortOptions
{
    [Required]
    public PortRange Rcc { get; set; } = new() { Start = 45000, End = 47000 };

    [Required]
    public PortRange GameServer { get; set; } = new() { Start = 50000, End = 60000 };

    [Required]
    public PortRange Proxy { get; set; } = new() { Start = 30000, End = 40000 };

    [Range(0, 3600)]
    public int RecentlyUsedHoldSeconds { get; set; } = 30;
}

public sealed class PortRange
{
    [Range(1, 65535)]
    public int Start { get; set; }

    [Range(1, 65535)]
    public int End { get; set; }
}

public sealed class ArbiterProcessOptions
{
    [Range(1, 10000)]
    public int MaxActiveProcesses { get; set; } = 256;

    [Range(1, 10000)]
    public int MaxActivePerYear { get; set; } = 128;

    [Range(0, 100)]
    public int ReservePerYear { get; set; } = 2;

    [Range(1, 1000)]
    public int MaxReuseCount { get; set; } = 5;

    [Range(1, 86400)]
    public int IdleTtlSeconds { get; set; } = 300;

    [Range(1, 300)]
    public int StartupTimeoutSeconds { get; set; } = 15;

    [Range(1, 300)]
    public int ShutdownTimeoutSeconds { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int JobExpirationSeconds { get; set; } = 6000000;

    [Range(1, 300)]
    public int CleanupIntervalSeconds { get; set; } = 5;
}
