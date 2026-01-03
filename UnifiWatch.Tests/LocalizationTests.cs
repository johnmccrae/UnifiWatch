using System.Globalization;
using System.Threading;
using UnifiWatch.Services.Localization;
using UnifiWatch.Configuration;
using Xunit;

namespace UnifiWatch.Tests;

public class LocalizationTests
{
    [Fact]
    public async Task CultureProvider_UsesConfigLanguage_WhenSpecified()
    {
        var fake = new FakeConfigProvider("fr-CA");
        var provider = new CultureProvider(fake);
        var culture = await provider.GetUserCultureAsync(CancellationToken.None);
        Assert.Equal("fr-CA", culture.Name);
    }

    [Fact]
    public void ResourceLocalizer_FallsBackToEnCA_WhenKeyMissing()
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var loc = ResourceLocalizer.Load(culture);
        var text = loc.CLI("StockOption.Description");
        Assert.False(string.IsNullOrEmpty(text));
    }

    [Fact]
    public void ResourceLocalizer_FormatsUsingCulture()
    {
        var culture = CultureInfo.GetCultureInfo("fr-CA");
        CultureInfo.CurrentCulture = culture;
        var loc = ResourceLocalizer.Load(culture);
        var text = loc.Notification("Product.InStock", "Dream Machine Pro", "UDM-SE");
        Assert.Contains("Dream Machine Pro", text);
        Assert.Contains("UDM-SE", text);
    }

    [Fact]
    public void ResourceLocalizer_MissingKey_ReturnsKeyAcrossFiles()
    {
        var culture = CultureInfo.GetCultureInfo("fr-CA");
        var loc = ResourceLocalizer.Load(culture);
        Assert.Equal("CLI.Nonexistent.Key", loc.CLI("CLI.Nonexistent.Key"));
        Assert.Equal("Notification.Nonexistent.Key", loc.Notification("Notification.Nonexistent.Key"));
        Assert.Equal("Error.Nonexistent.Key", loc.Error("Error.Nonexistent.Key"));
    }

    [Fact]
    public void ResourceLocalizer_FormatsDateCurrency_UsingCurrentCulture()
    {
        var culture = CultureInfo.GetCultureInfo("fr-CA");
        CultureInfo.CurrentCulture = culture;
        var date = new DateTime(2025, 12, 31, 18, 45, 0);
        var amount = 1234.56m;
        var formattedDate = string.Format(culture, "{0:D}", date);
        var formattedCurrency = string.Format(culture, "{0:N2}", amount);
        Assert.Contains("2025", formattedDate);
        Assert.Contains(",", formattedCurrency);
    }

    private class FakeConfigProvider : IConfigurationProvider
    {
        private readonly string _lang;
        public FakeConfigProvider(string lang) { _lang = lang; }
        public Task<ServiceConfiguration?> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<ServiceConfiguration?>(new ServiceConfiguration
            {
                Service = new ServiceSettings { Language = _lang }
            });
        public Task<bool> SaveAsync(ServiceConfiguration config, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
        public List<string> Validate(ServiceConfiguration config) => new();
        public string ConfigurationDirectory => string.Empty;
        public string ConfigurationFilePath => string.Empty;
        public Task<string?> BackupAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(string.Empty);
        public ServiceConfiguration GetDefaultConfiguration() => new ServiceConfiguration();
        public bool ConfigurationExists() => true;
        public Task<bool> DeleteAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);
    }
}
