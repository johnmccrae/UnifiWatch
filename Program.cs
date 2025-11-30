using System.CommandLine;
using UnifiStockTracker.Configuration;
using UnifiStockTracker.Services;

namespace UnifiStockTracker;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("UnifiStockTracker - Monitor Ubiquiti product stock availability");

        // Get-Stock command (modern stores)
        var getStockCommand = new Command("get-stock", "Get current stock for US/EU/UK stores");
        var storeOption = new Option<string>("--store", "Store to check (Europe, USA, UK)") { IsRequired = true };
        var collectionsOption = new Option<string[]?>("--collections", "Collections to filter (optional)") { AllowMultipleArgumentsPerToken = true };
        getStockCommand.AddOption(storeOption);
        getStockCommand.AddOption(collectionsOption);
        getStockCommand.SetHandler(async (string store, string[]? collections) =>
        {
            await GetStockAsync(store, collections, isLegacy: false);
        }, storeOption, collectionsOption);

        // Get-Stock-Legacy command (legacy stores)
        var getStockLegacyCommand = new Command("get-stock-legacy", "Get current stock for Brazil/India/Japan/Taiwan/Singapore/Mexico/China stores");
        var legacyStoreOption = new Option<string>("--store", "Store to check (Brazil, India, Japan, Taiwan, Singapore, Mexico, China)") { IsRequired = true };
        var legacyCollectionsOption = new Option<string[]?>("--collections", "Collections to filter (optional)") { AllowMultipleArgumentsPerToken = true };
        getStockLegacyCommand.AddOption(legacyStoreOption);
        getStockLegacyCommand.AddOption(legacyCollectionsOption);
        getStockLegacyCommand.SetHandler(async (string store, string[]? collections) =>
        {
            await GetStockAsync(store, collections, isLegacy: true);
        }, legacyStoreOption, legacyCollectionsOption);

        // Wait-Stock command (modern stores)
        var waitStockCommand = new Command("wait-stock", "Wait for products to be in stock (US/EU/UK stores)");
        var waitStoreOption = new Option<string>("--store", "Store to check (Europe, USA, UK)") { IsRequired = true };
        var productNamesOption = new Option<string[]?>("--product-names", "Product names to monitor") { AllowMultipleArgumentsPerToken = true };
        var productSkusOption = new Option<string[]?>("--product-skus", "Product SKUs to monitor") { AllowMultipleArgumentsPerToken = true };
        var secondsOption = new Option<int>("--seconds", () => 60, "Check interval in seconds");
        var noWebsiteOption = new Option<bool>("--no-website", () => false, "Don't open website when product is in stock");
        var noSoundOption = new Option<bool>("--no-sound", () => false, "Don't play sound when product is in stock");
        
        waitStockCommand.AddOption(waitStoreOption);
        waitStockCommand.AddOption(productNamesOption);
        waitStockCommand.AddOption(productSkusOption);
        waitStockCommand.AddOption(secondsOption);
        waitStockCommand.AddOption(noWebsiteOption);
        waitStockCommand.AddOption(noSoundOption);
        
        waitStockCommand.SetHandler(async (string store, string[]? productNames, string[]? productSkus, int seconds, bool noWebsite, bool noSound) =>
        {
            await WaitForStockAsync(store, productNames, productSkus, seconds, noWebsite, noSound, isLegacy: false);
        }, waitStoreOption, productNamesOption, productSkusOption, secondsOption, noWebsiteOption, noSoundOption);

        // Wait-Stock-Legacy command (legacy stores)
        var waitStockLegacyCommand = new Command("wait-stock-legacy", "Wait for products to be in stock (legacy stores)");
        var waitLegacyStoreOption = new Option<string>("--store", "Store to check (Brazil, India, Japan, Taiwan, Singapore, Mexico, China)") { IsRequired = true };
        var legacyProductNamesOption = new Option<string[]?>("--product-names", "Product names to monitor") { AllowMultipleArgumentsPerToken = true };
        var legacyProductSkusOption = new Option<string[]?>("--product-skus", "Product SKUs to monitor") { AllowMultipleArgumentsPerToken = true };
        var legacySecondsOption = new Option<int>("--seconds", () => 60, "Check interval in seconds");
        var legacyNoWebsiteOption = new Option<bool>("--no-website", () => false, "Don't open website when product is in stock");
        var legacyNoSoundOption = new Option<bool>("--no-sound", () => false, "Don't play sound when product is in stock");
        
        waitStockLegacyCommand.AddOption(waitLegacyStoreOption);
        waitStockLegacyCommand.AddOption(legacyProductNamesOption);
        waitStockLegacyCommand.AddOption(legacyProductSkusOption);
        waitStockLegacyCommand.AddOption(legacySecondsOption);
        waitStockLegacyCommand.AddOption(legacyNoWebsiteOption);
        waitStockLegacyCommand.AddOption(legacyNoSoundOption);
        
        waitStockLegacyCommand.SetHandler(async (string store, string[]? productNames, string[]? productSkus, int seconds, bool noWebsite, bool noSound) =>
        {
            await WaitForStockAsync(store, productNames, productSkus, seconds, noWebsite, noSound, isLegacy: true);
        }, waitLegacyStoreOption, legacyProductNamesOption, legacyProductSkusOption, legacySecondsOption, legacyNoWebsiteOption, legacyNoSoundOption);

        rootCommand.AddCommand(getStockCommand);
        rootCommand.AddCommand(getStockLegacyCommand);
        rootCommand.AddCommand(waitStockCommand);
        rootCommand.AddCommand(waitStockLegacyCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task GetStockAsync(string store, string[]? collections, bool isLegacy)
    {
        using var httpClient = new HttpClient();

        try
        {
            if (isLegacy)
            {
                var service = new UnifiStockLegacyService(httpClient);
                var products = await service.GetStockAsync(store, collections);
                DisplayProducts(products);
            }
            else
            {
                var service = new UnifiStockService(httpClient);
                var products = await service.GetStockAsync(store, collections);
                DisplayProducts(products);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static async Task WaitForStockAsync(string store, string[]? productNames, string[]? productSkus, int seconds, bool noWebsite, bool noSound, bool isLegacy)
    {
        if ((productNames == null || productNames.Length == 0) && (productSkus == null || productSkus.Length == 0))
        {
            Console.WriteLine("Error: You must specify at least one product name or SKU to monitor");
            return;
        }

        using var httpClient = new HttpClient();

        try
        {
            StockWatcher watcher;

            if (isLegacy)
            {
                var service = new UnifiStockLegacyService(httpClient);
                watcher = new StockWatcher(service, store);
            }
            else
            {
                var service = new UnifiStockService(httpClient);
                watcher = new StockWatcher(service, store);
            }

            await watcher.WaitForStockAsync(productNames, productSkus, seconds, noWebsite, noSound);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static void DisplayProducts(List<Models.UnifiProduct> products)
    {
        if (products.Count == 0)
        {
            Console.WriteLine("No products found");
            return;
        }

        Console.WriteLine($"\n{"Name",-50} {"Available",-10} {"Category",-30} {"SKU",-15} {"Price",-10}");
        Console.WriteLine(new string('-', 120));

        foreach (var product in products.OrderBy(p => p.Name))
        {
            var available = product.Available ? "✓ Yes" : "✗ No";
            var price = product.Price.HasValue ? $"${product.Price:F2}" : "N/A";
            
            Console.WriteLine($"{TruncateString(product.Name, 50),-50} {available,-10} {TruncateString(product.Category, 30),-30} {TruncateString(product.SKU, 15),-15} {price,-10}");
        }

        Console.WriteLine($"\nTotal: {products.Count} products");
        Console.WriteLine($"Available: {products.Count(p => p.Available)} products");
    }

    static string TruncateString(string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str))
            return string.Empty;

        return str.Length <= maxLength ? str : str.Substring(0, maxLength - 3) + "...";
    }
}
