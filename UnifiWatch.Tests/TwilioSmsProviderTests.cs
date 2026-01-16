using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using UnifiWatch.Configuration;
using UnifiWatch.Services.Credentials;
using UnifiWatch.Services.Notifications.Sms;

namespace UnifiWatch.Tests;

/// <summary>
/// Tests for TwilioSmsProvider
/// </summary>
public class TwilioSmsProviderTests
{
    private readonly Mock<ICredentialProvider> _mockCredentialProvider;
    private readonly Mock<ILogger<TwilioSmsProvider>> _mockLogger;
    private readonly HttpClient _httpClient;
    private readonly SmsNotificationConfig _config;

    public TwilioSmsProviderTests()
    {
        _mockCredentialProvider = new Mock<ICredentialProvider>();
        _mockLogger = new Mock<ILogger<TwilioSmsProvider>>();
        _httpClient = new HttpClient();
        
        _config = new SmsNotificationConfig
        {
            Enabled = true,
            ServiceType = "twilio",
            TwilioAccountSid = "AC1234567890abcdef1234567890abcdef",
            FromPhoneNumber = "+15551234567",
            ToPhoneNumbers = new List<string> { "+12125551234" },
            AuthTokenKeyName = "sms:twilio:auth-token",
            MaxMessageLength = 160,
            AllowMessageShortening = true
        };
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_WithValidConfig_ShouldInitialize()
    {
        // Act
        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullConfig_ShouldThrowArgumentNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TwilioSmsProvider(null!, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient));
    }

    [Fact]
    public void Constructor_WithNullCredentialProvider_ShouldThrowArgumentNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TwilioSmsProvider(_config, null!, _mockLogger.Object, _httpClient));
    }

    #endregion

    #region IsConfigured Tests

    [Fact]
    public void IsConfigured_WithAllRequiredFields_ShouldReturnTrue()
    {
        // Arrange
        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Act
        var result = provider.IsConfigured;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsConfigured_WithoutAccountSid_ShouldReturnFalse()
    {
        // Arrange
        _config.TwilioAccountSid = "";
        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Act
        var result = provider.IsConfigured;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsConfigured_WithoutFromPhoneNumber_ShouldReturnFalse()
    {
        // Arrange
        _config.FromPhoneNumber = "";
        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Act
        var result = provider.IsConfigured;

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ProviderName Tests

    [Fact]
    public void ProviderName_ShouldReturnCorrectName()
    {
        // Arrange
        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Act
        var name = provider.ProviderName;

        // Assert
        name.Should().Be("Twilio SMS");
    }

    #endregion

    #region MaxMessageLength Tests

    [Fact]
    public void MaxMessageLength_ShouldReturnConfiguredValue()
    {
        // Arrange
        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Act
        var length = provider.MaxMessageLength;

        // Assert
        length.Should().Be(160);
    }

    #endregion

    #region SendAsync Tests

    [Fact]
    public async Task SendAsync_WithNotConfigured_ShouldReturnFalse()
    {
        // Arrange
        _config.TwilioAccountSid = "";
        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Act
        var result = await provider.SendAsync("Test message", _config.ToPhoneNumbers, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithEmptyMessage_ShouldReturnFalse()
    {
        // Arrange
        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Act
        var result = await provider.SendAsync("", _config.ToPhoneNumbers, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithNoPhoneNumbers_ShouldReturnFalse()
    {
        // Arrange
        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Act
        var result = await provider.SendAsync("Test message", new List<string>(), CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithCredentialNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Act
        var result = await provider.SendAsync("Test message", _config.ToPhoneNumbers, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region TestConnectionAsync Tests

    [Fact]
    public async Task TestConnectionAsync_WithNotConfigured_ShouldReturnFalse()
    {
        // Arrange
        _config.TwilioAccountSid = "";
        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Act
        var result = await provider.TestConnectionAsync(CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task TestConnectionAsync_WithCredentialNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var provider = new TwilioSmsProvider(_config, _mockCredentialProvider.Object, _mockLogger.Object, _httpClient);

        // Act
        var result = await provider.TestConnectionAsync(CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
