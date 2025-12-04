using System.CommandLine;
using System.CommandLine.Invocation;
using UnifiStockTracker.Configuration;
using UnifiStockTracker.Services;

namespace UnifiStockTracker;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("UnifiStockTracker - Monitor Ubiquiti product stock availability");

        // Mode options (mutually exclusive)
        var stockOption = new Option<bool>("--stock", "Get current stock");
        var waitOption = new Option<bool>("--wait", "Wait for products to be in stock");
        
        // Store options (mutually exclusive)
        var storeOption = new Option<string?>("--store", "Store to check (Europe, USA, UK) - uses GraphQL API");
        var legacyApiStoreOption = new Option<string?>("--legacy-api-store", "Store to check (Brazil, India, Japan, Taiwan, Singapore, Mexico, China) - uses Shopify API");
        
        // Filter options
        var collectionsOption = new Option<string[]?>("--collections", "Collections to filter (optional)") { AllowMultipleArgumentsPerToken = true };
        var productNamesOption = new Option<string[]?>("--product-names", "Product names to monitor") { AllowMultipleArgumentsPerToken = true };
        var productSkusOption = new Option<string[]?>("--product-skus", "Product SKUs to monitor") { AllowMultipleArgumentsPerToken = true };
        
        // Wait options
        var secondsOption = new Option<int>("--seconds", () => 60, "Check interval in seconds");
        var noWebsiteOption = new Option<bool>("--no-website", () => false, "Don't open website when product is in stock");
        var noSoundOption = new Option<bool>("--no-sound", () => false, "Don't play sound when product is in stock");

        rootCommand.AddOption(stockOption);
        rootCommand.AddOption(waitOption);
        rootCommand.AddOption(storeOption);
        rootCommand.AddOption(legacyApiStoreOption);
        rootCommand.AddOption(collectionsOption);
        rootCommand.AddOption(productNamesOption);
        rootCommand.AddOption(productSkusOption);
        rootCommand.AddOption(secondsOption);
        rootCommand.AddOption(noWebsiteOption);
        rootCommand.AddOption(noSoundOption);

        rootCommand.SetHandler(async (InvocationContext context) =>
        {
            var stock = context.ParseResult.GetValueForOption(stockOption);
            var wait = context.ParseResult.GetValueForOption(waitOption);
            var store = context.ParseResult.GetValueForOption(storeOption);
            var legacyStore = context.ParseResult.GetValueForOption(legacyApiStoreOption);
            var collections = context.ParseResult.GetValueForOption(collectionsOption);
            var productNames = context.ParseResult.GetValueForOption(productNamesOption);
            var productSkus = context.ParseResult.GetValueForOption(productSkusOption);
            var seconds = context.ParseResult.GetValueForOption(secondsOption);
            var noWebsite = context.ParseResult.GetValueForOption(noWebsiteOption);
            var noSound = context.ParseResult.GetValueForOption(noSoundOption);

            // Validate mutually exclusive mode options
            if (!stock && !wait)
            {
                Console.WriteLine("Error: You must specify either --stock or --wait");
                context.ExitCode = 1;
                return;
            }
            if (stock && wait)
            {
                Console.WriteLine("Error: Cannot specify both --stock and --wait");
                context.ExitCode = 1;
                return;
            }

            // Validate mutually exclusive store options
            if (store == null && legacyStore == null)
            {
                Console.WriteLine("Error: You must specify either --store or --legacy-api-store");
                context.ExitCode = 1;
                return;
            }
            if (store != null && legacyStore != null)
            {
                Console.WriteLine("Error: Cannot specify both --store and --legacy-api-store");
                context.ExitCode = 1;
                return;
            }

            var selectedStore = store ?? legacyStore!;
            var isLegacy = legacyStore != null;

            try
            {
                if (stock)
                {
                    await GetStockAsync(selectedStore, collections, isLegacy);
                }
                else // wait
                {
                    await WaitForStockAsync(selectedStore, productNames, productSkus, seconds, noWebsite, noSound, isLegacy);
                }
                context.ExitCode = 0;
            }
            catch
            {
                context.ExitCode = 1;
            }
        });

        return await rootCommand.InvokeAsync(args);
    }

    static async Task GetStockAsync(string store, string[]? collections, bool isLegacy)
    {
        using var httpClient = new HttpClient();

        try
        {
            IUnifiStockService service = isLegacy
                ? new UnifiStockLegacyService(httpClient)
                : new UnifiStockService(httpClient);

            var products = await service.GetStockAsync(store, collections);
            DisplayProducts(products);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

    static async Task WaitForStockAsync(string store, string[]? productNames, string[]? productSkus, 
        int seconds, bool noWebsite, bool noSound, bool isLegacy)
    {
        using var httpClient = new HttpClient();

        try
        {
            IUnifiStockService stockService = isLegacy
                ? new UnifiStockLegacyService(httpClient)
                : new UnifiStockService(httpClient);
            var watcher = new StockWatcher(stockService, store);
            await watcher.WaitForStockAsync(productNames, productSkus, seconds, noWebsite, noSound);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

    static void DisplayProducts(List<UnifiStockTracker.Models.UnifiProduct> products)
    {
        Console.WriteLine($"\nFound {products.Count} products:\n");
        Console.WriteLine("{0,-50} {1,-12} {2,-30} {3,-20} {4,10}", 
            "Name", "Available", "Category", "SKU", "Price");
        Console.WriteLine(new string('-', 125));

        foreach (var product in products)
        {
            var availability = product.Available ? "✓ In Stock" : "✗ Out of Stock";
            var price = product.Price.HasValue ? $"{(product.Price.Value / 100):F2}" : "N/A";
            
            Console.WriteLine("{0,-50} {1,-12} {2,-30} {3,-20} {4,10}",
                product.Name.Length > 47 ? product.Name.Substring(0, 47) + "..." : product.Name,
                availability,
                product.Category?.Length > 27 ? product.Category.Substring(0, 27) + "..." : product.Category ?? "N/A",
                product.SKU ?? "N/A",
                price);
        }
    }
}
