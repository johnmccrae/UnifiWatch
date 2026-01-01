using System.Globalization;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using UnifiWatch.Models;
using UnifiWatch.Services.Notifications;
using UnifiWatch.Services.Localization;

namespace UnifiWatch.Tests;

public class EmailNotificationServiceTests
{
    private readonly Mock<IEmailProvider> _mockEmailProvider;
    private readonly Mock<IResourceLocalizer> _mockResourceLocalizer;
    private readonly Mock<ILogger<EmailNotificationService>> _mockLogger;
    private readonly EmailNotificationService _service;

    public EmailNotificationServiceTests()
    {
        _mockEmailProvider = new Mock<IEmailProvider>();
        _mockResourceLocalizer = new Mock<IResourceLocalizer>();
        _mockLogger = new Mock<ILogger<EmailNotificationService>>();
        _service = new EmailNotificationService(_mockEmailProvider.Object, _mockResourceLocalizer.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SendProductInStockNotificationAsync_WithValidProduct_ShouldSendEmail()
    {
        // Arrange
        var product = new UnifiProduct
        {
            Name = "Test Product",
            SKU = "TEST-001",
            Price = 299.99m
        };
        var recipient = "test@example.com";
        var store = "USA";
        var culture = new CultureInfo("en-CA");

        _mockResourceLocalizer
            .Setup(x => x.Notification("Email.ProductInStock.Subject", product.Name))
            .Returns("✓ Product In Stock");

        _mockResourceLocalizer
            .Setup(x => x.Notification("Email.ProductInStock.Body", It.IsAny<object[]>()))
            .Returns("Great news! Product: Test Product");

        _mockEmailProvider
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendProductInStockNotificationAsync(product, recipient, store, culture);

        // Assert
        result.Should().BeTrue();
        _mockEmailProvider.Verify(
            x => x.SendAsync(recipient, It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendProductOutOfStockNotificationAsync_WithValidProduct_ShouldSendEmail()
    {
        // Arrange
        var product = new UnifiProduct
        {
            Name = "Test Product",
            SKU = "TEST-001"
        };
        var recipient = "test@example.com";
        var store = "USA";
        var culture = new CultureInfo("en-CA");

        _mockResourceLocalizer
            .Setup(x => x.Notification("Email.ProductOutOfStock.Subject", product.Name))
            .Returns("✗ Product Out Of Stock");

        _mockResourceLocalizer
            .Setup(x => x.Notification("Email.ProductOutOfStock.Body", It.IsAny<object[]>()))
            .Returns("Stock update: Product: Test Product");

        _mockEmailProvider
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendProductOutOfStockNotificationAsync(product, recipient, store, culture);

        // Assert
        result.Should().BeTrue();
        _mockEmailProvider.Verify(
            x => x.SendAsync(recipient, It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendErrorNotificationAsync_WithErrorMessage_ShouldSendEmail()
    {
        // Arrange
        var errorMessage = "SMTP connection failed";
        var recipient = "test@example.com";
        var culture = new CultureInfo("en-CA");

        _mockResourceLocalizer
            .Setup(x => x.Notification("Email.Error.Subject"))
            .Returns("UnifiWatch Error Notification");

        _mockResourceLocalizer
            .Setup(x => x.Notification("Email.Error.Body", It.IsAny<object[]>()))
            .Returns("An error occurred");

        _mockEmailProvider
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendErrorNotificationAsync(errorMessage, recipient, culture);

        // Assert
        result.Should().BeTrue();
        _mockEmailProvider.Verify(
            x => x.SendAsync(recipient, "UnifiWatch Error Notification", It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("fr-CA")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    [InlineData("it-IT")]
    [InlineData("fr-FR")]
    public async Task SendProductInStockNotificationAsync_WithVariousCultures_ShouldSendLocalizedEmail(string cultureName)
    {
        // Arrange
        var product = new UnifiProduct { Name = "Product", SKU = "SKU-001" };
        var recipient = "test@example.com";
        var store = "USA";
        var culture = new CultureInfo(cultureName);

        _mockResourceLocalizer
            .Setup(x => x.Notification("Email.ProductInStock.Subject", product.Name))
            .Returns($"[{cultureName}] Subject");

        _mockResourceLocalizer
            .Setup(x => x.Notification("Email.ProductInStock.Body", It.IsAny<object[]>()))
            .Returns($"[{cultureName}] Body");

        _mockEmailProvider
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendProductInStockNotificationAsync(product, store, recipient, culture);

        // Assert
        result.Should().BeTrue();
        _mockResourceLocalizer.Verify(x => x.Notification("Email.ProductInStock.Subject", product.Name), Times.Once);
        _mockResourceLocalizer.Verify(x => x.Notification("Email.ProductInStock.Body", It.IsAny<object[]>()), Times.Once);
    }

    [Fact]
    public async Task SendProductInStockNotificationAsync_WhenEmailProviderFails_ShouldReturnFalse()
    {
        // Arrange
        var product = new UnifiProduct { Name = "Product", SKU = "SKU-001" };
        var recipient = "test@example.com";
        var store = "USA";
        var culture = new CultureInfo("en-CA");

        _mockResourceLocalizer
            .Setup(x => x.Notification(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns("Template");

        _mockEmailProvider
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.SendProductInStockNotificationAsync(product, store, recipient, culture);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendBatchNotificationAsync_WithMultipleRecipients_ShouldSendToAll()
    {
        // Arrange
        var recipients = new List<string> { "user1@example.com", "user2@example.com", "user3@example.com" };
        var subject = "Test Subject";
        var body = "Test Body";

        _mockEmailProvider
            .Setup(x => x.SendBatchAsync(recipients, subject, body, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, bool>
            {
                { "user1@example.com", true },
                { "user2@example.com", true },
                { "user3@example.com", true }
            });

        // Act
        var results = await _service.SendBatchNotificationAsync(recipients, subject, body);

        // Assert
        results.Should().HaveCount(3);
        results.Values.Should().AllSatisfy(v => v.Should().BeTrue());
        _mockEmailProvider.Verify(x => x.SendBatchAsync(recipients, subject, body, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendProductInStockNotificationAsync_WithNullCulture_ShouldUseCurrentCulture()
    {
        // Arrange
        var product = new UnifiProduct { Name = "Product", SKU = "SKU-001" };
        var recipient = "test@example.com";
        var store = "USA";

        _mockResourceLocalizer
            .Setup(x => x.Notification(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns("Template");

        _mockEmailProvider
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendProductInStockNotificationAsync(product, store, recipient, null);

        // Assert
        result.Should().BeTrue();
    }
}
