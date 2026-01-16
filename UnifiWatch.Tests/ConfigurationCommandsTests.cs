using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.CommandLine;
using UnifiWatch.CLI;
using UnifiWatch.Configuration;
using UnifiWatch.Services.Credentials;
using Xunit;

namespace UnifiWatch.Tests;

public class ConfigurationCommandsTests
{
    private readonly Mock<IConfigurationProvider> _configProviderMock;
    private readonly Mock<ICredentialProvider> _credentialProviderMock;
    private readonly Mock<ILogger> _loggerMock;

    public ConfigurationCommandsTests()
    {
        _configProviderMock = new Mock<IConfigurationProvider>();
        _credentialProviderMock = new Mock<ICredentialProvider>();
        _loggerMock = new Mock<ILogger>();
    }

    [Fact]
    public void CreateConfigureCommand_ReturnsValidCommand()
    {
        // Act
        var command = ConfigurationCommands.CreateConfigureCommand(
            _configProviderMock.Object,
            _credentialProviderMock.Object,
            _loggerMock.Object);

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("configure");
        command.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateShowConfigCommand_ReturnsValidCommand()
    {
        // Act
        var command = ConfigurationCommands.CreateShowConfigCommand(
            _configProviderMock.Object,
            _loggerMock.Object);

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("show-config");
        command.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateResetConfigCommand_ReturnsValidCommand()
    {
        // Act
        var command = ConfigurationCommands.CreateResetConfigCommand(
            _configProviderMock.Object,
            _credentialProviderMock.Object,
            _loggerMock.Object);

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("reset-config");
        command.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateTestNotificationsCommand_ReturnsValidCommand()
    {
        // Act
        var command = ConfigurationCommands.CreateTestNotificationsCommand(
            _configProviderMock.Object,
            _loggerMock.Object);

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be("test-notifications");
        command.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ShowConfigCommand_WhenConfigDoesNotExist_DisplaysMessage()
    {
        // Arrange
        _configProviderMock.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceConfiguration?)null);

        var command = ConfigurationCommands.CreateShowConfigCommand(
            _configProviderMock.Object,
            _loggerMock.Object);

        // Act
        var result = await command.InvokeAsync("");

        // Assert
        result.Should().Be(1); // Expect non-zero exit code when config doesn't exist
        _configProviderMock.Verify(x => x.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ShowConfigCommand_WhenConfigExists_DisplaysConfiguration()
    {
        // Arrange
        var config = new ServiceConfiguration
        {
            Service = new ServiceSettings
            {
                Language = "en-US",
                CheckIntervalSeconds = 60,
                Enabled = true,
                AutoStart = true
            },
            Monitoring = new MonitoringSettings
            {
                Store = "USA",
                UseModernApi = true
            },
            Notifications = new NotificationSettings
            {
                Desktop = new DesktopNotificationConfig { Enabled = true },
                Email = new EmailNotificationConfig { Enabled = false },
                Sms = new SmsNotificationConfig { Enabled = false }
            }
        };

        _configProviderMock.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var command = ConfigurationCommands.CreateShowConfigCommand(
            _configProviderMock.Object,
            _loggerMock.Object);

        // Act
        var result = await command.InvokeAsync("");

        // Assert
        result.Should().Be(0); // Expect success
        _configProviderMock.Verify(x => x.LoadAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

}
