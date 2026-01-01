using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnifiWatch.Configuration;
using UnifiWatch.Models;
using UnifiWatch.Services.Notifications;

namespace UnifiWatch.Services;

/// <summary>
/// Background service that periodically checks stock and sends notifications.
/// </summary>
public class UnifiWatchService : BackgroundService
{
    private readonly IConfigurationProvider _configProvider;
    private readonly IunifiwatchService _stockService;
    private readonly NotificationOrchestrator? _orchestrator;
    private readonly ILogger<UnifiWatchService> _logger;
    private readonly string _stateFilePath;
    private readonly bool _enableFileWatcher;
    private readonly TimeSpan _shutdownTimeout = TimeSpan.FromSeconds(30);
    private FileSystemWatcher? _watcher;
    private volatile bool _configReloadRequested;
    private ServiceConfiguration? _cachedConfig;
    private readonly object _configLock = new();
    private Task? _activeIteration;

    public UnifiWatchService(
        IConfigurationProvider configProvider,
        IunifiwatchService stockService,
        NotificationOrchestrator? orchestrator,
        ILogger<UnifiWatchService> logger,
        bool enableFileWatcher = true)
    {
        _configProvider = configProvider;
        _stockService = stockService;
        _orchestrator = orchestrator;
        _logger = logger;
        _stateFilePath = Path.Combine(configProvider.ConfigurationDirectory, "state.json");
        _enableFileWatcher = enableFileWatcher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UnifiWatchService starting");
        await InitializeWatcherAsync();

        // Load configuration (fallback to defaults if missing)
        _cachedConfig = await LoadConfigAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _activeIteration = RunIterationAsync(stoppingToken);
                await _activeIteration;
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UnifiWatchService loop: {Message}", ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        _logger.LogInformation("UnifiWatchService stopping");
        await WaitForActiveIterationAsync();
        await PersistStateOnShutdownAsync(stoppingToken);
    }

    /// <summary>
    /// Runs a single monitoring cycle (used for unit testing).
    /// </summary>
    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var config = await _configProvider.LoadAsync(cancellationToken)
                     ?? _configProvider.GetDefaultConfiguration();

        await ProcessIterationAsync(config, cancellationToken);
    }

    private async Task RunIterationAsync(CancellationToken stoppingToken)
    {
        // Apply pending config reloads triggered by FileSystemWatcher
        if (_configReloadRequested || _cachedConfig == null)
        {
            _cachedConfig = await LoadConfigAsync(stoppingToken);
            _configReloadRequested = false;
        }

        var config = _cachedConfig ?? _configProvider.GetDefaultConfiguration();

        await ProcessIterationAsync(config, stoppingToken);

        await Task.Delay(TimeSpan.FromSeconds(Math.Max(10, config.Service.CheckIntervalSeconds)), stoppingToken);
    }

    private async Task<ServiceConfiguration> LoadConfigAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cfg = await _configProvider.LoadAsync(cancellationToken);
            if (cfg != null)
            {
                _logger.LogInformation("Configuration loaded (enabled: {Enabled}, paused: {Paused})", cfg.Service.Enabled, cfg.Service.Paused);
                return cfg;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load configuration; falling back to defaults");
        }

        return _configProvider.GetDefaultConfiguration();
    }

    private async Task ProcessIterationAsync(ServiceConfiguration config, CancellationToken token)
    {
        if (!config.Service.Enabled || config.Service.Paused)
        {
            _logger.LogInformation("Service paused or disabled. Sleeping...");
            return;
        }

        var store = config.Monitoring.Store;
        var collections = config.Monitoring.Collections?.Count > 0 ? config.Monitoring.Collections.ToArray() : null;

        var products = await _stockService.GetStockAsync(store, collections, token);
        var filtered = FilterProducts(products, config);
        var inStock = filtered.Where(p => p.Available).ToList();

        _logger.LogInformation("Checked {Total} products, {InStock} in stock", filtered.Count, inStock.Count);

        if (inStock.Count > 0 && _orchestrator != null)
        {
            await _orchestrator.NotifyInStockAsync(inStock, store, token);
        }

        // Persist minimal state
        var state = ServiceState.FromProducts(filtered);
        await ServiceState.SaveAsync(_stateFilePath, state, token);
    }

    private Task InitializeWatcherAsync()
    {
        if (!_enableFileWatcher)
        {
            return Task.CompletedTask;
        }

        try
        {
            var directory = _configProvider.ConfigurationDirectory;
            var fileName = Path.GetFileName(_configProvider.ConfigurationFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnConfigChanged;
            _watcher.Created += OnConfigChanged;
            _watcher.Renamed += OnConfigChanged;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize configuration watcher; continuing without live reload");
        }

        return Task.CompletedTask;
    }

    private void OnConfigChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce via flag; loop will reload on next iteration
        _configReloadRequested = true;
        _logger.LogInformation("Configuration change detected at {Path}; reload scheduled", e.FullPath);
    }

    private async Task PersistStateOnShutdownAsync(CancellationToken stoppingToken)
    {
        try
        {
            // If we already have a cached config, reuse the last state file path.
            // No-op if no data was processed.
            var lastState = await ServiceState.LoadAsync(_stateFilePath, stoppingToken);
            if (lastState != null)
            {
                await ServiceState.SaveAsync(_stateFilePath, lastState, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist state during shutdown");
        }
    }

    private async Task WaitForActiveIterationAsync()
    {
        var iteration = _activeIteration;
        if (iteration == null)
        {
            return;
        }

        var completed = await Task.WhenAny(iteration, Task.Delay(_shutdownTimeout));
        if (completed != iteration)
        {
            _logger.LogWarning("Shutdown timed out after {Timeout}; iteration still running", _shutdownTimeout);
        }
        else if (iteration.IsFaulted)
        {
            _logger.LogError(iteration.Exception, "Iteration faulted during shutdown");
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnConfigChanged;
            _watcher.Created -= OnConfigChanged;
            _watcher.Renamed -= OnConfigChanged;
            _watcher.Dispose();
        }
    }

    private static List<UnifiProduct> FilterProducts(List<UnifiProduct> products, ServiceConfiguration config)
    {
        IEnumerable<UnifiProduct> result = products;

        if (config.Monitoring.ProductNames?.Count > 0)
        {
            var names = new HashSet<string>(config.Monitoring.ProductNames, StringComparer.OrdinalIgnoreCase);
            result = result.Where(p => p.Name != null && names.Contains(p.Name));
        }

        if (config.Monitoring.ProductSkus?.Count > 0)
        {
            var skus = new HashSet<string>(config.Monitoring.ProductSkus, StringComparer.OrdinalIgnoreCase);
            result = result.Where(p => p.SKU != null && skus.Contains(p.SKU));
        }

        return result.ToList();
    }
}
