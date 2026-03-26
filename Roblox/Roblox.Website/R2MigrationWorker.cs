using Microsoft.Extensions.Hosting;
using Roblox.Services;

namespace Roblox.Website;

// TODO: deprecate dis shit once its 5alas, we not deleting file so i make a no-content file with .migrated

public class R2MigrationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public R2MigrationWorker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var r2Service = new R2StorageService();

        var mappings = new Dictionary<string, string>
        {
            { Configuration.AssetDirectory, "assets/" },
            { Configuration.ThumbnailsDirectory, "images/thumbnails/" },
            { Configuration.GroupIconsDirectory, "images/groups/" },
        };

        foreach (var mapping in mappings)
        {
            if (!Directory.Exists(mapping.Key)) continue;

            var files = Directory.GetFiles(mapping.Key);
            foreach (var file in files)
            {
                if (stoppingToken.IsCancellationRequested) return;
                
                if (file.EndsWith(".migrated")) continue;

                var fileName = Path.GetFileName(file);
                var markerPath = file + ".migrated";
                var r2Key = mapping.Value + fileName;

                try
                {
                    if (File.Exists(markerPath)) continue;
                    
                    using var fs = File.OpenRead(file);
                    await r2Service.UploadFileAsync(r2Key, fs);
                    fs.Close();
                    
                    File.Create(markerPath).Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[R2-MIRGATION] Failed to migrate {file}: {ex.Message}");
                }
            }
        }
    }
}