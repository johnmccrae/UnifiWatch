using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UnifiWatch.Configuration;
using Xunit;

namespace UnifiWatch.Tests;

public class ConfigurationProviderTests
{
    private readonly Mock<ILogger<ConfigurationProvider>> _mockLogger;
    private string _testConfigDir;

    public ConfigurationProviderTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationProvider>>();
        _testConfigDir = Path.Combine(Path.GetTempPath(), "UnifiWatch-Test-" + Guid.NewGuid());
    }

    private void Cleanup()
    {
        if (Directory.Exists(_testConfigDir))
        {
            Directory.Delete(_testConfigDir, recursive: true);
        }
    }

    [Fact]
    public void Constructor_ShouldCreateConfigurationDirectory()
    {
        // Arrange & Act
        var provider = new ConfigurationProvider(_mockLogger.Object);

        // Assert
        var configDir = provider.ConfigurationDirectory;
        Directory.Exists(configDir).Should().BeTrue();
    }

    [Fact]
    public void GetDefaultConfiguration_ShouldReturnValidConfig()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);

        // Act
        var config = provider.GetDefaultConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.Service.Enabled.Should().BeTrue();
        config.Service.CheckIntervalSeconds.Should().Be(300);
        config.Monitoring.Store.Should().Be("USA");
        config.Notifications.Desktop.Enabled.Should().BeTrue();
        config.Credentials.StorageMethod.Should().Be("auto");
    }

    [Fact]
    public void Validate_WithDefaultConfig_ShouldReturnNoErrors()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);
        var config = provider.GetDefaultConfiguration();

        // Act
        var errors = provider.Validate(config);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithInvalidCheckInterval_ShouldReturnError()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);
        var config = provider.GetDefaultConfiguration();
        config.Service.CheckIntervalSeconds = 5; // Too low

        // Act
        var errors = provider.Validate(config);

        // Assert
        errors.Should().Contain(e => e.Contains("CheckIntervalSeconds"));
    }

    [Fact]
    public void Validate_WithNullStore_ShouldReturnError()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);
        var config = provider.GetDefaultConfiguration();
        config.Monitoring.Store = "";

        // Act
        var errors = provider.Validate(config);

        // Assert
        errors.Should().Contain(e => e.Contains("Store"));
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_ShouldReturnSameConfig()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);
        var config = provider.GetDefaultConfiguration();
        config.Monitoring.Store = "Europe";
        config.Service.CheckIntervalSeconds = 600;
        config.Monitoring.ProductNames.Add("Dream Machine");

        // Act
        var saved = await provider.SaveAsync(config);
        var loaded = await provider.LoadAsync();

        // Assert
        saved.Should().BeTrue();
        loaded.Should().NotBeNull();
        loaded.Monitoring.Store.Should().Be("Europe");
        loaded.Service.CheckIntervalSeconds.Should().Be(600);
        loaded.Monitoring.ProductNames.Should().Contain("Dream Machine");
    }

    [Fact]
    public async Task LoadAsync_WhenFileDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);

        // First delete any existing config
        if (provider.ConfigurationExists())
        {
            await provider.DeleteAsync();
        }

        // Act
        var config = await provider.LoadAsync();

        // Assert
        config.Should().BeNull();
    }

    [Fact]
    public async Task ConfigurationExists_AfterSave_ShouldReturnTrue()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);
        var config = provider.GetDefaultConfiguration();

        // Act
        await provider.SaveAsync(config);
        var exists = provider.ConfigurationExists();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveConfigurationFile()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);
        var config = provider.GetDefaultConfiguration();
        await provider.SaveAsync(config);

        // Act
        var deleted = await provider.DeleteAsync();
        var exists = provider.ConfigurationExists();

        // Assert
        deleted.Should().BeTrue();
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task BackupAsync_ShouldCreateBackupFile()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);
        var config = provider.GetDefaultConfiguration();
        await provider.SaveAsync(config);
        
        // Small delay to ensure different timestamp
        await Task.Delay(100);

        // Act
        var backupPath = await provider.BackupAsync();

        // Assert
        backupPath.Should().NotBeNullOrEmpty();
        File.Exists(backupPath).Should().BeTrue();
        backupPath.Should().Contain("backup");
    }

    [Fact]
    public async Task SaveAsync_WithEmailConfig_ShouldPersistCorrectly()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);
        var config = provider.GetDefaultConfiguration();
        config.Notifications.Email.Enabled = true;
        config.Notifications.Email.Recipients.Add("test@example.com");
        config.Notifications.Email.SmtpServer = "smtp.gmail.com";
        config.Notifications.Email.SmtpPort = 587;
        config.Notifications.Email.SenderEmail = "sender@example.com";

        // Act
        await provider.SaveAsync(config);
        var loaded = await provider.LoadAsync();

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Notifications.Email.Enabled.Should().BeTrue();
        loaded.Notifications.Email.Recipients.Should().Contain("test@example.com");
        loaded.Notifications.Email.SmtpServer.Should().Be("smtp.gmail.com");
        loaded.Notifications.Email.SmtpPort.Should().Be(587);
    }

    [Fact]
    public async Task SaveAsync_WithSmsConfig_ShouldPersistCorrectly()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);
        var config = provider.GetDefaultConfiguration();
        config.Notifications.Sms.Enabled = true;
        config.Notifications.Sms.Provider = "twilio";
        config.Notifications.Sms.Recipients.Add("+15551234567");

        // Act
        await provider.SaveAsync(config);
        var loaded = await provider.LoadAsync();

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Notifications.Sms.Enabled.Should().BeTrue();
        loaded.Notifications.Sms.Provider.Should().Be("twilio");
        loaded.Notifications.Sms.Recipients.Should().Contain("+15551234567");
    }

    [Fact]
    public void EmailNotificationConfig_IsValid_WhenEnabledAndComplete_ShouldReturnTrue()
    {
        // Arrange
        var config = new EmailNotificationConfig
        {
            Enabled = true,
            Recipients = new() { "test@example.com" },
            SmtpServer = "smtp.example.com",
            SmtpPort = 587,
            FromAddress = "sender@example.com",
            CredentialKey = "email-smtp"
        };

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void EmailNotificationConfig_IsValid_WhenDisabled_ShouldReturnTrue()
    {
        // Arrange
        var config = new EmailNotificationConfig { Enabled = false };

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void EmailNotificationConfig_IsValid_WhenEnabledButMissingRecipients_ShouldReturnFalse()
    {
        // Arrange
        var config = new EmailNotificationConfig
        {
            Enabled = true,
            SmtpServer = "smtp.example.com",
            SmtpPort = 587,
            SenderEmail = "sender@example.com",
        };

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void SmsNotificationConfig_IsValid_WhenEnabledAndComplete_ShouldReturnTrue()
    {
        // Arrange
        var config = new SmsNotificationConfig
        {
            Enabled = true,
            ServiceType = "twilio",
            ToPhoneNumbers = new() { "+15551234567" },
            FromPhoneNumber = "+15557654321",
            TwilioAccountSid = "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
            AuthTokenKeyName = "sms:twilio:auth-token"
        };

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void SmsNotificationConfig_IsValid_WhenDisabled_ShouldReturnTrue()
    {
        // Arrange
        var config = new SmsNotificationConfig { Enabled = false };

        // Act
        var isValid = config.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_CreateMultipleBackups_ShouldSucceed()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);
        var config = provider.GetDefaultConfiguration();

        // Act & Assert
        for (int i = 0; i < 3; i++)
        {
            config.Service.CheckIntervalSeconds = 300 + (i * 100);
            await provider.SaveAsync(config);
            await Task.Delay(100); // Ensure different timestamp for each backup
            var backupPath = await provider.BackupAsync();
            backupPath.Should().NotBeNullOrEmpty();
            File.Exists(backupPath).Should().BeTrue();
        }
    }

    [Fact]
    public async Task LoadAsync_WithInvalidJson_ShouldReturnNull()
    {
        // Arrange
        var provider = new ConfigurationProvider(_mockLogger.Object);
        Directory.CreateDirectory(provider.ConfigurationDirectory);
        
        // Write invalid JSON
        await File.WriteAllTextAsync(provider.ConfigurationFilePath, "{ invalid json }", System.Text.Encoding.UTF8);

        // Act
        var config = await provider.LoadAsync();

        // Assert
        config.Should().BeNull();

        // Cleanup
        File.Delete(provider.ConfigurationFilePath);
    }
}
