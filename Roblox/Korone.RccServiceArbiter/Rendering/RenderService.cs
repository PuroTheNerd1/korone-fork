using System.Collections.Concurrent;
using System.Diagnostics;
using Korone.RccServiceArbiter.Configuration;
using Korone.RccServiceArbiter.Processes;
using Korone.RccServiceArbiter.Rcc;
using Microsoft.Extensions.Options;
using Roblox.Rendering;

namespace Korone.RccServiceArbiter.Rendering;

public sealed class RenderService : IRenderService, IDisposable
{
    private readonly object _workersGate = new();
    private readonly List<RenderWorker> _idle = [];
    private readonly SemaphoreSlim _slots;
    private readonly ArbiterOptions _options;
    private readonly IPortAllocator _ports;
    private readonly IRccProcessLauncher _launcher;
    private readonly IRccReadinessProbe _readiness;
    private readonly IRccSoapClientFactory _soapFactory;
    private readonly IRenderScriptCatalog _scripts;
    private readonly ILogger<RenderService> _logger;
    private int _workerCount;
    private int _queued;
    private int _running;
    private long _completed;
    private long _failed;

    public RenderService(IOptions<ArbiterOptions> options, IPortAllocator ports, IRccProcessLauncher launcher,
        IRccReadinessProbe readiness, IRccSoapClientFactory soapFactory, IRenderScriptCatalog scripts,
        ILogger<RenderService> logger)
    {
        _options = options.Value;
        _ports = ports;
        _launcher = launcher;
        _readiness = readiness;
        _soapFactory = soapFactory;
        _scripts = scripts;
        _logger = logger;
        _slots = new SemaphoreSlim(_options.Render.MaxWorkers, _options.Render.MaxWorkers);
    }

    public async Task<RenderResult> RenderAsync(RenderRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var queued = Interlocked.Increment(ref _queued);
        if (queued > _options.Render.QueueCapacity + _options.Render.MaxWorkers)
        {
            Interlocked.Decrement(ref _queued);
            throw new RenderCapacityException("Render queue is full");
        }

        try
        {
            await _slots.WaitAsync(cancellationToken);
        }
        finally
        {
            Interlocked.Decrement(ref _queued);
        }

        Interlocked.Increment(ref _running);
        RenderWorker? worker = null;
        var healthy = false;
        try
        {
            if (request.Kind is RenderKind.PlaceConversion or RenderKind.HatConversion)
            {
                var converted = await ConvertAsync(request, cancellationToken);
                Interlocked.Increment(ref _completed);
                return converted;
            }

            worker = await AcquireWorkerAsync(cancellationToken);
            var script = _scripts.Create(request);
            var jobId = Guid.NewGuid();
            var job = new Job
            {
                Id = jobId.ToString(),
                Category = 2,
                Cores = 1,
                ExpirationInSeconds = _options.Render.JobTimeoutSeconds,
            };
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.Render.JobTimeoutSeconds));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            IReadOnlyList<LuaValue> values;
            try
            {
                values = _options.Render.DefaultYear >= 2018
                    ? await worker.Soap.BatchJobAsync(job, script, linked.Token)
                    : await worker.Soap.BatchJobExAsync(job, script, linked.Token);
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("RCC render job timed out");
            }

            var data = values.FirstOrDefault()?.Value;
            if (string.IsNullOrWhiteSpace(data))
            {
                throw new RenderExecutionException("RCC returned no render data");
            }
            if (request.Kind == RenderKind.Avatar3D)
            {
                data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(data));
            }
            if (Base64DecodedLength(data) > _options.Render.MaxOutputMegabytes * 1024L * 1024L)
            {
                throw new RenderExecutionException("RCC render output exceeded the configured limit");
            }

            healthy = true;
            worker.UseCount++;
            worker.LastUsedUtc = DateTime.UtcNow;
            Interlocked.Increment(ref _completed);
            return new RenderResult
            {
                JobId = jobId,
                ContentType = request.Kind is RenderKind.Avatar3D ? "application/json" : "image/png",
                Data = data,
                DependencyUrls = values.Skip(1).SelectMany(value => value.Table).Select(value => value.Value)
                    .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToArray(),
            };
        }
        catch
        {
            Interlocked.Increment(ref _failed);
            throw;
        }
        finally
        {
            if (worker != null) ReleaseWorker(worker, healthy);
            Interlocked.Decrement(ref _running);
            _slots.Release();
        }
    }

    public RenderStatistics GetStatistics()
    {
        lock (_workersGate)
        {
            return new RenderStatistics
            {
                WorkerCount = _workerCount, IdleWorkerCount = _idle.Count, RunningJobs = Volatile.Read(ref _running),
                QueuedJobs = Math.Max(0, Volatile.Read(ref _queued)), CompletedJobs = Interlocked.Read(ref _completed),
                FailedJobs = Interlocked.Read(ref _failed), Capacity = _options.Render.MaxWorkers,
                QueueCapacity = _options.Render.QueueCapacity,
            };
        }
    }

    public int CleanUpIdleWorkers()
    {
        List<RenderWorker> expired;
        lock (_workersGate)
        {
            expired = _idle.Where(worker => worker.Process.HasExited ||
                worker.LastUsedUtc.AddSeconds(_options.Render.IdleTtlSeconds) <= DateTime.UtcNow).ToList();
            _idle.RemoveAll(expired.Contains);
            _workerCount -= expired.Count;
        }
        foreach (var worker in expired) DisposeWorker(worker);
        return expired.Count;
    }

    private async Task<RenderWorker> AcquireWorkerAsync(CancellationToken cancellationToken)
    {
        lock (_workersGate)
        {
            while (_idle.Count > 0)
            {
                var index = _idle.Count - 1;
                var worker = _idle[index];
                _idle.RemoveAt(index);
                if (!worker.Process.HasExited) return worker;
                _workerCount--;
                DisposeWorker(worker);
            }
            _workerCount++;
        }

        var port = _ports.Allocate(_options.Ports.Rcc);
        IManagedProcess? process = null;
        try
        {
            var executable = Path.Combine(_options.RccServiceRoot, $"RCCService{_options.Render.DefaultYear}", "RCCService.exe");
            process = _launcher.Start(executable, $"-console {port}", Path.GetDirectoryName(executable));
            await _readiness.WaitUntilAvailableAsync(port, TimeSpan.FromSeconds(_options.Processes.StartupTimeoutSeconds), cancellationToken);
            return new RenderWorker(port, process, _soapFactory.Create(port));
        }
        catch
        {
            process?.KillTree(); process?.Dispose(); _ports.Release(port);
            lock (_workersGate) _workerCount--;
            throw;
        }
    }

    private void ReleaseWorker(RenderWorker worker, bool healthy)
    {
        var retain = healthy && !worker.Process.HasExited && worker.UseCount < _options.Render.MaxReuseCount;
        var pooled = false;
        lock (_workersGate)
        {
            if (retain && _idle.Count < _options.Render.IdleReserve) { _idle.Add(worker); pooled = true; }
            else _workerCount--;
        }
        if (!pooled) DisposeWorker(worker);
    }

    private async Task<RenderResult> ConvertAsync(RenderRequest request, CancellationToken cancellationToken)
    {
        var bytes = Convert.FromBase64String(request.InputData!);
        var directory = Path.Combine(Path.GetTempPath(), "korone-render", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var input = Path.Combine(directory, "input.rbxl");
        var output = Path.Combine(directory, "output.rbxl");
        try
        {
            await File.WriteAllBytesAsync(input, bytes, cancellationToken);
            using var process = Process.Start(new ProcessStartInfo(_options.Render.PlaceConverterPath,
                $"{(request.Kind == RenderKind.PlaceConversion ? "game" : "hat")} \"{output}\" \"{input}\"")
            { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = directory })
                ?? throw new RenderExecutionException("Could not start the place converter");
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0 || !File.Exists(output)) throw new RenderExecutionException($"Place converter exited with code {process.ExitCode}");
            var result = await File.ReadAllBytesAsync(output, cancellationToken);
            return new RenderResult { JobId = Guid.NewGuid(), ContentType = "application/octet-stream", Data = Convert.ToBase64String(result) };
        }
        finally { try { Directory.Delete(directory, true); } catch { } }
    }

    private void Validate(RenderRequest request)
    {
        if (request.Width <= 0 || request.Height <= 0 || request.Width > _options.Render.MaxDimension || request.Height > _options.Render.MaxDimension)
            throw new RenderValidationException($"Dimensions must be between 1 and {_options.Render.MaxDimension}");
        if (request.Kind is RenderKind.PlaceConversion or RenderKind.HatConversion)
        {
            if (string.IsNullOrWhiteSpace(request.InputData)) throw new RenderValidationException("inputData is required");
            try { if (Base64DecodedLength(request.InputData) > _options.Render.MaxInputMegabytes * 1024L * 1024L) throw new RenderValidationException("Input exceeds configured limit"); }
            catch (FormatException) { throw new RenderValidationException("inputData must be valid base64"); }
        }
        else if (request.Kind is RenderKind.Avatar or RenderKind.AvatarHeadshot or RenderKind.Avatar3D)
        {
            if (request.UserId is null && request.Avatar is null && string.IsNullOrWhiteSpace(request.CharacterAppearanceUrl)) throw new RenderValidationException("userId, avatar, or characterAppearanceUrl is required");
        }
        else if (request.Kind == RenderKind.Animation && (string.IsNullOrWhiteSpace(request.CharacterAppearanceUrl) || string.IsNullOrWhiteSpace(request.AnimationUrl)))
            throw new RenderValidationException("characterAppearanceUrl and animationUrl are required");
        else if (request.AssetId is null && string.IsNullOrWhiteSpace(request.AssetUrl) && string.IsNullOrWhiteSpace(request.AssetUrls))
            throw new RenderValidationException("assetId, assetUrl, or assetUrls is required");
    }

    private static long Base64DecodedLength(string value) => Convert.FromBase64String(value).LongLength;
    private void DisposeWorker(RenderWorker worker) { _ports.Release(worker.Port); try { worker.Process.KillTree(); } finally { worker.Process.Dispose(); } }
    public void Dispose() { lock (_workersGate) { foreach (var worker in _idle) DisposeWorker(worker); _idle.Clear(); _workerCount = 0; } _slots.Dispose(); }

    private sealed class RenderWorker(int port, IManagedProcess process, IRccSoapClient soap)
    { public int Port { get; } = port; public IManagedProcess Process { get; } = process; public IRccSoapClient Soap { get; } = soap; public int UseCount { get; set; } public DateTime LastUsedUtc { get; set; } = DateTime.UtcNow; }
}
