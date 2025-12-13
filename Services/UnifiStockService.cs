using System.Text;
using System.Text.Json;
using UnifiWatch.Configuration;
using UnifiWatch.Models;
using UnifiWatch.Services.Localization;

namespace UnifiWatch.Services;

public class unifiwatchService : IunifiwatchService
{
    private readonly HttpClient _httpClient;
    private const string GraphQLEndpoint = "https://ecomm.svc.ui.com/graphql";
    private const int PageLimit = 250;

    public unifiwatchService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<UnifiProduct>> GetStockAsync(string store, string[]? collections = null, CancellationToken cancellationToken = default)
    {
        if (!StoreConfiguration.ModernStores.TryGetValue(store, out var storeId))
        {
            throw new ArgumentException($"Store '{store}' is not supported. Valid stores: {string.Join(", ", StoreConfiguration.ModernStores.Keys)}");
        }

        if (!StoreConfiguration.ModernStoreLinks.TryGetValue(store, out var storeLink))
        {
            throw new ArgumentException($"Store link not found for '{store}'");
        }

        var loc = ServiceProviderHolder.GetService<ResourceLocalizer>()
                  ?? ResourceLocalizerHolder.Instance
                  ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
        Console.WriteLine(loc.CLI("Store.GettingProducts", store));

        var allProducts = new List<StorefrontProduct>();
        var offset = 0;
        var total = 1;

        while (offset < total)
        {
            var graphQLRequest = CreateGraphQLRequest(storeId, offset, PageLimit);
            var content = new StringContent(
                JsonSerializer.Serialize(graphQLRequest),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(GraphQLEndpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var graphQLResponse = JsonSerializer.Deserialize<GraphQLResponse>(responseBody);

            if (graphQLResponse?.Data?.StorefrontProducts == null)
            {
                throw new InvalidOperationException("Failed to retrieve products from the store");
            }

            allProducts.AddRange(graphQLResponse.Data.StorefrontProducts.Items);
            
            var pagination = graphQLResponse.Data.StorefrontProducts.Pagination;
            if (pagination != null)
            {
                total = pagination.Total;
                offset += PageLimit;
            }
            else
            {
                break;
            }
        }

        Console.WriteLine(loc.CLI("Store.RetrievedProducts", allProducts.Count));

        return ConvertToUnifiProducts(allProducts, storeLink, collections);
    }

    private static GraphQLRequest CreateGraphQLRequest(string storeId, int offset, int limit)
    {
        return new GraphQLRequest
        {
            OperationName = "GetProductsForLandingPagePro",
            Variables = new Dictionary<string, object>
            {
                {
                    "input", new Dictionary<string, object>
                    {
                        { "limit", limit },
                        { "offset", offset },
                        {
                            "filter", new Dictionary<string, object>
                            {
                                { "storeId", storeId },
                                { "language", "en" },
                                { "line", "Unifi" }
                            }
                        }
                    }
                }
            },
            Query = @"
query GetProductsForLandingPagePro($input: StorefrontProductListInput!) {
  storefrontProducts(input: $input) {
    pagination {
      limit
      offset
      total
      __typename
    }
    items {
      ...LandingProProductFragment
      __typename
    }
    __typename
  }
}

fragment LandingProProductFragment on StorefrontProduct {
  id
  title
  shortTitle
  name
  slug
  collectionSlug
  organizationalCollectionSlug
  shortDescription
  tags {
    name
    __typename
  }
  gallery {
    ...ImageOnlyGalleryFragment
    __typename
  }
  options {
    id
    title
    values {
      id
      title
      __typename
    }
    __typename
  }
  variants {
    id
    sku
    status
    title
    galleryItemIds
    isEarlyAccess
    optionValueIds
    displayPrice {
      ...MoneyFragment
      __typename
    }
    hasPurchaseHistory
    __typename
  }
  __typename
}

fragment ImageOnlyGalleryFragment on Gallery {
  id
  items {
    id
    data {
      __typename
      ... on Asset {
        id
        mimeType
        url
        height
        width
        __typename
      }
    }
    __typename
  }
  type
  __typename
}

fragment MoneyFragment on Money {
  amount
  currency
  __typename
}
"
        };
    }

    private static List<UnifiProduct> ConvertToUnifiProducts(
        List<StorefrontProduct> products,
        string storeLink,
        string[]? collections)
    {
        var result = new List<UnifiProduct>();

        foreach (var product in products)
        {
            foreach (var variant in product.Variants)
            {
                var category = GetCategory(product);

                if (collections != null && collections.Length > 0 && !collections.Contains(category))
                {
                    continue;
                }

                result.Add(new UnifiProduct
                {
                    Name = product.Title,
                    ShortName = product.ShortTitle,
                    Available = variant.Status == "AVAILABLE",
                    Category = category,
                    Collection = product.CollectionSlug,
                    OrganizationalCollectionSlug = product.OrganizationalCollectionSlug,
                    SKU = variant.SKU,
                    SKUName = variant.Title,
                    EarlyAccess = variant.IsEarlyAccess,
                    ProductUrl = $"{storeLink}/collections/{product.CollectionSlug}/products/{product.Slug}",
                    Price = variant.DisplayPrice?.Amount,
                    Tags = product.Tags.Select(t => t.Name).ToArray()
                });
            }
        }

        return result;
    }

    private static string GetCategory(StorefrontProduct product)
    {
        if (!string.IsNullOrEmpty(product.CollectionSlug) &&
            StoreConfiguration.CollectionToCategory.TryGetValue(product.CollectionSlug, out var category))
        {
            return category;
        }

        if (!string.IsNullOrEmpty(product.OrganizationalCollectionSlug) &&
            StoreConfiguration.CollectionToCategory.TryGetValue(product.OrganizationalCollectionSlug, out category))
        {
            return category;
        }

        return "Unknown";
    }
}
