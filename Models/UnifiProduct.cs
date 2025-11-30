namespace UnifiStockTracker.Models;

public class UnifiProduct
{
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public bool Available { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Collection { get; set; }
    public string? OrganizationalCollectionSlug { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string? SKUName { get; set; }
    public bool EarlyAccess { get; set; }
    public string ProductUrl { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Updated { get; set; }
    public string[]? Tags { get; set; }
}
