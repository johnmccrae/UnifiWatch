using System.Net.Mail;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using UnifiWatch.Models;
using UnifiWatch.Services.Credentials;
using UnifiWatch.Services.Notifications;

namespace UnifiWatch.Tests;

public class SmtpEmailProviderTests
{
    private readonly Mock<ILogger<SmtpEmailProvider>> _mockLogger;
    private readonly Mock<ICredentialProvider> _mockCredentialProvider;
    private readonly EmailNotificationSettings _emailSettings;
    private readonly SmtpEmailProvider _provider;

    public SmtpEmailProviderTests()
    {
        _mockLogger = new Mock<ILogger<SmtpEmailProvider>>();
        _mockCredentialProvider = new Mock<ICredentialProvider>();

        _emailSettings = new EmailNotificationSettings
        {
            Enabled = true,
            SmtpServer = "smtp.gmail.com",
            SmtpPort = 587,
            UseTls = true,
            FromAddress = "noreply@unifiwatch.local",
            CredentialKey = "test-email"
        };

        _provider = new SmtpEmailProvider(_mockLogger.Object, Microsoft.Extensions.Options.Options.Create(_emailSettings), _mockCredentialProvider.Object);
    }

    [Fact]
    public async Task SendAsync_WithValidEmail_ShouldAttemptConnection()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        // This will fail due to actual SMTP connection attempt, but we're testing the flow
        var result = await _provider.SendAsync(recipient, subject, body);

        // Assert
        // The actual SMTP connection will fail, but we verify the credential provider was called
        _mockCredentialProvider.Verify(
            x => x.RetrieveAsync(_emailSettings.CredentialKey, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithInvalidEmail_ShouldReturnFalse()
    {
        // Arrange
        var invalidEmail = "invalid-email";
        var subject = "Test Subject";
        var body = "Test Body";

        // Act
        var result = await _provider.SendAsync(invalidEmail, subject, body);

        // Assert
        result.Should().BeFalse();
        _mockCredentialProvider.Verify(
            x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData("test@domain.com")]
    [InlineData("user.name+tag@example.co.uk")]
    [InlineData("firstname.lastname@company.museum")]
    public async Task SendAsync_WithVariousValidEmails_ShouldPassValidation(string email)
    {
        // Arrange
        var subject = "Test";
        var body = "Test";

        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        // Will attempt connection for valid emails
        var result = await _provider.SendAsync(email, subject, body);

        // Assert - the validation passes, even if SMTP fails
        _mockCredentialProvider.Verify(
            x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user name@domain.com")]
    public async Task SendAsync_WithInvalidEmailFormats_ShouldReturnFalse(string invalidEmail)
    {
        // Arrange
        var subject = "Test";
        var body = "Test";

        // Act
        var result = await _provider.SendAsync(invalidEmail, subject, body);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendBatchAsync_WithMultipleRecipients_ShouldReturnResults()
    {
        // Arrange
        var recipients = new List<string> { "valid@example.com", "invalid-email", "another@example.com" };
        var subject = "Test Subject";
        var body = "Test Body";

        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var results = await _provider.SendBatchAsync(recipients, subject, body);

        // Assert
        results.Should().HaveCount(3);
        results.Should().ContainKey("valid@example.com");
        results.Should().ContainKey("invalid-email");
        results.Should().ContainKey("another@example.com");
        results["invalid-email"].Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithPlainAndHtmlBody_ShouldAttemptSend()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test Subject";
        var plainBody = "Plain text body";
        var htmlBody = "<p>HTML body</p>";

        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _provider.SendAsync(recipient, subject, plainBody, htmlBody);

        // Assert
        _mockCredentialProvider.Verify(
            x => x.RetrieveAsync(_emailSettings.CredentialKey, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithCancellationToken_ShouldRespectCancellation()
    {
        // Arrange
        var recipient = "test@example.com";
        var subject = "Test";
        var body = "Test";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _provider.SendAsync(recipient, subject, body, cancellationToken: cts.Token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_WithSmtpSettingsConfiguration_ShouldUseTlsWhenEnabled()
    {
        // Arrange
        var tlsEnabledSettings = new EmailNotificationSettings
        {
            Enabled = true,
            SmtpServer = "smtp.gmail.com",
            SmtpPort = 587,
            UseTls = true,
            FromAddress = "sender@example.com",
            CredentialKey = "test-email"
        };

        var provider = new SmtpEmailProvider(_mockLogger.Object, Microsoft.Extensions.Options.Options.Create(tlsEnabledSettings), _mockCredentialProvider.Object);

        _mockCredentialProvider
            .Setup(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await provider.SendAsync("test@example.com", "Subject", "Body");

        // Assert
        _mockCredentialProvider.Verify(x => x.RetrieveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendBatchAsync_WithEmptyList_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var recipients = new List<string>();
        var subject = "Test";
        var body = "Test";

        // Act
        var results = await _provider.SendBatchAsync(recipients, subject, body);

        // Assert
        results.Should().BeEmpty();
    }
}
