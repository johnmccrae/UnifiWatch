using Microsoft.Extensions.Logging;
using UnifiWatch.Configuration;
using UnifiWatch.Models;
using UnifiWatch.Services;
using UnifiWatch.Services.Notifications;
using Xunit;

namespace UnifiWatch.Tests;

public class UnifiWatchServiceTests
{
    private class FakeStockService : IunifiwatchService
    {
        public Task<List<UnifiProduct>> GetStockAsync(string store, string[]? collections = null, CancellationToken cancellationToken = default)
        {
            var products = new List<UnifiProduct>
            {
                new UnifiProduct { Name = "Test Product", SKU = "TEST-001", Available = true, Price = 9999 },
                new UnifiProduct { Name = "Another Product", SKU = "TEST-002", Available = false, Price = 4999 }
            };
            return Task.FromResult(products);
        }
    }

    private class NoopOrchestrator : NotificationOrchestrator
    {
        public NoopOrchestrator() : base(
            emailService: new EmailNotificationService(
                emailProvider: new TestEmailProvider(),
                resourceLocalizer: Services.Localization.ResourceLocalizer.Load(System.Globalization.CultureInfo.GetCultureInfo("en-CA")),
                logger: new LoggerFactory().CreateLogger<EmailNotificationService>()),
            smsService: new SmsNotificationService(
                smsProvider: new TestSmsProvider(),
                resourceLocalizer: Services.Localization.ResourceLocalizer.Load(System.Globalization.CultureInfo.GetCultureInfo("en-CA")),
                logger: new LoggerFactory().CreateLogger<SmsNotificationService>(),
                settings: new SmsNotificationSettings { Enabled = false }),
            emailOptions: Microsoft.Extensions.Options.Options.Create(new EmailNotificationSettings { Enabled = false, Recipients = new List<string>() }),
            smsOptions: Microsoft.Extensions.Options.Options.Create(new SmsNotificationSettings { Enabled = false, Recipients = new List<string>() }),
            logger: new LoggerFactory().CreateLogger<NotificationOrchestrator>(),
            dedupeWindow: TimeSpan.FromMinutes(5))
        { }

        private class TestEmailProvider : IEmailProvider
        {
            public Task<bool> SendAsync(string recipient, string subject, string plainBody, string? htmlBody = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
            public Task<Dictionary<string, bool>> SendBatchAsync(List<string> recipients, string subject, string plainBody, string? htmlBody = null, CancellationToken cancellationToken = default)
                => Task.FromResult(recipients.ToDictionary(r => r, r => true));
        }
        private class TestSmsProvider : ISmsProvider
        {
            public Task<bool> SendAsync(string recipient, string message, CancellationToken cancellationToken = default) => Task.FromResult(true);
            public Task<Dictionary<string, bool>> SendBatchAsync(IList<string> recipients, string message, CancellationToken cancellationToken = default)
                => Task.FromResult(recipients.ToDictionary(r => r, r => true));
        }
    }

    [Fact]
    public async Task RunOnce_WritesStateFile()
    {
        // Arrange: mock configuration provider to use defaults
        var loggerFactory = new LoggerFactory();
        var configProvider = new ConfigurationProvider(loggerFactory.CreateLogger<ConfigurationProvider>());
        var stockService = new FakeStockService();
        var orchestrator = new NoopOrchestrator();
        var service = new UnifiWatchService(configProvider, stockService, orchestrator, loggerFactory.CreateLogger<UnifiWatchService>(), enableFileWatcher: false);

        var statePath = Path.Combine(configProvider.ConfigurationDirectory, "state.json");
        if (File.Exists(statePath)) File.Delete(statePath);

        // Act
        await service.RunOnceAsync();

        // Assert
        Assert.True(File.Exists(statePath));
        var state = await ServiceState.LoadAsync(statePath);
        Assert.NotNull(state);
        Assert.True(state!.AvailabilityBySku.ContainsKey("TEST-001"));
    }

    [Fact]
    public async Task RunOnce_WhenPaused_DoesNotWriteState()
    {
        // Arrange: create a temporary configuration file with paused=true
        var loggerFactory = new LoggerFactory();
        var configProvider = new ConfigurationProvider(loggerFactory.CreateLogger<ConfigurationProvider>());
        var stockService = new FakeStockService();
        var orchestrator = new NoopOrchestrator();
        var service = new UnifiWatchService(configProvider, stockService, orchestrator, loggerFactory.CreateLogger<UnifiWatchService>(), enableFileWatcher: false);

        var pausedConfig = configProvider.GetDefaultConfiguration();
        pausedConfig.Service.Paused = true;
        await configProvider.SaveAsync(pausedConfig);

        var statePath = Path.Combine(configProvider.ConfigurationDirectory, "state.json");
        if (File.Exists(statePath)) File.Delete(statePath);

        // Act
        await service.RunOnceAsync();

        // Assert
        Assert.False(File.Exists(statePath));
    }

    [Fact]
    public async Task RunOnce_AppliesLatestConfigChanges()
    {
        var loggerFactory = new LoggerFactory();
        var configProvider = new ConfigurationProvider(loggerFactory.CreateLogger<ConfigurationProvider>());
        var stockService = new FakeStockService();
        var orchestrator = new NoopOrchestrator();
        var service = new UnifiWatchService(configProvider, stockService, orchestrator, loggerFactory.CreateLogger<UnifiWatchService>(), enableFileWatcher: false);

        var statePath = Path.Combine(configProvider.ConfigurationDirectory, "state.json");
        if (File.Exists(statePath)) File.Delete(statePath);

        // First run paused (no state write)
        var pausedConfig = configProvider.GetDefaultConfiguration();
        pausedConfig.Service.Paused = true;
        await configProvider.SaveAsync(pausedConfig);
        await service.RunOnceAsync();
        Assert.False(File.Exists(statePath));

        // Second run resumed (writes state)
        pausedConfig.Service.Paused = false;
        await configProvider.SaveAsync(pausedConfig);
        await service.RunOnceAsync();
        Assert.True(File.Exists(statePath));
    }
}
