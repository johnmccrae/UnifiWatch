using System.Globalization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using UnifiWatch.Models;
using UnifiWatch.Services.Notifications;
using UnifiWatch.Services.Localization;

namespace UnifiWatch.Tests;

public class SmsNotificationServiceTests
{
    private readonly Mock<ISmsProvider> _mockSmsProvider;
    private readonly Mock<IResourceLocalizer> _mockResourceLocalizer;
    private readonly Mock<ILogger<SmsNotificationService>> _mockLogger;
    private readonly Services.Notifications.SmsNotificationSettings _settings;
    private readonly SmsNotificationService _service;

    public SmsNotificationServiceTests()
    {
        _mockSmsProvider = new Mock<ISmsProvider>();
        _mockResourceLocalizer = new Mock<IResourceLocalizer>();
        _mockLogger = new Mock<ILogger<SmsNotificationService>>();

        _settings = new Services.Notifications.SmsNotificationSettings
        {
            MaxMessageLength = 160,
            AllowMessageShortening = true
        };

        _service = new SmsNotificationService(
            _mockSmsProvider.Object,
            _mockResourceLocalizer.Object,
            _mockLogger.Object,
            _settings);
    }

    [Fact]
    public async Task SendProductInStockNotificationAsync_WithValidProduct_ShouldSendSms()
    {
        // Arrange
        var product = new UnifiProduct
        {
            Name = "Dream Machine Pro",
            SKU = "UDM-PRO",
            Price = 379.00m
        };

        var recipient = "+12125551234";
        var store = "USA";
        var culture = new CultureInfo("en-CA");

        _mockResourceLocalizer
            .Setup(x => x.Notification("Sms.ProductInStock.Subject", product.Name))
            .Returns("✓ Dream Machine Pro In Stock");

        _mockResourceLocalizer
            .Setup(x => x.Notification("Sms.ProductInStock.Body", product.Name, product.SKU, "$379.00", store))
            .Returns("Good news! Dream Machine Pro (UDM-PRO) at USA. Price: $379.00. Check now!");

        _mockSmsProvider
            .Setup(x => x.SendAsync(recipient, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendProductInStockNotificationAsync(
            product, recipient, store, culture);

        // Assert
        result.Should().BeTrue();

        _mockSmsProvider.Verify(
            x => x.SendAsync(
                recipient,
                It.Is<string>(msg => msg.StartsWith("[IN STOCK] ") && msg.Contains("Dream Machine Pro") && msg.Contains("$379.00")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendProductOutOfStockNotificationAsync_WithValidProduct_ShouldSendSms()
    {
        // Arrange
        var product = new UnifiProduct
        {
            Name = "Dream Machine Pro",
            SKU = "UDM-PRO"
        };

        var recipient = "+12125551234";
        var store = "USA";

        _mockResourceLocalizer
            .Setup(x => x.Notification("Sms.ProductOutOfStock.Subject", product.Name))
            .Returns("✗ Dream Machine Pro Out of Stock");

        _mockResourceLocalizer
            .Setup(x => x.Notification("Sms.ProductOutOfStock.Body", product.Name, product.SKU, store))
            .Returns("Dream Machine Pro (UDM-PRO) at USA is out of stock. We'll notify you when available.");

        _mockSmsProvider
            .Setup(x => x.SendAsync(recipient, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendProductOutOfStockNotificationAsync(
            product, recipient, store);

        // Assert
        result.Should().BeTrue();

        _mockSmsProvider.Verify(
            x => x.SendAsync(recipient, It.Is<string>(msg => msg.StartsWith("[OUT OF STOCK] ") && msg.Contains("Out of Stock")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendErrorNotificationAsync_WithErrorMessage_ShouldSendSms()
    {
        // Arrange
        var errorMessage = "API connection failed";
        var recipient = "+12125551234";

        _mockResourceLocalizer
            .Setup(x => x.Notification("Sms.Error.Subject"))
            .Returns("UnifiWatch Alert");

        _mockResourceLocalizer
            .Setup(x => x.Notification("Sms.Error.Body", errorMessage))
            .Returns("Error: API connection failed");

        _mockSmsProvider
            .Setup(x => x.SendAsync(recipient, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendErrorNotificationAsync(errorMessage, recipient);

        // Assert
        result.Should().BeTrue();

        _mockSmsProvider.Verify(
            x => x.SendAsync(recipient, It.Is<string>(msg => msg.StartsWith("[ERROR] ") && msg.Contains("Alert") && msg.Contains("API connection failed")), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("fr-CA", "✓ Dream Machine Pro En Stock", "Bonne nouvelle!", "[EN STOCK] ")]
    [InlineData("de-DE", "✓ Dream Machine Pro Verfügbar", "Gute Nachrichten!", "[VERFÜGBAR] ")]
    [InlineData("es-ES", "✓ Dream Machine Pro Disponible", "¡Buenas noticias!", "[DISPONIBLE] ")]
    [InlineData("it-IT", "✓ Dream Machine Pro Disponibile", "Buone notizie!", "[DISPONIBILE] ")]
    [InlineData("fr-FR", "✓ Dream Machine Pro Disponible", "Bonne nouvelle!", "[EN STOCK] ")]
    public async Task SendProductInStockNotificationAsync_WithVariousCultures_ShouldSendLocalizedSms(
        string cultureCode, string expectedSubject, string expectedBodyPrefix, string expectedPrefix)
    {
        // Arrange
        var product = new UnifiProduct { Name = "Dream Machine Pro", SKU = "UDM-PRO", Price = 379.00m };
        var recipient = "+12125551234";
        var culture = new CultureInfo(cultureCode);

        _mockResourceLocalizer
            .Setup(x => x.Notification("Sms.ProductInStock.Subject", product.Name))
            .Returns(expectedSubject);

        _mockResourceLocalizer
            .Setup(x => x.Notification("Sms.ProductInStock.Body", It.IsAny<object[]>()))
            .Returns($"{expectedBodyPrefix} Dream Machine Pro (UDM-PRO) at USA. Price: 379,00 €. Check now!");

        _mockSmsProvider
            .Setup(x => x.SendAsync(recipient, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendProductInStockNotificationAsync(
            product, recipient, "USA", culture);

        // Assert
        result.Should().BeTrue();

        _mockSmsProvider.Verify(
            x => x.SendAsync(
                recipient,
                It.Is<string>(msg => msg.StartsWith(expectedPrefix) && msg.Contains(expectedBodyPrefix)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendProductInStockNotificationAsync_WhenSmsProviderFails_ShouldReturnFalse()
    {
        // Arrange
        var product = new UnifiProduct { Name = "Test Product", SKU = "TEST-001" };
        var recipient = "+12125551234";

        _mockResourceLocalizer
            .Setup(x => x.Notification(It.IsAny<string>(), It.IsAny<object[]>()))
            .Returns("Test message");

        _mockSmsProvider
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);  // Provider fails

        // Act
        var result = await _service.SendProductInStockNotificationAsync(product, recipient);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendBatchNotificationAsync_WithMultipleRecipients_ShouldSendToAll()
    {
        // Arrange
        var recipients = new List<string> { "+12125551234", "+13105551234", "+14155551234" };
        var subject = "Test Subject";
        var body = "Test body message";

        _mockSmsProvider
            .Setup(x => x.SendBatchAsync(recipients, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, bool>
            {
                { "+12125551234", true },
                { "+13105551234", true },
                { "+14155551234", true }
            });

        // Act
        var results = await _service.SendBatchNotificationAsync(recipients, subject, body);

        // Assert
        results.Should().HaveCount(3);
        results.Values.Should().AllSatisfy(v => v.Should().BeTrue());

        _mockSmsProvider.Verify(
            x => x.SendBatchAsync(recipients, It.Is<string>(msg => msg.Contains(subject) && msg.Contains(body)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendProductInStockNotificationAsync_WithLongMessage_ShouldShortenToFit()
    {
        // Arrange
        var product = new UnifiProduct
        {
            Name = "UniFi Dream Machine Pro with Ultra High Performance and Advanced Security Features",
            SKU = "UDM-PRO-ULTRA-ADVANCED-2024",
            Price = 379.00m
        };

        var recipient = "+12125551234";
        var store = "United States of America - Official Ubiquiti Store";

        // Create a message that will exceed 160 chars
        var longSubject = "✓ UniFi Dream Machine Pro with Ultra High Performance and Advanced Security Features In Stock";
        var longBody = "Good news! UniFi Dream Machine Pro with Ultra High Performance and Advanced Security Features (UDM-PRO-ULTRA-ADVANCED-2024) at United States of America - Official Ubiquiti Store. Price: $379.00. Check now and buy today!";

        _mockResourceLocalizer
            .Setup(x => x.Notification("Sms.ProductInStock.Subject", product.Name))
            .Returns(longSubject);

        _mockResourceLocalizer
            .Setup(x => x.Notification("Sms.ProductInStock.Body", It.IsAny<object[]>()))
            .Returns(longBody);

        _mockSmsProvider
            .Setup(x => x.SendAsync(recipient, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.SendProductInStockNotificationAsync(
            product, recipient, store);

        // Assert
        result.Should().BeTrue();

        _mockSmsProvider.Verify(
            x => x.SendAsync(
                recipient,
                It.Is<string>(msg =>
                    msg.Length <= 160 &&
                    msg.EndsWith("...")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void ShortenMessageIfNeeded_WithLongMessage_ShouldBreakAtWordBoundary()
    {
        // Arrange
        var service = new SmsNotificationService(
            _mockSmsProvider.Object,
            _mockResourceLocalizer.Object,
            _mockLogger.Object,
            _settings);

        var longMessage = "This is a very long message that definitely exceeds the one hundred and sixty character limit for SMS messages and needs to be shortened to fit properly within the constraints.";

        // Act - Use reflection to call private method
        var shortenMethod = typeof(SmsNotificationService).GetMethod(
            "ShortenMessageIfNeeded",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var result = (string)shortenMethod!.Invoke(service, new object[] { longMessage })!;

        // Assert
        result.Length.Should().BeLessThanOrEqualTo(160);
        result.Should().EndWith("...");
        result.Should().NotEndWith(" ...", "Should trim space before ellipsis");

        // Should break at word boundary (last space in second half)
        var withoutEllipsis = result[..^3];
        withoutEllipsis.Should().NotEndWith(" ", "Should not end with space");
    }
}
