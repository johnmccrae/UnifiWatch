using System.Globalization;
using System.Text.Json;

namespace UnifiWatch.Services.Localization;

public class ResourceLocalizer : IResourceLocalizer
{
    private readonly Dictionary<string, string> _cli = new();
    private readonly Dictionary<string, string> _notifications = new();
    private readonly Dictionary<string, string> _errors = new();

    public static ResourceLocalizer Load(CultureInfo culture)
    {
        var loc = new ResourceLocalizer();
        var basePath = Path.Combine(AppContext.BaseDirectory, "Resources");

        string[] candidates = new[]
        {
            culture.Name,
            culture.TwoLetterISOLanguageName,
            "en-CA"
        };

        foreach (var c in candidates)
        {
            LoadJson(Path.Combine(basePath, $"CLI.{c}.json"), loc._cli);
            LoadJson(Path.Combine(basePath, $"Notifications.{c}.json"), loc._notifications);
            LoadJson(Path.Combine(basePath, $"Errors.{c}.json"), loc._errors);
        }
        return loc;
    }

    private static void LoadJson(string path, Dictionary<string,string> target)
    {
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path);
            var dict = JsonSerializer.Deserialize<Dictionary<string,string>>(json);
            if (dict == null) return;
            foreach (var kv in dict)
            {
                if (!target.ContainsKey(kv.Key))
                    target[kv.Key] = kv.Value;
            }
        }
        catch (JsonException)
        {
            // Ignore malformed JSON files to avoid hard failures
            return;
        }
    }

    public string CLI(string key, params object[] args) => Format(_cli, key, args);
    public string Notification(string key, params object[] args) => Format(_notifications, key, args);
    public string Error(string key, params object[] args) => Format(_errors, key, args);

    private static string Format(Dictionary<string,string> dict, string key, object[] args)
    {
        if (!dict.TryGetValue(key, out var value))
            return key;
        return args == null || args.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, args);
    }
}
