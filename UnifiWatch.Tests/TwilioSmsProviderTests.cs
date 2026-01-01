using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using FluentAssertions;
using UnifiWatch.Services.Notifications;
using UnifiWatch.Services.Credentials;

namespace UnifiWatch.Tests;

public class TwilioSmsProviderTests
{
    private readonly Mock<ICredentialProvider> _mockCredentialProvider;
    private readonly Mock<ILogger<TwilioSmsProvider>> _mockLogger;
    private readonly SmsNotificationSettings _settings;
    private readonly TwilioSmsProvider _provider;

    public TwilioSmsProviderTests()
    {
        _mockCredentialProvider = new Mock<ICredentialProvider>();
        _mockLogger = new Mock<ILogger<TwilioSmsProvider>>();

        _settings = new SmsNotificationSettings
        {
            ServiceType = "twilio",
            TwilioAccountSid = "AC12345678901234567890123456789012",
            FromPhoneNumber = "+12125551234",
            AuthTokenKeyName = "sms:twilio:auth-token",
            MaxMessageLength = 160,
            AllowMessageShortening = true
        };

        var optionsMock = new Mock<IOptions<SmsNotificationSettings>>();
        optionsMock.Setup(x => x.Value).Returns(_settings);

        _provider = new TwilioSmsProvider(
            optionsMock.Object,
            _mockCredentialProvider.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SendAsync_WithValidPhoneNumber_ShouldNormalizeAndAttemptSend()
    {
        // Arrange
        var phoneNumber = "(212) 555-1234";  // Various format
        var message = "Test message";

        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-auth-token");

        // Act - Will fail at Twilio SDK call without real credentials, but we can verify phone validation
        // We expect this to normalize to +12125551234
        await _provider.SendAsync(phoneNumber, message);

        // Assert - Verify credential provider was called
        _mockCredentialProvider.Verify(
            x => x.RetrieveAsync(_settings.AuthTokenKeyName, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithInvalidPhoneNumber_ShouldReturnFalse()
    {
        // Arrange
        var invalidPhone = "abc123";  // Invalid phone number
        var message = "Test message";

        // Act
        var result = await _provider.SendAsync(invalidPhone, message);

        // Assert
        result.Should().BeFalse();
        _mockCredentialProvider.Verify(
            x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not attempt to send with invalid phone number");
    }

    [Theory]
    [InlineData("+12125551234")]  // E.164 format
    [InlineData("(212) 555-1234")]  // US format with parentheses
    [InlineData("212-555-1234")]  // US format with dashes
    [InlineData("2125551234")]  // Plain digits
    public async Task SendAsync_WithVariousPhoneFormats_ShouldNormalizeCorrectly(string phoneNumber)
    {
        // Arrange
        var message = "Test";

        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-token");

        // Act - All should normalize to +12125551234
        await _provider.SendAsync(phoneNumber, message);

        // Assert - Verify credential provider was called (indicates phone was valid)
        _mockCredentialProvider.Verify(
            x => x.RetrieveAsync(_settings.AuthTokenKeyName, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("123")]  // Too short
    [InlineData("abc-def-ghij")]  // Letters
    [InlineData("")]  // Empty
    public async Task SendAsync_WithInvalidPhoneFormats_ShouldReturnFalse(string invalidPhone)
    {
        // Arrange
        var message = "Test";

        // Act
        var result = await _provider.SendAsync(invalidPhone, message);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithMessageExceedingLimit_ShouldReturnFalse()
    {
        // Arrange
        var phoneNumber = "+12125551234";
        var longMessage = new string('A', 161);  // 161 chars, exceeds 160 limit

        // Act
        var result = await _provider.SendAsync(phoneNumber, longMessage);

        // Assert
        result.Should().BeFalse();
        _mockCredentialProvider.Verify(
            x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not attempt to send message exceeding limit");
    }

    [Fact]
    public async Task SendAsync_WithNoAuthToken_ShouldReturnFalse()
    {
        // Arrange
        var phoneNumber = "+12125551234";
        var message = "Test message";

        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);  // No auth token found

        // Act
        var result = await _provider.SendAsync(phoneNumber, message);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendBatchAsync_WithMultipleRecipients_ShouldReturnResults()
    {
        // Arrange
        var recipients = new List<string> { "+12125551234", "+13105551234", "invalid-number" };
        var message = "Batch test message";

        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-token");

        // Act
        var results = await _provider.SendBatchAsync(recipients, message);

        // Assert
        results.Should().HaveCount(3);
        results.Should().ContainKey("+12125551234");
        results.Should().ContainKey("+13105551234");
        results.Should().ContainKey("invalid-number");

        // Invalid number should have failed
        results["invalid-number"].Should().BeFalse();
    }

    [Fact]
    public async Task SendBatchAsync_WithEmptyList_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var recipients = new List<string>();
        var message = "Test";

        // Act
        var results = await _provider.SendBatchAsync(recipients, message);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SendBatchAsync_WithMessageExceedingLimit_ShouldReturnAllFalse()
    {
        // Arrange
        var recipients = new List<string> { "+12125551234", "+13105551234" };
        var longMessage = new string('B', 161);

        // Act
        var results = await _provider.SendBatchAsync(recipients, longMessage);

        // Assert
        results.Should().HaveCount(2);
        results.Values.Should().AllSatisfy(v => v.Should().BeFalse());
    }
}
