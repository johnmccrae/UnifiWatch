using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using UnifiWatch.CLI;
using UnifiWatch.Configuration;
using UnifiWatch.Services;
using UnifiWatch.Services.Credentials;
using UnifiWatch.Services.Notifications;
using UnifiWatch.Services.Notifications.Sms;

namespace UnifiWatch;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Force UTF-8 encoding for console output to ensure proper Unicode rendering
        Console.OutputEncoding = Encoding.UTF8;

        // Check for service mode first (--service-mode)
        if (args.Contains("--service-mode", StringComparer.OrdinalIgnoreCase))
        {
            return await RunServiceModeAsync(args.Where(a => !a.Equals("--service-mode", StringComparison.OrdinalIgnoreCase)).ToArray());
        }

        // Check for configuration commands
        if (args.Length > 0)
        {
            var command = args[0].ToLowerInvariant();
            if (command is "configure" or "show-config" or "reset-config" or "test-notifications" or "validate-config" or "health-check")
            {
                return await HandleConfigurationCommandAsync(args);
            }
        }

        var rootCommand = new RootCommand("UnifiWatch - Monitor Ubiquiti product stock availability");

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
                AnsiConsole.MarkupLine("[red]✗ Error: You must specify either --stock or --wait[/]");
                context.ExitCode = 1;
                return;
            }
            if (stock && wait)
            {
                AnsiConsole.MarkupLine("[red]✗ Error: Cannot specify both --stock and --wait[/]");
                context.ExitCode = 1;
                return;
            }

            // Validate mutually exclusive store options
            if (store == null && legacyStore == null)
            {
                AnsiConsole.MarkupLine("[red]✗ Error: You must specify either --store or --legacy-api-store[/]");
                context.ExitCode = 1;
                return;
            }
            if (store != null && legacyStore != null)
            {
                AnsiConsole.MarkupLine("[red]✗ Error: Cannot specify both --store and --legacy-api-store[/]");
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

    static async Task<int> HandleConfigurationCommandAsync(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });

        var logger = loggerFactory.CreateLogger<Program>();
        var configProvider = new ConfigurationProvider(loggerFactory.CreateLogger<ConfigurationProvider>());
        var credentialProvider = CredentialProviderFactory.CreateProvider("auto", loggerFactory);

        var rootCommand = new RootCommand("UnifiWatch Configuration");

        // Add configuration commands
        rootCommand.AddCommand(ConfigurationCommands.CreateConfigureCommand(
            configProvider, credentialProvider, logger));
        rootCommand.AddCommand(ConfigurationCommands.CreateShowConfigCommand(
            configProvider, logger));
        rootCommand.AddCommand(ConfigurationCommands.CreateResetConfigCommand(
            configProvider, credentialProvider, logger));
        rootCommand.AddCommand(ConfigurationCommands.CreateTestNotificationsCommand(
            configProvider, logger));
        rootCommand.AddCommand(ConfigurationCommands.CreateValidateConfigCommand(
            configProvider, credentialProvider, logger));
        rootCommand.AddCommand(ConfigurationCommands.CreateHealthCheckCommand(
            configProvider, logger));

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
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
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

    static void DisplayProducts(List<UnifiWatch.Models.UnifiProduct> products)
    {
        Console.WriteLine($"\n[Found {products.Count} products]\n");
        Console.WriteLine("{0,-50} {1,-15} {2,-30} {3,-20} {4,10}", 
            "Name", "Available", "Category", "SKU", "Price");
        Console.WriteLine(new string('-', 128));

        foreach (var product in products)
        {
            var availability = product.Available ? "[In Stock]" : "[Out of Stock]";
            var price = product.Price.HasValue ? $"{(product.Price.Value / 100):F2}" : "N/A";
            var name = product.Name.Length > 47 ? product.Name.Substring(0, 47) + "..." : product.Name;
            var category = product.Category?.Length > 27 ? product.Category.Substring(0, 27) + "..." : product.Category ?? "N/A";
            var sku = (product.SKU ?? "N/A").Length > 20 ? (product.SKU ?? "N/A").Substring(0, 17) + "..." : product.SKU ?? "N/A";
            
            Console.WriteLine("{0,-50} {1,-15} {2,-30} {3,-20} {4,10}",
                name,
                availability,
                category,
                sku,
                price);
        }
    }

    /// <summary>
    /// Runs UnifiWatch as a background service
    /// Registers dependencies and configures hosting for Windows Service, systemd, or standalone
    /// </summary>
    static async Task<int> RunServiceModeAsync(string[] args)
    {
        try
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Register core services
                    services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
                    
                    // Register credential provider
                    services.AddSingleton<ICredentialProvider>(sp =>
                        CredentialProviderFactory.CreateProvider("auto", 
                            sp.GetRequiredService<ILoggerFactory>()));

                    // Register HTTP clients for stock services
                    services.AddHttpClient<IUnifiStockService, UnifiStockService>();
                    services.AddHttpClient<UnifiStockLegacyService>();

                    // Register notification providers (optional)
                    services.AddScoped<SmtpEmailProvider>();
                    services.AddScoped<MicrosoftGraphEmailProvider>();
                    services.AddScoped<ISmsProvider, TwilioSmsProvider>();

                    // Register notification orchestrator
                    services.AddSingleton<NotificationOrchestrator>(sp =>
                    {
                        var config = sp.GetRequiredService<IConfigurationProvider>().LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                        
                        return new NotificationOrchestrator(
                            config,
                            config?.Notifications?.Email?.Provider?.Equals("smtp", StringComparison.OrdinalIgnoreCase) == true 
                                ? sp.GetRequiredService<SmtpEmailProvider>() 
                                : null,
                            config?.Notifications?.Email?.Provider?.Equals("microsoft-graph", StringComparison.OrdinalIgnoreCase) == true 
                                ? sp.GetRequiredService<MicrosoftGraphEmailProvider>() 
                                : null,
                            config?.Notifications?.Sms?.Enabled == true 
                                ? sp.GetRequiredService<ISmsProvider>() 
                                : null,
                            sp.GetRequiredService<ILogger<NotificationOrchestrator>>());
                    });

                    // Register background service
                    services.AddHostedService<UnifiWatchService>();
                })
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.AddDebug();
                })
                .UseConsoleLifetime()
                .Build();

            // Add platform-specific service hosting
            #if WINDOWS
            host = host.UseWindowsService();
            #elif LINUX
            host = host.UseSystemd();
            #endif

            await host.RunAsync();
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Service Mode Error: {ex.Message}[/]");
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}
