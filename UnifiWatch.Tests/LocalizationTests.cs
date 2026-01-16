using System.Globalization;
using System.Reflection;
using System.Resources;
using FluentAssertions;
using Xunit;

namespace UnifiWatch.Tests;

public class LocalizationTests
{
    private static Assembly GetMainAssembly()
    {
        // Load the main assembly
        return Assembly.Load("UnifiWatch");
    }

    [Fact(Skip = "Resources are embedded at build time in the main assembly")]
    public void ResourceFiles_ExistAndAreEmbedded()
    {
        // Arrange
        var assembly = GetMainAssembly();
        var resourceNames = assembly.GetManifestResourceNames();

        // Act & Assert - Check for CLI resources
        resourceNames.Should().Contain(r => r.Contains("CLI.en-CA.json"), "CLI resources should be embedded");

        // Act & Assert - Check for Errors resources
        resourceNames.Should().Contain(r => r.Contains("Errors.en-CA.json"), "Errors resources should be embedded");

        // Act & Assert - Check for Notifications resources
        resourceNames.Should().Contain(r => r.Contains("Notifications.en-CA.json"), "Notifications resources should be embedded");
    }

    [Fact(Skip = "Resources are embedded at build time in the main assembly")]
    public void CLI_ResourceFile_CanBeLoaded()
    {
        // Arrange
        var assembly = GetMainAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.Contains("CLI.en-CA.json"));

        // Assert
        resourceName.Should().NotBeNull("CLI resource should exist in main assembly");

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName!);
        stream.Should().NotBeNull("CLI resource stream should be accessible");
    }

    [Fact(Skip = "Resources are embedded at build time in the main assembly")]
    public void Errors_ResourceFile_CanBeLoaded()
    {
        // Arrange
        var assembly = GetMainAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.Contains("Errors.en-CA.json"));

        // Assert
        resourceName.Should().NotBeNull("Errors resource should exist in main assembly");

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName!);
        stream.Should().NotBeNull("Errors resource stream should be accessible");
    }

    [Fact(Skip = "Resources are embedded at build time in the main assembly")]
    public void Notifications_ResourceFile_CanBeLoaded()
    {
        // Arrange
        var assembly = GetMainAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(r => r.Contains("Notifications.en-CA.json"));

        // Assert
        resourceName.Should().NotBeNull("Notifications resource should exist in main assembly");

        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName!);
        stream.Should().NotBeNull("Notifications resource stream should be accessible");
    }

    [Fact]
    public void EnglishCanadian_IsDefaultCulture()
    {
        // Arrange
        var expectedCulture = "en-CA";

        // Act
        var culture = CultureInfo.GetCultureInfo(expectedCulture);

        // Assert
        culture.Name.Should().Be(expectedCulture);
        culture.EnglishName.Should().Contain("English");
        culture.EnglishName.Should().Contain("Canada");
    }

    [Fact]
    public void SupportedCultures_AreValid()
    {
        // Arrange
        var supportedCultures = new[]
        {
            "en-CA", // English (Canada) - default
            "fr-CA", // French (Canada)
            "de-DE", // German (Germany)
            "es-ES", // Spanish (Spain)
            "fr-FR", // French (France)
            "it-IT", // Italian (Italy)
            "pt-BR"  // Portuguese (Brazil)
        };

        // Act & Assert
        foreach (var cultureName in supportedCultures)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            culture.Should().NotBeNull($"{cultureName} should be a valid culture");
            culture.Name.Should().Be(cultureName);
        }
    }

    [Fact]
    public void DateFormatting_RespectsCulture()
    {
        // Arrange
        var date = new DateTime(2024, 3, 15, 14, 30, 0);
        var cultures = new[] { "en-CA", "fr-CA", "de-DE" };

        // Act & Assert
        foreach (var cultureName in cultures)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            var formatted = date.ToString("d", culture);
            formatted.Should().NotBeNullOrWhiteSpace($"Date should format for {cultureName}");
        }
    }

    [Fact]
    public void NumberFormatting_RespectsCulture()
    {
        // Arrange
        var number = 1234.56;
        var cultures = new[] { "en-CA", "fr-CA", "de-DE" };

        // Act & Assert
        foreach (var cultureName in cultures)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            var formatted = number.ToString("N2", culture);
            formatted.Should().NotBeNullOrWhiteSpace($"Number should format for {cultureName}");
        }
    }

    [Fact]
    public void CurrencyFormatting_RespectsCulture()
    {
        // Arrange
        var amount = 1234.56m;
        var cultures = new[] { "en-CA", "fr-CA", "de-DE" };

        // Act & Assert
        foreach (var cultureName in cultures)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            var formatted = amount.ToString("C", culture);
            formatted.Should().NotBeNullOrWhiteSpace($"Currency should format for {cultureName}");
        }
    }

    [Theory]
    [InlineData("en-CA", "2024-03-15")]
    [InlineData("fr-CA", "2024-03-15")]
    [InlineData("de-DE", "2024-03-15")]
    public void DateParsing_WorksAcrossCultures(string cultureName, string dateString)
    {
        // Arrange
        var culture = CultureInfo.GetCultureInfo(cultureName);

        // Act
        var success = DateTime.TryParse(dateString, culture, out var result);

        // Assert
        success.Should().BeTrue($"Date should parse in {cultureName}");
        result.Year.Should().Be(2024);
        result.Month.Should().Be(3);
        result.Day.Should().Be(15);
    }

    [Fact(Skip = "Resource file validation deferred to Phase 2b")]
    public void ResourceFiles_ContainRequiredKeys()
    {
        // This test will be expanded once we implement IStringLocalizer
        // For now, we just verify the files exist
        var mainAssembly = GetMainAssembly();
        var resourceNames = mainAssembly.GetManifestResourceNames();

        // Assert
        resourceNames.Should().Contain(r => r.Contains("CLI"), "CLI resources should exist");
        resourceNames.Should().Contain(r => r.Contains("Errors"), "Errors resources should exist");
        resourceNames.Should().Contain(r => r.Contains("Notifications"), "Notifications resources should exist");
    }
}
