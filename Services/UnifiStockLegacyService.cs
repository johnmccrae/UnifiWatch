using System.Globalization;
using System.Text.Json;
using UnifiWatch.Configuration;
using UnifiWatch.Models;

namespace UnifiWatch.Services;

public class unifiwatchLegacyService : IunifiwatchService
{
    private readonly HttpClient _httpClient;

    public unifiwatchLegacyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<UnifiProduct>> GetStockAsync(string store, string[]? collections = null, CancellationToken cancellationToken = default)
    {
        if (!StoreConfiguration.LegacyStores.TryGetValue(store, out var storeUrl))
        {
            throw new ArgumentException($"Store '{store}' is not supported. Valid stores: {string.Join(", ", StoreConfiguration.LegacyStores.Keys)}");
        }

        var collectionsToFetch = collections ?? StoreConfiguration.LegacyCollections.Keys.ToArray();
        var result = new List<UnifiProduct>();

        foreach (var collectionKey in collectionsToFetch)
        {
            if (!StoreConfiguration.LegacyCollections.TryGetValue(collectionKey, out var collectionSlug))
            {
                Console.WriteLine($"Warning: Collection '{collectionKey}' not found, skipping...");
                continue;
            }

            var url = $"{storeUrl}/collections/{collectionSlug}";
            var productsUrl = $"{url}/products.json";

            try
            {
                Console.WriteLine($"Fetching {productsUrl}...");
                var response = await _httpClient.GetAsync(productsUrl, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var productsResponse = JsonSerializer.Deserialize<ShopifyProductsResponse>(responseBody);

                if (productsResponse?.Products == null)
                {
                    Console.WriteLine($"No products found for collection '{collectionKey}'");
                    continue;
                }

                foreach (var product in productsResponse.Products)
                {
                    foreach (var variant in product.Variants)
                    {
                        result.Add(new UnifiProduct
                        {
                            Name = product.Title,
                            Available = variant.Available,
                            Category = collectionKey,
                            Price = ParsePrice(variant.Price),
                            SKU = variant.SKU,
                            SKUName = variant.Title,
                            Created = ParseDateTime(variant.CreatedAt),
                            Updated = ParseDateTime(variant.UpdatedAt),
                            ProductUrl = $"{url}/products/{product.Handle}",
                            Tags = string.IsNullOrWhiteSpace(product.Tags) 
                                ? null 
                                : product.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching {productsUrl}: {ex.Message}");
            }
        }

        return result;
    }

    private static decimal? ParsePrice(string price)
    {
        if (string.IsNullOrWhiteSpace(price))
            return null;

        if (decimal.TryParse(price, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }

    private static DateTime? ParseDateTime(string? dateTime)
    {
        if (string.IsNullOrWhiteSpace(dateTime))
            return null;

        if (DateTime.TryParse(dateTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result))
            return result;

        return null;
    }
}
