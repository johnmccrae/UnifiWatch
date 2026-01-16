using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using UnifiWatch.Configuration;
using UnifiWatch.Models;
using UnifiWatch.Services;
using UnifiWatch.Services.Notifications;
using UnifiWatch.Services.Notifications.Sms;
using ConfigServiceConfiguration = UnifiWatch.Configuration.ServiceConfiguration;
using ConfigServiceSettings = UnifiWatch.Configuration.ServiceSettings;
using ConfigMonitoringSettings = UnifiWatch.Configuration.MonitoringSettings;
using ConfigNotificationSettings = UnifiWatch.Configuration.NotificationSettings;
using ConfigCredentialSettings = UnifiWatch.Configuration.CredentialSettings;

namespace UnifiWatch.Tests;

/// <summary>
/// Tests for UnifiWatchService background service functionality
/// </summary>
public class UnifiWatchServiceTests : IDisposable
{
    private readonly Mock<IConfigurationProvider> _mockConfigProvider;
    private readonly Mock<IUnifiStockService> _mockStockService;
    private readonly Mock<NotificationOrchestrator> _mockOrchestrator;
    private readonly Mock<ILogger<UnifiWatchService>> _mockLogger;
    private readonly string _testStateFile;

    public UnifiWatchServiceTests()
    {
        _mockConfigProvider = new Mock<IConfigurationProvider>();
        _mockStockService = new Mock<IUnifiStockService>();
        
        // Mock NotificationOrchestrator (requires concrete mocks for email/sms providers)
        var mockConfig = CreateTestConfiguration();
        var mockSmtpProvider = new Mock<INotificationProvider>();
        var mockGraphProvider = new Mock<INotificationProvider>();
        var mockSmsProvider = new Mock<ISmsProvider>();
        var mockOrchestratorLogger = new Mock<ILogger<NotificationOrchestrator>>();

        _mockOrchestrator = new Mock<NotificationOrchestrator>(
            mockConfig,
            mockSmtpProvider.Object,
            mockGraphProvider.Object,
            mockSmsProvider.Object,
            mockOrchestratorLogger.Object
        );

        _mockLogger = new Mock<ILogger<UnifiWatchService>>();

        // Use temp file for state
        _testStateFile = Path.Combine(Path.GetTempPath(), $"test_state_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        // Clean up test state file
        if (File.Exists(_testStateFile))
        {
            File.Delete(_testStateFile);
        }
    }

    [Fact]
    public async Task Service_Should_LoadConfiguration_OnStartup()
    {
        // Arrange
        var config = CreateTestConfiguration();
        _mockConfigProvider
            .Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mockStockService
            .Setup(x => x.GetStockAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UnifiProduct>());

        var service = new UnifiWatchService(
            _mockConfigProvider.Object,
            _mockStockService.Object,
            _mockOrchestrator.Object,
            _mockLogger.Object
        );

        var cts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(cts.Token);
        await Task.Delay(100); // Let service start
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert
        _mockConfigProvider.Verify(x => x.LoadAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Service_Should_PerformStockChecks_Periodically()
    {
        // Arrange
        var config = CreateTestConfiguration();
        config.Service.CheckIntervalSeconds = 1; // Short interval for testing

        _mockConfigProvider
            .Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mockStockService
            .Setup(x => x.GetStockAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UnifiProduct>());

        var service = new UnifiWatchService(
            _mockConfigProvider.Object,
            _mockStockService.Object,
            _mockOrchestrator.Object,
            _mockLogger.Object
        );

        var cts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(cts.Token);
        await Task.Delay(2500); // Wait for at least 2 checks
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert
        _mockStockService.Verify(
            x => x.GetStockAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(2)
        );
    }

    [Fact]
    public async Task Service_Should_DetectNewlyAvailableProducts()
    {
        // Arrange
        var config = CreateTestConfiguration();
        config.Service.CheckIntervalSeconds = 1;

        _mockConfigProvider
            .Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var callCount = 0;
        _mockStockService
            .Setup(x => x.GetStockAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // First call: product unavailable
                if (callCount == 1)
                {
                    return new List<UnifiProduct>
                    {
                        new() { SKU = "UDR-US", Name = "Dream Router", Available = false, Price = 199 }
                    };
                }
                // Second call: product now available
                else
                {
                    return new List<UnifiProduct>
                    {
                        new() { SKU = "UDR-US", Name = "Dream Router", Available = true, Price = 199 }
                    };
                }
            });

        _mockOrchestrator
            .Setup(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true });

        var service = new UnifiWatchService(
            _mockConfigProvider.Object,
            _mockStockService.Object,
            _mockOrchestrator.Object,
            _mockLogger.Object
        );

        var cts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(cts.Token);
        await Task.Delay(2500); // Wait for 2 checks
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert
        _mockOrchestrator.Verify(
            x => x.SendAsync(
                It.Is<NotificationMessage>(m => m.Products.Any(p => p.SKU == "UDR-US")),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Service_Should_NotNotify_IfProductStaysAvailable()
    {
        // Arrange
        var config = CreateTestConfiguration();
        config.Service.CheckIntervalSeconds = 1;

        _mockConfigProvider
            .Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var callCount = 0;
        // Product transitions: unavailable -> available -> available
        _mockStockService
            .Setup(x => x.GetStockAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // First check: product unavailable
                if (callCount == 1)
                {
                    return new List<UnifiProduct>
                    {
                        new() { SKU = "UDR-US", Name = "Dream Router", Available = false, Price = 199 }
                    };
                }
                // Subsequent checks: product available (notification sent on second check)
                // Third check: product still available (no new notification)
                else
                {
                    return new List<UnifiProduct>
                    {
                        new() { SKU = "UDR-US", Name = "Dream Router", Available = true, Price = 199 }
                    };
                }
            });

        _mockOrchestrator
            .Setup(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true });

        var service = new UnifiWatchService(
            _mockConfigProvider.Object,
            _mockStockService.Object,
            _mockOrchestrator.Object,
            _mockLogger.Object
        );

        var cts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(cts.Token);
        await Task.Delay(3000); // Wait for 3 checks: unavailable, becomes available (notify), stays available (no notify)
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - Product becomes available on second check (notification sent)
        // Product stays available on third check (no additional notification)
        _mockOrchestrator.Verify(
            x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Service_Should_RespectPausedFlag()
    {
        // Arrange
        var config = CreateTestConfiguration();
        config.Service.Paused = true; // Service is paused
        config.Service.CheckIntervalSeconds = 1;

        _mockConfigProvider
            .Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _mockStockService
            .Setup(x => x.GetStockAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UnifiProduct>());

        var service = new UnifiWatchService(
            _mockConfigProvider.Object,
            _mockStockService.Object,
            _mockOrchestrator.Object,
            _mockLogger.Object
        );

        var cts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(cts.Token);
        await Task.Delay(2000); // Wait
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - Should not perform stock checks when paused
        _mockStockService.Verify(
            x => x.GetStockAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Service_Should_FilterProducts_ByCollection()
    {
        // Arrange
        var config = CreateTestConfiguration();
        config.Monitoring.Collections = new List<string> { "switching" };
        config.Service.CheckIntervalSeconds = 1;

        _mockConfigProvider
            .Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var callCount = 0;
        _mockStockService
            .Setup(x => x.GetStockAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // First check: unavailable
                if (callCount == 1)
                {
                    return new List<UnifiProduct>
                    {
                        new() { SKU = "USW-24", Name = "Switch 24", Available = false, Collection = "switching" },
                        new() { SKU = "UDR-US", Name = "Dream Router", Available = false, Collection = "security" }
                    };
                }
                // Subsequent checks: available
                else
                {
                    return new List<UnifiProduct>
                    {
                        new() { SKU = "USW-24", Name = "Switch 24", Available = true, Collection = "switching" },
                        new() { SKU = "UDR-US", Name = "Dream Router", Available = true, Collection = "security" }
                    };
                }
            });

        _mockOrchestrator
            .Setup(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true });

        var service = new UnifiWatchService(
            _mockConfigProvider.Object,
            _mockStockService.Object,
            _mockOrchestrator.Object,
            _mockLogger.Object
        );

        var cts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(cts.Token);
        await Task.Delay(2000); // Wait for 2 checks
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - Should only notify for switching product
        _mockOrchestrator.Verify(
            x => x.SendAsync(
                It.Is<NotificationMessage>(m =>
                    m.Products.Count == 1 &&
                    m.Products[0].SKU == "USW-24"
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Service_Should_FilterProducts_BySKU()
    {
        // Arrange
        var config = CreateTestConfiguration();
        config.Monitoring.ProductSkus = new List<string> { "UDR-US" };
        config.Service.CheckIntervalSeconds = 1;

        _mockConfigProvider
            .Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var callCount = 0;
        _mockStockService
            .Setup(x => x.GetStockAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // First check: unavailable
                if (callCount == 1)
                {
                    return new List<UnifiProduct>
                    {
                        new() { SKU = "USW-24", Name = "Switch 24", Available = false },
                        new() { SKU = "UDR-US", Name = "Dream Router", Available = false }
                    };
                }
                // Subsequent checks: available
                else
                {
                    return new List<UnifiProduct>
                    {
                        new() { SKU = "USW-24", Name = "Switch 24", Available = true },
                        new() { SKU = "UDR-US", Name = "Dream Router", Available = true }
                    };
                }
            });

        _mockOrchestrator
            .Setup(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true });

        var service = new UnifiWatchService(
            _mockConfigProvider.Object,
            _mockStockService.Object,
            _mockOrchestrator.Object,
            _mockLogger.Object
        );

        var cts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(cts.Token);
        await Task.Delay(2000); // Wait for 2 checks
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - Should only notify for specified SKU
        _mockOrchestrator.Verify(
            x => x.SendAsync(
                It.Is<NotificationMessage>(m =>
                    m.Products.Count == 1 &&
                    m.Products[0].SKU == "UDR-US"
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task Service_Should_FilterProducts_ByName()
    {
        // Arrange
        var config = CreateTestConfiguration();
        config.Monitoring.ProductNames = new List<string> { "Dream" };
        config.Service.CheckIntervalSeconds = 1;

        _mockConfigProvider
            .Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var callCount = 0;
        _mockStockService
            .Setup(x => x.GetStockAsync(It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                // First check: unavailable
                if (callCount == 1)
                {
                    return new List<UnifiProduct>
                    {
                        new() { SKU = "USW-24", Name = "Switch 24", Available = false },
                        new() { SKU = "UDR-US", Name = "Dream Router", Available = false },
                        new() { SKU = "UDM-SE", Name = "Dream Machine SE", Available = false }
                    };
                }
                // Subsequent checks: available
                else
                {
                    return new List<UnifiProduct>
                    {
                        new() { SKU = "USW-24", Name = "Switch 24", Available = true },
                        new() { SKU = "UDR-US", Name = "Dream Router", Available = true },
                        new() { SKU = "UDM-SE", Name = "Dream Machine SE", Available = true }
                    };
                }
            });

        _mockOrchestrator
            .Setup(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationResult { Success = true });

        var service = new UnifiWatchService(
            _mockConfigProvider.Object,
            _mockStockService.Object,
            _mockOrchestrator.Object,
            _mockLogger.Object
        );

        var cts = new CancellationTokenSource();

        // Act
        var serviceTask = service.StartAsync(cts.Token);
        await Task.Delay(2000); // Wait for 2 checks
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        // Assert - Should notify only for products matching name filter (both Dream products = 2)
        _mockOrchestrator.Verify(
            x => x.SendAsync(
                It.Is<NotificationMessage>(m =>
                    m.Products.Count == 2 &&
                    m.Products.All(p => p.Name.Contains("Dream"))
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void Service_Should_ThrowArgumentNullException_ForNullDependencies()
    {
        // Assert - All required dependencies
        Assert.Throws<ArgumentNullException>(() =>
            new UnifiWatchService(null!, _mockStockService.Object, _mockOrchestrator.Object, _mockLogger.Object));

        Assert.Throws<ArgumentNullException>(() =>
            new UnifiWatchService(_mockConfigProvider.Object, null!, _mockOrchestrator.Object, _mockLogger.Object));

        Assert.Throws<ArgumentNullException>(() =>
            new UnifiWatchService(_mockConfigProvider.Object, _mockStockService.Object, null!, _mockLogger.Object));

        Assert.Throws<ArgumentNullException>(() =>
            new UnifiWatchService(_mockConfigProvider.Object, _mockStockService.Object, _mockOrchestrator.Object, null!));
    }

    private ConfigServiceConfiguration CreateTestConfiguration()
    {
        return new ConfigServiceConfiguration
        {
            Service = new ConfigServiceSettings
            {
                Enabled = true,
                Paused = false,
                CheckIntervalSeconds = 300
            },
            Monitoring = new ConfigMonitoringSettings
            {
                Store = "USA",
                Collections = new List<string>(),
                ProductSkus = new List<string>(),
                ProductNames = new List<string>()
            },
            Notifications = new ConfigNotificationSettings
            {
                Desktop = new DesktopNotificationConfig { Enabled = true },
                Email = new EmailNotificationConfig { Enabled = false },
                Sms = new SmsNotificationConfig { Enabled = false }
            },
            Credentials = new ConfigCredentialSettings
            {
                StorageMethod = "environment"
            }
        };
    }
}
