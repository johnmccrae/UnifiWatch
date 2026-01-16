using System.Text.Json.Serialization;

namespace UnifiWatch.Models;

/// <summary>
/// Persisted state for the UnifiWatch service
/// Tracks product availability changes to prevent duplicate notifications
/// Saved to state.json in the configuration directory
/// </summary>
public class ServiceState
{
    /// <summary>
    /// Timestamp of the last successful stock check
    /// </summary>
    [JsonPropertyName("lastCheckTime")]
    public DateTime? LastCheckTime { get; set; }

    /// <summary>
    /// Dictionary of product SKU to its last known availability state
    /// Key: Product SKU (e.g., "UDR-US")
    /// Value: ProductState with availability and timestamp
    /// </summary>
    [JsonPropertyName("productStates")]
    public Dictionary<string, ProductState> ProductStates { get; set; } = new();

    /// <summary>
    /// Total number of checks performed since service start
    /// Used for statistics and debugging
    /// </summary>
    [JsonPropertyName("totalChecks")]
    public long TotalChecks { get; set; }

    /// <summary>
    /// Total number of notifications sent since service start
    /// Used for statistics and debugging
    /// </summary>
    [JsonPropertyName("totalNotificationsSent")]
    public long TotalNotificationsSent { get; set; }

    /// <summary>
    /// Service start time (for uptime tracking)
    /// </summary>
    [JsonPropertyName("serviceStartTime")]
    public DateTime? ServiceStartTime { get; set; }

    /// <summary>
    /// Last notification sent time (for rate limiting and debugging)
    /// </summary>
    [JsonPropertyName("lastNotificationTime")]
    public DateTime? LastNotificationTime { get; set; }
}

/// <summary>
/// Represents the tracked state of a single product
/// </summary>
public class ProductState
{
    /// <summary>
    /// Product SKU (unique identifier)
    /// </summary>
    [JsonPropertyName("sku")]
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// Product name for logging/debugging
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Last known availability status
    /// </summary>
    [JsonPropertyName("available")]
    public bool Available { get; set; }

    /// <summary>
    /// Last known price
    /// </summary>
    [JsonPropertyName("price")]
    public decimal? Price { get; set; }

    /// <summary>
    /// Timestamp when this state was last updated
    /// </summary>
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Number of times we've seen this product become available
    /// </summary>
    [JsonPropertyName("availabilityChanges")]
    public int AvailabilityChanges { get; set; }

    /// <summary>
    /// Timestamp when last notification was sent for this product
    /// Used to prevent duplicate notifications within a short time window
    /// </summary>
    [JsonPropertyName("lastNotificationTime")]
    public DateTime? LastNotificationTime { get; set; }
}
