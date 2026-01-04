using System.Text.Json.Serialization;

namespace UnifiStockTracker.Models;

// GraphQL Request Models
public class GraphQLRequest
{
    [JsonPropertyName("operationName")]
    public string OperationName { get; set; } = string.Empty;

    [JsonPropertyName("variables")]
    public Dictionary<string, object> Variables { get; set; } = new();

    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;
}

// GraphQL Response Models
public class GraphQLResponse
{
    [JsonPropertyName("data")]
    public GraphQLData? Data { get; set; }
}

public class GraphQLData
{
    [JsonPropertyName("storefrontProducts")]
    public StorefrontProducts? StorefrontProducts { get; set; }
}

public class StorefrontProducts
{
    [JsonPropertyName("pagination")]
    public Pagination? Pagination { get; set; }

    [JsonPropertyName("items")]
    public List<StorefrontProduct> Items { get; set; } = new();
}

public class Pagination
{
    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class StorefrontProduct
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("shortTitle")]
    public string? ShortTitle { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("collectionSlug")]
    public string? CollectionSlug { get; set; }

    [JsonPropertyName("organizationalCollectionSlug")]
    public string? OrganizationalCollectionSlug { get; set; }

    [JsonPropertyName("shortDescription")]
    public string? ShortDescription { get; set; }

    [JsonPropertyName("tags")]
    public List<Tag> Tags { get; set; } = new();

    [JsonPropertyName("variants")]
    public List<Variant> Variants { get; set; } = new();
}

public class Tag
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class Variant
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("sku")]
    public string SKU { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("isEarlyAccess")]
    public bool IsEarlyAccess { get; set; }

    [JsonPropertyName("displayPrice")]
    public DisplayPrice? DisplayPrice { get; set; }
}

public class DisplayPrice
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;
}
