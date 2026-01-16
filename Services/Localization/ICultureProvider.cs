using System.Globalization;

namespace UnifiWatch.Services.Localization;

/// <summary>
/// Provides culture/locale information for the application with a 4-tier fallback strategy:
/// 1. CLI --language flag (if provided)
/// 2. ServiceConfiguration.Service.Language (if not "auto")
/// 3. System CultureInfo.CurrentUICulture
/// 4. Default fallback to "en-CA"
/// </summary>
public interface ICultureProvider
{
    /// <summary>
    /// Gets the current user culture based on the 4-tier fallback strategy
    /// </summary>
    CultureInfo GetUserCulture();

    /// <summary>
    /// Gets the current time zone based on configuration or system default
    /// </summary>
    TimeZoneInfo GetTimeZone();
}
