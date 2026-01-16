using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using UnifiWatch.Configuration;
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
/// Tests for NotificationOrchestrator multi-channel notification coordination
/// </summary>
public class NotificationOrchestratorTests
{
    private readonly Mock<INotificationProvider> _mockSmtpProvider;
    private readonly Mock<INotificationProvider> _mockGraphProvider;
    private readonly Mock<ISmsProvider> _mockSmsProvider;
    private readonly Mock<ILogger<NotificationOrchestrator>> _mockLogger;
    private readonly ConfigServiceConfiguration _config;

    public NotificationOrchestratorTests()
    {
        _mockSmtpProvider = new Mock<INotificationProvider>();
        _mockSmtpProvider.Setup(x => x.ProviderName).Returns("SMTP Email");
        
        _mockGraphProvider = new Mock<INotificationProvider>();
        _mockGraphProvider.Setup(x => x.ProviderName).Returns("Microsoft Graph");
        
        _mockSmsProvider = new Mock<ISmsProvider>();
        _mockSmsProvider.Setup(x => x.ProviderName).Returns("Twilio SMS");

        _mockLogger = new Mock<ILogger<NotificationOrchestrator>>();

        _config = new ConfigServiceConfiguration
        {
            Service = new ConfigServiceSettings(),
            Monitoring = new ConfigMonitoringSettings(),
            Notifications = new ConfigNotificationSettings
            {
                Desktop = new DesktopNotificationConfig { Enabled = true },
                Email = new EmailNotificationConfig
                {
                    Enabled = false,
                    Provider = "smtp",
                    FromAddress = "test@example.com",
                    ToAddresses = new List<string> { "recipient@example.com" }
                },
                Sms = new SmsNotificationConfig
                {
                    Enabled = false,
                    ServiceType = "twilio",
                    FromPhoneNumber = "+15551234567",
                    ToPhoneNumbers = new List<string> { "+12125551234" }
                }
            },
            Credentials = new ConfigCredentialSettings()
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitialize()
    {
        // Act
        var orchestrator = new NotificationOrchestrator(
            _config,
            _mockSmtpProvider.Object,
            _mockGraphProvider.Object,
            _mockSmsProvider.Object,
            _mockLogger.Object);

        // Assert
        orchestrator.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new NotificationOrchestrator(
                null!,
                _mockSmtpProvider.Object,
                _mockGraphProvider.Object,
                _mockSmsProvider.Object,
                _mockLogger.Object));
    }

    #endregion

    #region SendAsync Tests

    [Fact]
    public async Task SendAsync_WithNullNotification_ShouldThrowArgumentNull()
    {
        // Arrange
        var orchestrator = new NotificationOrchestrator(
            _config,
            _mockSmtpProvider.Object,
            _mockGraphProvider.Object,
            _mockSmsProvider.Object,
            _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            orchestrator.SendAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SendAsync_WithNoChannelsEnabled_ShouldReturnSuccess()
    {
        // Arrange
        _config.Notifications.Desktop.Enabled = false;
        _config.Notifications.Email.Enabled = false;
        _config.Notifications.Sms.Enabled = false;

        var orchestrator = new NotificationOrchestrator(
            _config,
            _mockSmtpProvider.Object,
            _mockGraphProvider.Object,
            _mockSmsProvider.Object,
            _mockLogger.Object);

        var notification = new NotificationMessage
        {
            Subject = "Test",
            TextBody = "Test message",
            Recipients = new List<string> { "test@example.com" }
        };

        // Act
        var result = await orchestrator.SendAsync(notification, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.DesktopSuccess.Should().BeFalse();
        result.EmailSuccess.Should().BeFalse();
        result.SmsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithDesktopEnabled_ShouldAttemptDesktopNotification()
    {
        // Arrange
        _config.Notifications.Desktop.Enabled = true;
        var orchestrator = new NotificationOrchestrator(
            _config,
            _mockSmtpProvider.Object,
            _mockGraphProvider.Object,
            _mockSmsProvider.Object,
            _mockLogger.Object);

        var notification = new NotificationMessage
        {
            Subject = "Test Alert",
            TextBody = "Test message",
            Recipients = new List<string>()
        };

        // Act
        var result = await orchestrator.SendAsync(notification, CancellationToken.None);

        // Assert
        result.DesktopSuccess.Should().BeTrue();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WithEmailEnabled_ShouldAttemptEmailNotification()
    {
        // Arrange
        _config.Notifications.Email.Enabled = true;
        _config.Notifications.Desktop.Enabled = false;

        _mockSmtpProvider
            .Setup(x => x.IsConfigured)
            .Returns(true);

        _mockSmtpProvider
            .Setup(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var orchestrator = new NotificationOrchestrator(
            _config,
            _mockSmtpProvider.Object,
            _mockGraphProvider.Object,
            _mockSmsProvider.Object,
            _mockLogger.Object);

        var notification = new NotificationMessage
        {
            Subject = "Test Email",
            TextBody = "Test message",
            Recipients = new List<string> { "test@example.com" }
        };

        // Act
        var result = await orchestrator.SendAsync(notification, CancellationToken.None);

        // Assert
        result.EmailSuccess.Should().BeTrue();
        _mockSmtpProvider.Verify(x => x.SendAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithSmsEnabled_ShouldAttemptSmsNotification()
    {
        // Arrange
        _config.Notifications.Sms.Enabled = true;
        _config.Notifications.Desktop.Enabled = false;

        _mockSmsProvider
            .Setup(x => x.IsConfigured)
            .Returns(true);

        _mockSmsProvider
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var orchestrator = new NotificationOrchestrator(
            _config,
            _mockSmtpProvider.Object,
            _mockGraphProvider.Object,
            _mockSmsProvider.Object,
            _mockLogger.Object);

        var notification = new NotificationMessage
        {
            Subject = "Test SMS",
            TextBody = "Test message",
            Recipients = new List<string>()
        };

        // Act
        var result = await orchestrator.SendAsync(notification, CancellationToken.None);

        // Assert
        result.SmsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_WithMultipleChannelsEnabled_ShouldSendToAll()
    {
        // Arrange
        _config.Notifications.Desktop.Enabled = true;
        _config.Notifications.Email.Enabled = true;
        _config.Notifications.Sms.Enabled = true;

        _mockSmtpProvider.Setup(x => x.IsConfigured).Returns(true);
        _mockSmtpProvider
            .Setup(x => x.SendAsync(It.IsAny<NotificationMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockSmsProvider.Setup(x => x.IsConfigured).Returns(true);
        _mockSmsProvider
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var orchestrator = new NotificationOrchestrator(
            _config,
            _mockSmtpProvider.Object,
            _mockGraphProvider.Object,
            _mockSmsProvider.Object,
            _mockLogger.Object);

        var notification = new NotificationMessage
        {
            Subject = "Multi-Channel Alert",
            TextBody = "Test message",
            Recipients = new List<string> { "test@example.com" }
        };

        // Act
        var result = await orchestrator.SendAsync(notification, CancellationToken.None);

        // Assert
        result.DesktopSuccess.Should().BeTrue();
        result.EmailSuccess.Should().BeTrue();
        result.SmsSuccess.Should().BeTrue();
        result.Success.Should().BeTrue();
    }

    #endregion

    #region TestAllChannelsAsync Tests

    [Fact]
    public async Task TestAllChannelsAsync_WithNoChannelsEnabled_ShouldReturnNotConfigured()
    {
        // Arrange
        _config.Notifications.Desktop.Enabled = false;
        _config.Notifications.Email.Enabled = false;
        _config.Notifications.Sms.Enabled = false;

        var orchestrator = new NotificationOrchestrator(
            _config,
            _mockSmtpProvider.Object,
            _mockGraphProvider.Object,
            _mockSmsProvider.Object,
            _mockLogger.Object);

        // Act
        var result = await orchestrator.TestAllChannelsAsync(CancellationToken.None);

        // Assert
        result.DesktopAvailable.Should().BeFalse();
        result.EmailTestResult.Should().Be("Not configured");
        result.SmsTestResult.Should().Be("Not configured");
    }

    [Fact]
    public async Task TestAllChannelsAsync_WithEmailConfigured_ShouldTestEmail()
    {
        // Arrange
        _config.Notifications.Email.Enabled = true;

        _mockSmtpProvider.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var orchestrator = new NotificationOrchestrator(
            _config,
            _mockSmtpProvider.Object,
            _mockGraphProvider.Object,
            _mockSmsProvider.Object,
            _mockLogger.Object);

        // Act
        var result = await orchestrator.TestAllChannelsAsync(CancellationToken.None);

        // Assert
        result.EmailTestResult.Should().Be("Connected");
    }

    #endregion

    #region NotificationResult Tests

    [Fact]
    public void NotificationResult_ShouldTrackChannelStatus()
    {
        // Arrange & Act
        var result = new NotificationResult
        {
            DesktopSuccess = true,
            EmailSuccess = false,
            SmsSuccess = true
        };

        // Assert
        result.DesktopSuccess.Should().BeTrue();
        result.EmailSuccess.Should().BeFalse();
        result.SmsSuccess.Should().BeTrue();
        result.AttemptedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region NotificationChannelTestResults Tests

    [Fact]
    public void NotificationChannelTestResults_ToString_ShouldProvideReadableOutput()
    {
        // Arrange
        var results = new NotificationChannelTestResults
        {
            DesktopAvailable = true,
            EmailTestResult = "Connected",
            SmsTestResult = "Failed"
        };

        // Act
        var output = results.ToString();

        // Assert
        output.Should().Contain("Desktop: Available");
        output.Should().Contain("Email: Connected");
        output.Should().Contain("SMS: Failed");
    }

    #endregion
}
