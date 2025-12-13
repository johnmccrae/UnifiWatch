using System.Globalization;
using UnifiWatch.Configuration;

namespace UnifiWatch.Services.Localization;

public class CultureProvider
{
    private readonly IConfigurationProvider _configProvider;

    public CultureProvider(IConfigurationProvider configProvider)
    {
        _configProvider = configProvider;
    }

    public async Task<CultureInfo> GetUserCultureAsync(CancellationToken ct)
    {
        // 1. Config file language
        try
        {
            var config = await _configProvider.LoadAsync(ct);
            var lang = config?.Service?.Language?.Trim();
            if (!string.IsNullOrEmpty(lang) && !string.Equals(lang, "auto", StringComparison.OrdinalIgnoreCase))
            {
                try { return CultureInfo.GetCultureInfo(lang); } catch { /* ignore invalid */ }
            }
        }
        catch
        {
            // ignore config load failures and continue
        }

        // 2. System default
        var system = CultureInfo.CurrentUICulture;
        if (system != null) return system;

        // 3. Fallback
        return CultureInfo.GetCultureInfo("en-CA");
    }
}
