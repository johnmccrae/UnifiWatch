using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UnifiWatch.Services.Localization;
using Xunit;

namespace UnifiWatch.Tests;

public class CultureProviderTests
{
    private readonly Mock<ILogger<CultureProvider>> _mockLogger;

    public CultureProviderTests()
    {
        _mockLogger = new Mock<ILogger<CultureProvider>>();
    }

    [Fact]
    public void GetUserCulture_WithCliLanguage_ReturnsCliCulture()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, cliLanguage: "fr-CA");

        // Act
        var culture = provider.GetUserCulture();

        // Assert
        culture.Name.Should().Be("fr-CA");
    }

    [Fact]
    public void GetUserCulture_WithInvalidCliLanguage_FallsBackToNextTier()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, cliLanguage: "invalid-culture", configLanguage: "de-DE");

        // Act
        var culture = provider.GetUserCulture();

        // Assert
        culture.Name.Should().Be("de-DE");
    }

    [Fact]
    public void GetUserCulture_WithAutoCliLanguage_FallsBackToConfig()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, cliLanguage: "auto", configLanguage: "es-ES");

        // Act
        var culture = provider.GetUserCulture();

        // Assert
        culture.Name.Should().Be("es-ES");
    }

    [Fact]
    public void GetUserCulture_WithConfigLanguage_ReturnsConfigCulture()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, configLanguage: "it-IT");

        // Act
        var culture = provider.GetUserCulture();

        // Assert
        culture.Name.Should().Be("it-IT");
    }

    [Fact]
    public void GetUserCulture_WithInvalidConfigLanguage_FallsBackToSystem()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, configLanguage: "invalid-culture");

        // Act
        var culture = provider.GetUserCulture();

        // Assert
        // Should return system culture or en-CA fallback
        culture.Should().NotBeNull();
        culture.Name.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetUserCulture_WithAutoConfigLanguage_FallsBackToSystem()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, configLanguage: "auto");

        // Act
        var culture = provider.GetUserCulture();

        // Assert
        culture.Should().NotBeNull();
    }

    [Fact]
    public void GetUserCulture_WithNoSettings_ReturnsSystemOrFallback()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object);

        // Act
        var culture = provider.GetUserCulture();

        // Assert
        culture.Should().NotBeNull();
        culture.Name.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetUserCulture_FallbackOrder_CliTakesPrecedence()
    {
        // Arrange
        var provider = new CultureProvider(
            _mockLogger.Object,
            cliLanguage: "fr-FR",
            configLanguage: "de-DE");

        // Act
        var culture = provider.GetUserCulture();

        // Assert
        culture.Name.Should().Be("fr-FR", "CLI language should take precedence over config");
    }

    [Fact]
    public void GetTimeZone_WithConfigTimeZone_ReturnsConfiguredTimeZone()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, configTimeZone: "Pacific Standard Time");

        // Act
        var timeZone = provider.GetTimeZone();

        // Assert
        timeZone.Id.Should().Be("Pacific Standard Time");
    }

    [Fact]
    public void GetTimeZone_WithInvalidConfigTimeZone_FallsBackToSystem()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, configTimeZone: "Invalid/TimeZone");

        // Act
        var timeZone = provider.GetTimeZone();

        // Assert
        timeZone.Should().Be(TimeZoneInfo.Local);
    }

    [Fact]
    public void GetTimeZone_WithAutoConfigTimeZone_ReturnsSystemTimeZone()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, configTimeZone: "auto");

        // Act
        var timeZone = provider.GetTimeZone();

        // Assert
        timeZone.Should().Be(TimeZoneInfo.Local);
    }

    [Fact]
    public void GetTimeZone_WithNoConfig_ReturnsSystemTimeZone()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object);

        // Act
        var timeZone = provider.GetTimeZone();

        // Assert
        timeZone.Should().Be(TimeZoneInfo.Local);
    }

    [Fact]
    public void GetUserCulture_WithNullCliLanguage_FallsBackCorrectly()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, cliLanguage: null, configLanguage: "fr-CA");

        // Act
        var culture = provider.GetUserCulture();

        // Assert
        culture.Name.Should().Be("fr-CA");
    }

    [Fact]
    public void GetUserCulture_WithEmptyCliLanguage_FallsBackCorrectly()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, cliLanguage: "", configLanguage: "de-DE");

        // Act
        var culture = provider.GetUserCulture();

        // Assert
        culture.Name.Should().Be("de-DE");
    }

    [Fact]
    public void GetUserCulture_WithWhitespaceCliLanguage_FallsBackCorrectly()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, cliLanguage: "   ", configLanguage: "es-ES");

        // Act
        var culture = provider.GetUserCulture();

        // Assert
        culture.Name.Should().Be("es-ES");
    }

    [Fact]
    public void GetTimeZone_WithNullConfigTimeZone_ReturnsSystemTimeZone()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, configTimeZone: null);

        // Act
        var timeZone = provider.GetTimeZone();

        // Assert
        timeZone.Should().Be(TimeZoneInfo.Local);
    }

    [Fact]
    public void GetTimeZone_WithEmptyConfigTimeZone_ReturnsSystemTimeZone()
    {
        // Arrange
        var provider = new CultureProvider(_mockLogger.Object, configTimeZone: "");

        // Act
        var timeZone = provider.GetTimeZone();

        // Assert
        timeZone.Should().Be(TimeZoneInfo.Local);
    }

    [Fact]
    public void GetUserCulture_SupportedCultures_AreValid()
    {
        // Arrange
        var supportedCultures = new[] { "en-CA", "fr-CA", "de-DE", "es-ES", "fr-FR", "it-IT", "pt-BR" };

        foreach (var cultureName in supportedCultures)
        {
            var provider = new CultureProvider(_mockLogger.Object, cliLanguage: cultureName);

            // Act
            var culture = provider.GetUserCulture();

            // Assert
            culture.Name.Should().Be(cultureName);
        }
    }

    [Fact]
    public void GetTimeZone_CommonTimeZones_AreValid()
    {
        // Arrange
        var commonTimeZones = new[]
        {
            "Eastern Standard Time",
            "Central Standard Time",
            "Mountain Standard Time",
            "Pacific Standard Time",
            "UTC"
        };

        foreach (var timeZoneName in commonTimeZones)
        {
            var provider = new CultureProvider(_mockLogger.Object, configTimeZone: timeZoneName);

            // Act
            var timeZone = provider.GetTimeZone();

            // Assert
            timeZone.Id.Should().Be(timeZoneName);
        }
    }
}
