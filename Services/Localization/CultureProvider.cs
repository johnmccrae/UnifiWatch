using System.Globalization;
using Microsoft.Extensions.Logging;

namespace UnifiWatch.Services.Localization;

/// <summary>
/// Implementation of ICultureProvider
/// </summary>
public class CultureProvider : ICultureProvider
{
    private readonly ILogger<CultureProvider> _logger;
    private readonly string? _cliLanguage;
    private readonly string? _configLanguage;
    private readonly string? _configTimeZone;

    public CultureProvider(
        ILogger<CultureProvider> logger,
        string? cliLanguage = null,
        string? configLanguage = null,
        string? configTimeZone = null)
    {
        _logger = logger;
        _cliLanguage = cliLanguage;
        _configLanguage = configLanguage;
        _configTimeZone = configTimeZone;
    }

    /// <inheritdoc />
    public CultureInfo GetUserCulture()
    {
        // Tier 1: CLI --language flag
        if (!string.IsNullOrWhiteSpace(_cliLanguage) && _cliLanguage != "auto")
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(_cliLanguage);
                _logger.LogDebug("Using culture from CLI flag: {Culture}", culture.Name);
                return culture;
            }
            catch (CultureNotFoundException ex)
            {
                _logger.LogWarning(ex, "Invalid culture from CLI flag: {Language}, falling back", _cliLanguage);
            }
        }

        // Tier 2: ServiceConfiguration.Service.Language
        if (!string.IsNullOrWhiteSpace(_configLanguage) && _configLanguage != "auto")
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(_configLanguage);
                _logger.LogDebug("Using culture from configuration: {Culture}", culture.Name);
                return culture;
            }
            catch (CultureNotFoundException ex)
            {
                _logger.LogWarning(ex, "Invalid culture from configuration: {Language}, falling back", _configLanguage);
            }
        }

        // Tier 3: System CultureInfo.CurrentUICulture
        var systemCulture = CultureInfo.CurrentUICulture;
        if (systemCulture.Name != "en-US") // Don't log for default invariant culture
        {
            _logger.LogDebug("Using system culture: {Culture}", systemCulture.Name);
            return systemCulture;
        }

        // Tier 4: Default fallback to en-CA
        var fallbackCulture = CultureInfo.GetCultureInfo("en-CA");
        _logger.LogDebug("Using fallback culture: {Culture}", fallbackCulture.Name);
        return fallbackCulture;
    }

    /// <inheritdoc />
    public TimeZoneInfo GetTimeZone()
    {
        // Configuration time zone
        if (!string.IsNullOrWhiteSpace(_configTimeZone) && _configTimeZone != "auto")
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_configTimeZone);
                _logger.LogDebug("Using time zone from configuration: {TimeZone}", timeZone.Id);
                return timeZone;
            }
            catch (TimeZoneNotFoundException ex)
            {
                _logger.LogWarning(ex, "Invalid time zone from configuration: {TimeZone}, using system default", _configTimeZone);
            }
        }

        // Fallback to system local time zone
        var systemTimeZone = TimeZoneInfo.Local;
        _logger.LogDebug("Using system time zone: {TimeZone}", systemTimeZone.Id);
        return systemTimeZone;
    }
}
