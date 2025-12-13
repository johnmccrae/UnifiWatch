using System.CommandLine;
using System.CommandLine.Invocation;
using UnifiWatch.Configuration;
using UnifiWatch.Services;
using Microsoft.Extensions.Logging;
using UnifiWatch.Services.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace UnifiWatch;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("UnifiWatch - Monitor stock availability for Ubiquiti products");

        // Initialize culture from configuration (will apply descriptions after options are created)
        System.Globalization.CultureInfo? initializedCulture = null;
        ResourceLocalizer? localizer = null;
        IServiceProvider? serviceProvider = null;
        try
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var services = new ServiceCollection();
            services.AddSingleton(loggerFactory);
            var configProvider = new ConfigurationProvider(loggerFactory.CreateLogger<ConfigurationProvider>());
            services.AddSingleton<IConfigurationProvider>(configProvider);
            var cultureProvider = new UnifiWatch.Services.Localization.CultureProvider(configProvider);
            var culture = await cultureProvider.GetUserCultureAsync(CancellationToken.None);
            System.Globalization.CultureInfo.CurrentCulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = culture;
            initializedCulture = culture;
            localizer = ResourceLocalizer.Load(culture);
            services.AddSingleton(localizer);
            serviceProvider = services.BuildServiceProvider();
            // Cache localizer instance for reuse (back-compat)
            ResourceLocalizerHolder.Instance = localizer;
        }
        catch
        {
            // Ignore culture init failures; fallback remains default
        }

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

        // Apply localized descriptions if available
        if (localizer != null)
        {
            stockOption.Description = localizer.CLI("StockOption.Description");
            waitOption.Description = localizer.CLI("WaitOption.Description");
            storeOption.Description = localizer.CLI("StoreOption.Description");
            legacyApiStoreOption.Description = localizer.CLI("LegacyApiStoreOption.Description");
            collectionsOption.Description = localizer.CLI("CollectionsOption.Description");
            productNamesOption.Description = localizer.CLI("ProductNamesOption.Description");
            productSkusOption.Description = localizer.CLI("ProductSkusOption.Description");
            secondsOption.Description = localizer.CLI("SecondsOption.Description");
            noWebsiteOption.Description = localizer.CLI("NoWebsiteOption.Description");
            noSoundOption.Description = localizer.CLI("NoSoundOption.Description");
        }

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
                Console.WriteLine((localizer ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture))
                    .Error("Error.MustSpecifyStockOrWait"));
                context.ExitCode = 1;
                return;
            }
            if (stock && wait)
            {
                Console.WriteLine((localizer ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture))
                    .Error("Error.CannotSpecifyBothStockAndWait"));
                context.ExitCode = 1;
                return;
            }

            // Validate mutually exclusive store options
            if (store == null && legacyStore == null)
            {
                Console.WriteLine((localizer ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture))
                    .Error("Error.MustSpecifyStoreOrLegacy"));
                context.ExitCode = 1;
                return;
            }
            if (store != null && legacyStore != null)
            {
                Console.WriteLine((localizer ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture))
                    .Error("Error.CannotSpecifyBothStoreAndLegacy"));
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
            IunifiwatchService service = isLegacy
                ? new unifiwatchLegacyService(httpClient)
                : new unifiwatchService(httpClient);

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
            IunifiwatchService stockService = isLegacy
                ? new unifiwatchLegacyService(httpClient)
                : new unifiwatchService(httpClient);
            var watcher = new StockWatcher(stockService, store);
            await watcher.WaitForStockAsync(productNames, productSkus, seconds, noWebsite, noSound);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

    static void DisplayProducts(List<UnifiWatch.Models.UnifiProduct> products)
    {
        var loc = ResourceLocalizerHolder.Instance ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
        Console.WriteLine("\n" + loc.CLI("List.FoundProducts", products.Count) + "\n");
        Console.WriteLine("{0,-50} {1,-12} {2,-30} {3,-20} {4,10}", 
            loc.CLI("List.Headers.Name"), loc.CLI("List.Headers.Available"), loc.CLI("List.Headers.Category"), loc.CLI("List.Headers.SKU"), loc.CLI("List.Headers.Price"));
        Console.WriteLine(new string('-', 125));

        foreach (var product in products)
        {
            var availability = product.Available ? loc.CLI("List.InStock") : loc.CLI("List.OutOfStock");
            var price = product.Price.HasValue ? (product.Price.Value / 100).ToString("F2", System.Globalization.CultureInfo.CurrentCulture) : loc.CLI("List.PriceNA");
            
            Console.WriteLine("{0,-50} {1,-12} {2,-30} {3,-20} {4,10}",
                product.Name.Length > 47 ? product.Name.Substring(0, 47) + "..." : product.Name,
                availability,
                product.Category?.Length > 27 ? product.Category.Substring(0, 27) + "..." : product.Category ?? loc.CLI("List.CategoryNA"),
                product.SKU ?? loc.CLI("List.SKUNA"),
                price);
        }
    }
}
