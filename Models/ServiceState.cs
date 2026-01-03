using System.Text.Json;
using System.Text.Json.Serialization;
using UnifiWatch.Models;

namespace UnifiWatch.Models;

/// <summary>
/// Minimal persisted state for service deduplication and change detection.
/// </summary>
public class ServiceState
{
    [JsonPropertyName("lastCheckUtc")] public DateTimeOffset LastCheckUtc { get; set; }
    [JsonPropertyName("availabilityBySku")] public Dictionary<string, bool> AvailabilityBySku { get; set; } = new();

    public static ServiceState FromProducts(IEnumerable<UnifiProduct> products)
    {
        var state = new ServiceState { LastCheckUtc = DateTimeOffset.UtcNow };
        foreach (var p in products)
        {
            var sku = p.SKU ?? p.Name ?? string.Empty;
            if (!string.IsNullOrEmpty(sku))
            {
                state.AvailabilityBySku[sku] = p.Available;
            }
        }
        return state;
    }

    public static async Task SaveAsync(string path, ServiceState state, CancellationToken cancellationToken = default)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(state, options);
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    public static async Task<ServiceState?> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            return null;
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<ServiceState>(json);
    }
}
