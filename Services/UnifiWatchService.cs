using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UnifiWatch.Configuration;
using UnifiWatch.Models;
using UnifiWatch.Services.Notifications;
using UnifiWatch.Utilities;
using ConfigServiceConfiguration = UnifiWatch.Configuration.ServiceConfiguration;

namespace UnifiWatch.Services;

/// <summary>
/// Background service for continuous UniFi stock monitoring
/// Runs as Windows Service, systemd daemon, or launchd service
/// Monitors stock changes and sends multi-channel notifications
/// </summary>
public class UnifiWatchService : BackgroundService
{
    private readonly IConfigurationProvider _configProvider;
    private readonly IUnifiStockService _stockService;
    private readonly NotificationOrchestrator _notificationOrchestrator;
    private readonly ILogger<UnifiWatchService> _logger;
    private readonly string _stateFilePath;
    private ConfigServiceConfiguration? _config;
    private ServiceState _state;
    private FileSystemWatcher? _configWatcher;

    public UnifiWatchService(
        IConfigurationProvider configProvider,
        IUnifiStockService stockService,
        NotificationOrchestrator notificationOrchestrator,
        ILogger<UnifiWatchService> logger)
    {
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
        _stockService = stockService ?? throw new ArgumentNullException(nameof(stockService));
        _notificationOrchestrator = notificationOrchestrator ?? throw new ArgumentNullException(nameof(notificationOrchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Determine state file path (same directory as config.json)
        var configDirectory = PlatformUtilities.GetConfigurationDirectory();
        _stateFilePath = Path.Combine(configDirectory, "state.json");

        _state = new ServiceState
        {
            ServiceStartTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Main service execution loop
    /// Loads configuration, monitors stock, sends notifications, and handles graceful shutdown
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("UnifiWatch service starting at {Time}", DateTime.UtcNow);

        try
        {
            // Load initial configuration
            await LoadConfigurationAsync(stoppingToken);

            // Perform pre-flight validation (informational, not blocking)
            ValidatePreFlightConditions();

            // Load persisted state
            await LoadStateAsync();

            // Start configuration file watcher
            StartConfigurationWatcher();

            // Main monitoring loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check if service is paused
                    if (_config?.Service.Paused == true)
                    {
                        _logger.LogDebug("Service is paused, skipping stock check");
                        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                        continue;
                    }

                    // Perform stock check
                    await PerformStockCheckAsync(stoppingToken);

                    // Wait for next check interval
                    var checkInterval = TimeSpan.FromSeconds(_config?.Service.CheckIntervalSeconds ?? 300);
                    _logger.LogDebug("Next stock check in {Interval} seconds", checkInterval.TotalSeconds);
                    await Task.Delay(checkInterval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Graceful shutdown in progress
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in monitoring loop");
                    // Wait a bit before retrying to avoid tight error loops
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("UnifiWatch service shutdown requested");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Fatal error in UnifiWatch service");
            throw;
        }
        finally
        {
            // Graceful shutdown
            await ShutdownAsync();
        }
    }

    /// <summary>
    /// Loads service configuration from disk
    /// </summary>
    private async Task LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            _config = await _configProvider.LoadAsync(cancellationToken);
            _logger.LogInformation("Configuration loaded: Store={Store}, CheckInterval={Interval}s",
                _config.Monitoring.Store,
                _config.Service.CheckIntervalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration");
            throw;
        }
    }

    /// <summary>
    /// Loads persisted service state from state.json
    /// </summary>
    private async Task LoadStateAsync()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                var json = await File.ReadAllTextAsync(_stateFilePath);
                var loadedState = JsonSerializer.Deserialize<ServiceState>(json);
                if (loadedState != null)
                {
                    _state = loadedState;
                    _state.ServiceStartTime = DateTime.UtcNow; // Reset start time
                    _logger.LogInformation("Loaded state from {Path}: {ProductCount} products tracked, {TotalChecks} total checks",
                        _stateFilePath,
                        _state.ProductStates.Count,
                        _state.TotalChecks);
                }
            }
            else
            {
                _logger.LogInformation("No previous state file found, starting fresh");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load state file, starting fresh");
        }
    }

    /// <summary>
    /// Saves current service state to state.json
    /// </summary>
    private async Task SaveStateAsync()
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_stateFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_stateFilePath, json);
            _logger.LogDebug("State saved to {Path}", _stateFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save state file");
        }
    }

    /// <summary>
    /// Starts FileSystemWatcher to monitor config.json for changes
    /// </summary>
    private void StartConfigurationWatcher()
    {
        try
        {
            var configDirectory = PlatformUtilities.GetConfigurationDirectory();
            
            // Ensure directory exists before watching it
            if (!Directory.Exists(configDirectory))
            {
                _logger.LogWarning("Configuration directory does not exist: {Directory}, skipping file watcher",
                    configDirectory);
                return;
            }

            var configFileName = "config.json";

            _configWatcher = new FileSystemWatcher(configDirectory, configFileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };

            _configWatcher.Changed += async (sender, e) =>
            {
                _logger.LogInformation("Configuration file changed, reloading...");
                try
                {
                    // Wait a bit to ensure file write is complete
                    await Task.Delay(500);
                    await LoadConfigurationAsync(CancellationToken.None);
                    _logger.LogInformation("Configuration reloaded successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reload configuration");
                }
            };

            _configWatcher.EnableRaisingEvents = true;
            _logger.LogInformation("Started configuration file watcher for {Directory}/{File}",
                configDirectory, configFileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start configuration file watcher");
        }
    }

    /// <summary>
    /// Performs a single stock check cycle
    /// Fetches products, detects changes, sends notifications
    /// </summary>
    private async Task PerformStockCheckAsync(CancellationToken cancellationToken)
    {
        if (_config == null)
        {
            _logger.LogWarning("Configuration not loaded, skipping stock check");
            return;
        }

        _state.TotalChecks++;
        _state.LastCheckTime = DateTime.UtcNow;

        _logger.LogInformation("Starting stock check #{CheckCount} for store: {Store}",
            _state.TotalChecks,
            _config.Monitoring.Store);

        try
        {
            // Fetch current stock
            var products = await _stockService.GetStockAsync(_config.Monitoring.Store, null, cancellationToken);

            _logger.LogInformation("Fetched {ProductCount} products", products.Count);

            // Filter products based on configuration
            var filteredProducts = FilterProducts(products);

            _logger.LogInformation("Filtered to {FilteredCount} products matching criteria", filteredProducts.Count);

            // Detect changes and products newly in stock
            var newlyAvailable = DetectChanges(filteredProducts);

            if (newlyAvailable.Any())
            {
                _logger.LogInformation("Found {Count} newly available products", newlyAvailable.Count);

                // Send notifications
                await SendNotificationsAsync(newlyAvailable, cancellationToken);
            }
            else
            {
                _logger.LogDebug("No new products available");
            }

            // Update state with current products
            UpdateProductStates(filteredProducts);

            // Save state
            await SaveStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stock check failed");
        }
    }

    /// <summary>
    /// Filters products based on configuration criteria
    /// </summary>
    private List<UnifiProduct> FilterProducts(List<UnifiProduct> products)
    {
        var filtered = products.AsEnumerable();

        // Filter by collection if specified
        if (_config?.Monitoring.Collections?.Any() == true)
        {
            filtered = filtered.Where(p =>
                p.Collection != null &&
                _config.Monitoring.Collections.Contains(p.Collection, StringComparer.OrdinalIgnoreCase));
        }

        // Filter by SKU if specified
        if (_config?.Monitoring.ProductSkus?.Any() == true)
        {
            filtered = filtered.Where(p =>
                _config.Monitoring.ProductSkus.Contains(p.SKU, StringComparer.OrdinalIgnoreCase));
        }

        // Filter by name (partial match) if specified
        if (_config?.Monitoring.ProductNames?.Any() == true)
        {
            filtered = filtered.Where(p =>
                _config.Monitoring.ProductNames.Any(name =>
                    p.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                    (p.ShortName != null && p.ShortName.Contains(name, StringComparison.OrdinalIgnoreCase))));
        }

        return filtered.ToList();
    }

    /// <summary>
    /// Detects products that are newly available (changed from unavailable to available)
    /// </summary>
    private List<UnifiProduct> DetectChanges(List<UnifiProduct> currentProducts)
    {
        var newlyAvailable = new List<UnifiProduct>();

        foreach (var product in currentProducts)
        {
            if (!product.Available)
                continue;

            // Check if we've seen this product before
            if (_state.ProductStates.TryGetValue(product.SKU, out var previousState))
            {
                // Product was previously unavailable, now available
                if (!previousState.Available && product.Available)
                {
                    _logger.LogInformation("Product {SKU} ({Name}) is now AVAILABLE (was unavailable)",
                        product.SKU, product.Name);
                    newlyAvailable.Add(product);
                }
            }
            else
            {
                // First time seeing this product and it's available
                _logger.LogInformation("New product {SKU} ({Name}) detected and AVAILABLE",
                    product.SKU, product.Name);
                newlyAvailable.Add(product);
            }
        }

        return newlyAvailable;
    }

    /// <summary>
    /// Updates internal state with current product availability
    /// </summary>
    private void UpdateProductStates(List<UnifiProduct> products)
    {
        foreach (var product in products)
        {
            if (_state.ProductStates.TryGetValue(product.SKU, out var existingState))
            {
                // Update existing state
                var availabilityChanged = existingState.Available != product.Available;
                existingState.Available = product.Available;
                existingState.Price = product.Price;
                existingState.Name = product.Name;
                existingState.LastUpdated = DateTime.UtcNow;

                if (availabilityChanged)
                {
                    existingState.AvailabilityChanges++;
                }
            }
            else
            {
                // Add new product state
                _state.ProductStates[product.SKU] = new ProductState
                {
                    SKU = product.SKU,
                    Name = product.Name,
                    Available = product.Available,
                    Price = product.Price,
                    LastUpdated = DateTime.UtcNow,
                    AvailabilityChanges = 0
                };
            }
        }
    }

    /// <summary>
    /// Sends notifications for newly available products
    /// </summary>
    private async Task SendNotificationsAsync(List<UnifiProduct> products, CancellationToken cancellationToken)
    {
        try
        {
            var message = new NotificationMessage
            {
                Subject = $"UniFi Stock Alert: {products.Count} Product(s) Available",
                TextBody = $"{products.Count} product(s) are now in stock at {_config?.Monitoring.Store}",
                Products = products,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _notificationOrchestrator.SendAsync(message, cancellationToken);

            if (result.Success)
            {
                _state.TotalNotificationsSent++;
                _state.LastNotificationTime = DateTime.UtcNow;

                // Update last notification time for each product
                foreach (var product in products)
                {
                    if (_state.ProductStates.TryGetValue(product.SKU, out var state))
                    {
                        state.LastNotificationTime = DateTime.UtcNow;
                    }
                }

                _logger.LogInformation("Notification sent successfully: Desktop={Desktop}, Email={Email}, SMS={SMS}",
                    result.DesktopSuccess, result.EmailSuccess, result.SmsSuccess);
            }
            else
            {
                _logger.LogWarning("Notification failed to send");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notifications");
        }
    }

    /// <summary>
    /// Graceful shutdown with state persistence
    /// </summary>
    private async Task ShutdownAsync()
    {
        _logger.LogInformation("UnifiWatch service shutting down...");

        try
        {
            // Dispose configuration watcher
            _configWatcher?.Dispose();

            // Save final state
            await SaveStateAsync();

            _logger.LogInformation("Service shutdown complete. Total checks: {Checks}, Total notifications: {Notifications}",
                _state.TotalChecks, _state.TotalNotificationsSent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during shutdown");
        }
    }

    /// <summary>
    /// Validates pre-flight conditions before service monitoring begins
    /// Logs information about configuration, dependencies, and system resources
    /// Does not block service startup, allows graceful degradation
    /// </summary>
    private void ValidatePreFlightConditions()
    {
        try
        {
            _logger.LogInformation("Performing pre-flight validation...");

            // 1. Verify configuration is loaded
            if (_config == null)
            {
                _logger.LogWarning("Configuration not loaded");
                return;
            }

            // 2. Verify monitoring configuration
            if (string.IsNullOrWhiteSpace(_config.Monitoring.Store))
            {
                _logger.LogWarning("No store configured. Service will not monitor stock.");
            }

            // 3. Check service is enabled
            if (!_config.Service.Enabled)
            {
                _logger.LogWarning("Service is disabled in configuration.");
            }

            // 4. Verify check interval is reasonable
            if (_config.Service.CheckIntervalSeconds < 10)
            {
                _logger.LogWarning("Check interval is very short: {Interval}s (minimum recommended: 10s)",
                    _config.Service.CheckIntervalSeconds);
            }

            // 5. Verify stock service is available
            if (_stockService == null)
            {
                _logger.LogWarning("Stock service is not available.");
            }

            // 6. Verify notification orchestrator is available
            if (_notificationOrchestrator == null)
            {
                _logger.LogWarning("Notification orchestrator is not available.");
            }

            // 7. Log notification channel configuration
            var enabledChannels = new List<string>();
            if (_config.Notifications.Desktop.Enabled)
                enabledChannels.Add("Desktop");
            if (_config.Notifications.Email.Enabled)
                enabledChannels.Add("Email");
            if (_config.Notifications.Sms.Enabled)
                enabledChannels.Add("SMS");

            if (enabledChannels.Count > 0)
            {
                _logger.LogInformation("Notification channels enabled: {Channels}",
                    string.Join(", ", enabledChannels));
            }
            else
            {
                _logger.LogWarning("No notification channels enabled. Stock updates will not be sent.");
            }

            _logger.LogInformation("Pre-flight validation complete.");
            _logger.LogInformation("Monitoring store: {Store}, Check interval: {Interval}s, Language: {Language}",
                _config.Monitoring.Store ?? "Not configured",
                _config.Service.CheckIntervalSeconds,
                _config.Service.Language ?? "default");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pre-flight validation");
        }
    }}