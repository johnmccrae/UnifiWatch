using System.CommandLine;
using System.CommandLine.Invocation;
using UnifiWatch.Configuration;
using UnifiWatch.Services;
using UnifiWatch.Services.Credentials;
using UnifiWatch.Services.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnifiWatch.Services.Localization;
using Microsoft.Extensions.DependencyInjection;
using UnifiWatch.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Hosting.WindowsServices;
using UnifiWatch.Services.Installation;
using UnifiWatch.Services.Configuration;

namespace UnifiWatch;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Create language option first for early parsing
        var languageOption = new Option<string?>("--language", "Override language/culture (e.g., fr-CA, de-DE, es-ES)");
        
        var rootCommand = new RootCommand("UnifiWatch - Monitor stock availability for Ubiquiti products");
        rootCommand.AddGlobalOption(languageOption);

        // Mode options (mutually exclusive)
        var stockOption = new Option<bool>("--stock", "Get current stock");
        var waitOption = new Option<bool>("--wait", "Wait for products to be in stock");
        var serviceModeOption = new Option<bool>("--service-mode", "Run UnifiWatch as a background service");
        
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

        // Service management options
        var installServiceOption = new Option<bool>("--install-service", "Install UnifiWatch as a system service");
        var uninstallServiceOption = new Option<bool>("--uninstall-service", "Uninstall UnifiWatch system service");
        var startServiceOption = new Option<bool>("--start-service", "Start UnifiWatch service");
        var stopServiceOption = new Option<bool>("--stop-service", "Stop UnifiWatch service");
        var serviceStatusOption = new Option<bool>("--service-status", "Check UnifiWatch service status");

        // Configuration options
        var configureOption = new Option<bool>("--configure", "Interactive configuration wizard");
        var showConfigOption = new Option<bool>("--show-config", "Display current configuration");

        var testEmailOption = new Option<bool>("--test-email", "Send a test email notification");

        // Initialize culture from CLI flag or configuration
        System.Globalization.CultureInfo? initializedCulture = null;
        ResourceLocalizer? localizer = null;
        IServiceProvider? serviceProvider = null;
        
        // Early parse to check for --language override
        var parseResult = rootCommand.Parse(args);
        var languageOverride = parseResult.GetValueForOption(languageOption);
        
        try
        {
            // Check if running in test-email mode to enable info logging
            var isTestEmailMode = args.Contains("--test-email");
            var minLogLevel = isTestEmailMode ? LogLevel.Information : LogLevel.Warning;
            
            var loggerFactory = LoggerFactory.Create(builder => 
            {
                builder.AddConsole();
                builder.SetMinimumLevel(minLogLevel);
                // Always suppress ConfigurationProvider info unless in test mode
                if (!isTestEmailMode)
                {
                    builder.AddFilter("UnifiWatch.Configuration.ConfigurationProvider", LogLevel.Warning);
                }
            });
            var services = new ServiceCollection();
            services.AddSingleton(loggerFactory);
            services.AddLogging(builder => 
            {
                builder.AddConsole();
                builder.SetMinimumLevel(minLogLevel);
                // Always suppress ConfigurationProvider info unless in test mode
                if (!isTestEmailMode)
                {
                    builder.AddFilter("UnifiWatch.Configuration.ConfigurationProvider", LogLevel.Warning);
                }
            });
            var configProvider = new ConfigurationProvider(loggerFactory.CreateLogger<ConfigurationProvider>());
            services.AddSingleton<IConfigurationProvider>(configProvider);
            
            // Register credential provider
            var credentialProvider = CredentialProviderFactory.CreateProvider("auto", loggerFactory);
            services.AddSingleton<ICredentialProvider>(credentialProvider);
            
            // Load configuration to extract notification settings
            Configuration.ServiceConfiguration? config = null;
            try
            {
                config = await configProvider.LoadAsync(CancellationToken.None);
            }
            catch
            {
                // If config doesn't exist, use defaults
                config = new Configuration.ServiceConfiguration();
            }
            
            // Register notification services (email and SMS)
            // Load from config.json, with sensible defaults

            // HTTP client for Graph
            services.AddHttpClient(nameof(GraphEmailProvider));

            // Email notification service
            services.AddSingleton<IOptions<Services.Notifications.EmailNotificationSettings>>(sp => 
            {
                // Map from Configuration.EmailNotificationConfig to Services.Notifications.EmailNotificationSettings
                var configEmail = config?.Notifications?.Email;
                var emailSettings = new Services.Notifications.EmailNotificationSettings
                {
                    Enabled = configEmail?.Enabled ?? false,
                    SmtpServer = configEmail?.SmtpServer ?? string.Empty,
                    SmtpPort = configEmail?.SmtpPort ?? 587,
                    UseTls = configEmail?.UseTls ?? true,
                    FromAddress = configEmail?.FromAddress ?? string.Empty,
                    Recipients = configEmail?.Recipients ?? new List<string>(),
                    CredentialKey = configEmail?.CredentialKey ?? "email-smtp",
                    UseOAuth = configEmail?.UseOAuth ?? false,
                    OAuthTenantId = configEmail?.OAuthTenantId ?? string.Empty,
                    OAuthClientId = configEmail?.OAuthClientId ?? string.Empty,
                    OAuthCredentialKey = configEmail?.OAuthCredentialKey ?? "email-oauth",
                    OAuthMailbox = configEmail?.OAuthMailbox ?? string.Empty
                };
                return Options.Create(emailSettings);
            });

            services.AddSingleton<IEmailProvider>(sp =>
            {
                var emailOpts = sp.GetRequiredService<IOptions<Services.Notifications.EmailNotificationSettings>>().Value;
                if (emailOpts.UseOAuth)
                {
                    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GraphEmailProvider));
                    return new GraphEmailProvider(
                        sp.GetRequiredService<ILogger<GraphEmailProvider>>(),
                        sp.GetRequiredService<IOptions<Services.Notifications.EmailNotificationSettings>>(),
                        sp.GetRequiredService<ICredentialProvider>(),
                        httpClient);
                }

                return new SmtpEmailProvider(
                    sp.GetRequiredService<ILogger<SmtpEmailProvider>>(),
                    sp.GetRequiredService<IOptions<Services.Notifications.EmailNotificationSettings>>(),
                    sp.GetRequiredService<ICredentialProvider>());
            });

            services.AddSingleton<EmailNotificationService>(sp => new EmailNotificationService(
                sp.GetRequiredService<IEmailProvider>(),
                sp.GetRequiredService<IResourceLocalizer>(),
                sp.GetRequiredService<ILogger<EmailNotificationService>>()));
            
            // SMS notification service
            services.AddSingleton<IOptions<Services.Notifications.SmsNotificationSettings>>(sp =>
            {
                // Map from Configuration.SmsNotificationConfig to Services.Notifications.SmsNotificationSettings
                var configSms = config?.Notifications?.Sms;
                var smsSettings = new Services.Notifications.SmsNotificationSettings
                {
                    Enabled = configSms?.Enabled ?? false,
                    ServiceType = configSms?.Provider ?? "twilio",
                    Recipients = configSms?.Recipients ?? new List<string>(),
                    AuthTokenKeyName = configSms?.CredentialKey ?? "sms:auth-token"
                };
                return Options.Create(smsSettings);
            });
            
            services.AddSingleton<ISmsProvider>(sp => new TwilioSmsProvider(
                sp.GetRequiredService<IOptions<Services.Notifications.SmsNotificationSettings>>(),
                sp.GetRequiredService<ICredentialProvider>(),
                sp.GetRequiredService<ILogger<TwilioSmsProvider>>()));
            services.AddSingleton<SmsNotificationService>(sp => new SmsNotificationService(
                sp.GetRequiredService<ISmsProvider>(),
                sp.GetRequiredService<IResourceLocalizer>(),
                sp.GetRequiredService<ILogger<SmsNotificationService>>(),
                sp.GetRequiredService<IOptions<Services.Notifications.SmsNotificationSettings>>().Value));
            
            // Register NotificationOrchestrator with configurable dedupe window
            services.AddSingleton<NotificationOrchestrator>(sp =>
            {
                var emailSvc = sp.GetRequiredService<EmailNotificationService>();
                var smsSvc = sp.GetRequiredService<SmsNotificationService>();
                var emailOpts = sp.GetRequiredService<IOptions<Services.Notifications.EmailNotificationSettings>>();
                var smsOpts = sp.GetRequiredService<IOptions<Services.Notifications.SmsNotificationSettings>>();
                var logger = sp.GetRequiredService<ILogger<NotificationOrchestrator>>();
                
                // Validate dedupe window (1-60 minutes, default 5)
                var dedupeMinutes = config?.Notifications?.DedupeMinutes ?? 5;
                dedupeMinutes = Math.Clamp(dedupeMinutes, 1, 60);
                var dedupeWindow = TimeSpan.FromMinutes(dedupeMinutes);
                
                return new NotificationOrchestrator(emailSvc, smsSvc, emailOpts, smsOpts, logger, dedupeWindow);
            });
            
            System.Globalization.CultureInfo culture;
            if (!string.IsNullOrWhiteSpace(languageOverride))
            {
                // CLI flag takes precedence
                culture = System.Globalization.CultureInfo.GetCultureInfo(languageOverride);
            }
            else
            {
                // Check config file
                var cultureProvider = new UnifiWatch.Services.Localization.CultureProvider(configProvider);
                culture = await cultureProvider.GetUserCultureAsync(CancellationToken.None);
            }
            
            System.Globalization.CultureInfo.CurrentCulture = culture;
            System.Globalization.CultureInfo.CurrentUICulture = culture;
            initializedCulture = culture;
            localizer = ResourceLocalizer.Load(culture);
            services.AddSingleton<IResourceLocalizer>(localizer);
            services.AddSingleton(localizer);
            serviceProvider = services.BuildServiceProvider();
            // Cache localizer instance for reuse (back-compat)
            ResourceLocalizerHolder.Instance = localizer;
            ServiceProviderHolder.Provider = serviceProvider;
        }
        catch
        {
            // Ignore culture init failures; fallback remains default
            localizer = ResourceLocalizer.Load(System.Globalization.CultureInfo.GetCultureInfo("en-CA"));
            ResourceLocalizerHolder.Instance = localizer;
        }

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
            languageOption.Description = localizer.CLI("LanguageOption.Description");
            serviceModeOption.Description = localizer.CLI("ServiceMode.Description", "Run UnifiWatch as a background service");
            installServiceOption.Description = localizer.CLI("InstallService.Description", "Install UnifiWatch as a system service");
            uninstallServiceOption.Description = localizer.CLI("UninstallService.Description", "Uninstall UnifiWatch system service");
            startServiceOption.Description = localizer.CLI("StartService.Description", "Start UnifiWatch service");
            stopServiceOption.Description = localizer.CLI("StopService.Description", "Stop UnifiWatch service");
            serviceStatusOption.Description = localizer.CLI("ServiceStatus.Description", "Check UnifiWatch service status");
            configureOption.Description = localizer.CLI("Configure.Description", "Interactive configuration wizard");
            showConfigOption.Description = localizer.CLI("ShowConfig.Description", "Display current configuration");
            testEmailOption.Description = localizer.CLI("TestEmail.Description", "Send a test email notification");

            rootCommand.AddOption(stockOption);
            rootCommand.AddOption(waitOption);
            rootCommand.AddOption(serviceModeOption);
            rootCommand.AddOption(installServiceOption);
            rootCommand.AddOption(uninstallServiceOption);
            rootCommand.AddOption(startServiceOption);
            rootCommand.AddOption(stopServiceOption);
            rootCommand.AddOption(serviceStatusOption);
            rootCommand.AddOption(configureOption);
            rootCommand.AddOption(showConfigOption);
            rootCommand.AddOption(testEmailOption);
            rootCommand.AddOption(storeOption);
            rootCommand.AddOption(legacyApiStoreOption);
            rootCommand.AddOption(collectionsOption);
            rootCommand.AddOption(productNamesOption);
            rootCommand.AddOption(productSkusOption);
            rootCommand.AddOption(secondsOption);
            rootCommand.AddOption(noWebsiteOption);
            rootCommand.AddOption(noSoundOption);
        }
        else
        {
            rootCommand.AddOption(stockOption);
            rootCommand.AddOption(waitOption);
            rootCommand.AddOption(serviceModeOption);
            rootCommand.AddOption(installServiceOption);
            rootCommand.AddOption(uninstallServiceOption);
            rootCommand.AddOption(startServiceOption);
            rootCommand.AddOption(stopServiceOption);
            rootCommand.AddOption(serviceStatusOption);
            rootCommand.AddOption(configureOption);
            rootCommand.AddOption(showConfigOption);
            rootCommand.AddOption(testEmailOption);
            rootCommand.AddOption(storeOption);
            rootCommand.AddOption(legacyApiStoreOption);
            rootCommand.AddOption(collectionsOption);
            rootCommand.AddOption(productNamesOption);
            rootCommand.AddOption(productSkusOption);
            rootCommand.AddOption(secondsOption);
            rootCommand.AddOption(noWebsiteOption);
            rootCommand.AddOption(noSoundOption);
        }

        rootCommand.SetHandler(async (InvocationContext context) =>
        {
            var stock = context.ParseResult.GetValueForOption(stockOption);
            var wait = context.ParseResult.GetValueForOption(waitOption);
            var serviceMode = context.ParseResult.GetValueForOption(serviceModeOption);
            var installService = context.ParseResult.GetValueForOption(installServiceOption);
            var uninstallService = context.ParseResult.GetValueForOption(uninstallServiceOption);
            var startService = context.ParseResult.GetValueForOption(startServiceOption);
            var stopService = context.ParseResult.GetValueForOption(stopServiceOption);
            var serviceStatus = context.ParseResult.GetValueForOption(serviceStatusOption);
            var configure = context.ParseResult.GetValueForOption(configureOption);
            var showConfig = context.ParseResult.GetValueForOption(showConfigOption);
            var store = context.ParseResult.GetValueForOption(storeOption);
                        var testEmail = context.ParseResult.GetValueForOption(testEmailOption);
            var legacyStore = context.ParseResult.GetValueForOption(legacyApiStoreOption);
            var collections = context.ParseResult.GetValueForOption(collectionsOption);
            var productNames = context.ParseResult.GetValueForOption(productNamesOption);
            var productSkus = context.ParseResult.GetValueForOption(productSkusOption);
            var seconds = context.ParseResult.GetValueForOption(secondsOption);
            var noWebsite = context.ParseResult.GetValueForOption(noWebsiteOption);
            var noSound = context.ParseResult.GetValueForOption(noSoundOption);

            // Handle service management commands (all other modes skip mode validation)
            // These require admin privileges
            if (installService)
            {
                if (!IsElevatedMode())
                {
                    Console.WriteLine("? Service installation requires administrator privileges.");
                    Console.WriteLine("Please run the command with elevated privileges (Run as Administrator).");
                    context.ExitCode = 1;
                    return;
                }
                await HandleInstallServiceAsync();
                context.ExitCode = 0;
                return;
            }
            if (uninstallService)
            {
                if (!IsElevatedMode())
                {
                    Console.WriteLine("? Service uninstallation requires administrator privileges.");
                    Console.WriteLine("Please run the command with elevated privileges (Run as Administrator).");
                    context.ExitCode = 1;
                    return;
                }
                await HandleUninstallServiceAsync();
                context.ExitCode = 0;
                return;
            }
            if (startService)
            {
                await HandleStartServiceAsync();
                context.ExitCode = 0;
                return;
            }
            if (stopService)
            {
                await HandleStopServiceAsync();
                context.ExitCode = 0;
                return;
            }
            if (serviceStatus)
            {
                await HandleServiceStatusAsync();
                context.ExitCode = 0;
                return;
            }
            if (configure)
            {
                await HandleConfigureAsync(serviceProvider);
                context.ExitCode = 0;
                return;
            }
            if (testEmail)
            {
                await HandleTestEmailAsync(serviceProvider);
                context.ExitCode = 0;
                return;
            }
            if (showConfig)
            {
                await HandleShowConfigAsync(serviceProvider);
                context.ExitCode = 0;
                return;
            }

            // Service mode bypasses stock/wait/store validations
            if (serviceMode)
            {
                await RunServiceModeAsync();
                context.ExitCode = 0;
                return;
            }

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
                    await GetStockAsync(selectedStore, collections, productNames, productSkus, isLegacy);
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

    static async Task GetStockAsync(string store, string[]? collections, string[]? productNames, string[]? productSkus, bool isLegacy)
    {
        using var httpClient = new HttpClient();

        try
        {
            IunifiwatchService service = isLegacy
                ? new unifiwatchLegacyService(httpClient)
                : new unifiwatchService(httpClient);

            var products = await service.GetStockAsync(store, collections);
            
            // Apply product filters if specified
            if ((productNames?.Length ?? 0) > 0)
            {
                var nameSet = new HashSet<string>(productNames!, StringComparer.OrdinalIgnoreCase);
                products = products.Where(p => nameSet.Contains(p.Name)).ToList();
            }
            
            if ((productSkus?.Length ?? 0) > 0)
            {
                var skuSet = new HashSet<string>(productSkus!, StringComparer.OrdinalIgnoreCase);
                products = products.Where(p => !string.IsNullOrEmpty(p.SKU) && skuSet.Contains(p.SKU)).ToList();
            }
            
            DisplayProducts(products);
        }
        catch (Exception ex)
        {
            var loc = ResourceLocalizerHolder.Instance ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
            Console.WriteLine(loc.Error("Error.OperationFailed", ex.Message));
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

            var orchestrator = ServiceProviderHolder.GetService<NotificationOrchestrator>();
            var watcher = new StockWatcher(stockService, store, orchestrator);
            await watcher.WaitForStockAsync(productNames, productSkus, seconds, noWebsite, noSound);
        }
        catch (Exception ex)
        {
            var loc = ResourceLocalizerHolder.Instance ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
            Console.WriteLine(loc.Error("Error.OperationFailed", ex.Message));
            throw;
        }
    }

    static void DisplayProducts(List<UnifiWatch.Models.UnifiProduct> products)
    {
        var loc = ResourceLocalizerHolder.Instance ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
        Console.WriteLine("\n" + loc.CLI("List.FoundProducts", products.Count) + "\n");
        Console.WriteLine("{0,-46} {1,-16} {2,-35} {3,-23} {4,10}", 
            loc.CLI("List.Headers.Name"), loc.CLI("List.Headers.Available"), loc.CLI("List.Headers.Category"), loc.CLI("List.Headers.SKU"), loc.CLI("List.Headers.Price"));
        Console.WriteLine(new string('-', 135));

        foreach (var product in products)
        {
            var availability = product.Available ? loc.CLI("List.InStock") : loc.CLI("List.OutOfStock");
            var price = product.Price.HasValue ? (product.Price.Value / 100).ToString("F2", System.Globalization.CultureInfo.CurrentCulture) : loc.CLI("List.PriceNA");
            
            Console.WriteLine("{0,-46} {1,-16} {2,-35} {3,-23} {4,10}",
                product.Name.Length > 43 ? product.Name.Substring(0, 43) + "..." : product.Name,
                availability,
                product.Category?.Length > 32 ? product.Category.Substring(0, 32) + "..." : product.Category ?? loc.CLI("List.CategoryNA"),
                product.SKU ?? loc.CLI("List.SKUNA"),
                price);
        }
    }

    private static async Task RunServiceModeAsync()
    {
        try
        {
            Console.Out.WriteLine("[Service] Initializing service host builder...");
            Console.Out.Flush();
            
            var hostBuilder = CreateServiceHostBuilder();
            Console.Out.WriteLine("[Service] Building host...");
            Console.Out.Flush();
            
            using var host = hostBuilder.Build();
            Console.Out.WriteLine("[Service] Host built successfully, running...");
            Console.Out.Flush();
            
            await host.RunAsync();
            Console.Out.WriteLine("[Service] Service exited normally");
            Console.Out.Flush();
        }
        catch (Exception ex)
        {
            var msg = $"FATAL ERROR: Service host crashed: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            Console.WriteLine(msg);
            Console.Out.Flush();
            try
            {
                System.Diagnostics.EventLog.WriteEntry("Application", msg, System.Diagnostics.EventLogEntryType.Error);
            }
            catch { }
            throw;
        }
    }

    private static IHostBuilder CreateServiceHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .UseWindowsService(options => options.ServiceName = "UnifiWatch")
            .ConfigureLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
            })
            .ConfigureServices(services =>
            {
                // Core services
                services.AddSingleton<IConfigurationProvider, ConfigurationProvider>();
                
                // Stock service with fallback
                services.AddHttpClient(nameof(unifiwatchService));
                services.AddHttpClient(nameof(unifiwatchLegacyService));
                services.AddSingleton<IunifiwatchService>(sp =>
                {
                    try
                    {
                        var cfgProvider = sp.GetRequiredService<IConfigurationProvider>();
                        var cfg = cfgProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult()
                                  ?? cfgProvider.GetDefaultConfiguration();
                        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                        return cfg.Monitoring.UseModernApi
                            ? new unifiwatchService(httpClientFactory.CreateClient(nameof(unifiwatchService)))
                            : new unifiwatchLegacyService(httpClientFactory.CreateClient(nameof(unifiwatchLegacyService)));
                    }
                    catch (Exception ex)
                    {
                        var logger = sp.GetRequiredService<ILogger<Program>>();
                        logger.LogError(ex, "Failed to load stock service configuration, using defaults");
                        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                        return new unifiwatchService(httpClientFactory.CreateClient(nameof(unifiwatchService)));
                    }
                });
                
                // Localization
                services.AddSingleton<IResourceLocalizer>(sp => ResourceLocalizer.Load(System.Globalization.CultureInfo.GetCultureInfo("en-CA")));
                services.AddSingleton<ResourceLocalizer>(sp => (ResourceLocalizer)sp.GetRequiredService<IResourceLocalizer>());
                
                // Credentials provider
                services.AddSingleton<ICredentialProvider>(sp =>
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    return CredentialProviderFactory.CreateProvider("auto", loggerFactory);
                });
                
                // Notification services (load real config)
                services.AddHttpClient(nameof(GraphEmailProvider));

                services.AddSingleton<IOptions<Services.Notifications.EmailNotificationSettings>>(sp =>
                {
                    var cfgProvider = sp.GetRequiredService<IConfigurationProvider>();
                    var cfg = cfgProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult()
                              ?? cfgProvider.GetDefaultConfiguration();

                    var configEmail = cfg.Notifications.Email;
                    var emailSettings = new Services.Notifications.EmailNotificationSettings
                    {
                        Enabled = configEmail.Enabled,
                        SmtpServer = configEmail.SmtpServer,
                        SmtpPort = configEmail.SmtpPort,
                        UseTls = configEmail.UseTls,
                        FromAddress = configEmail.FromAddress,
                        Recipients = configEmail.Recipients,
                        CredentialKey = configEmail.CredentialKey ?? "email-smtp",
                        UseOAuth = configEmail.UseOAuth,
                        OAuthTenantId = configEmail.OAuthTenantId,
                        OAuthClientId = configEmail.OAuthClientId,
                        OAuthCredentialKey = configEmail.OAuthCredentialKey ?? "email-oauth",
                        OAuthMailbox = configEmail.OAuthMailbox
                    };
                    return Options.Create(emailSettings);
                });

                services.AddSingleton<IOptions<Services.Notifications.SmsNotificationSettings>>(sp =>
                {
                    var cfgProvider = sp.GetRequiredService<IConfigurationProvider>();
                    var cfg = cfgProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult()
                              ?? cfgProvider.GetDefaultConfiguration();
                    var configSms = cfg.Notifications.Sms;
                    var smsSettings = new Services.Notifications.SmsNotificationSettings
                    {
                        Enabled = configSms.Enabled,
                        ServiceType = configSms.Provider,
                        Recipients = configSms.Recipients,
                        AuthTokenKeyName = configSms.CredentialKey ?? "sms:auth-token"
                    };
                    return Options.Create(smsSettings);
                });

                services.AddSingleton<IEmailProvider>(sp =>
                {
                    var emailOpts = sp.GetRequiredService<IOptions<Services.Notifications.EmailNotificationSettings>>().Value;
                    if (emailOpts.UseOAuth)
                    {
                        var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GraphEmailProvider));
                        return new GraphEmailProvider(
                            sp.GetRequiredService<ILogger<GraphEmailProvider>>(),
                            sp.GetRequiredService<IOptions<Services.Notifications.EmailNotificationSettings>>(),
                            sp.GetRequiredService<ICredentialProvider>(),
                            httpClient);
                    }

                    return new SmtpEmailProvider(
                        sp.GetRequiredService<ILogger<SmtpEmailProvider>>(),
                        sp.GetRequiredService<IOptions<Services.Notifications.EmailNotificationSettings>>(),
                        sp.GetRequiredService<ICredentialProvider>());
                });

                services.AddSingleton<EmailNotificationService>(sp => new EmailNotificationService(
                    sp.GetRequiredService<IEmailProvider>(),
                    sp.GetRequiredService<IResourceLocalizer>(),
                    sp.GetRequiredService<ILogger<EmailNotificationService>>()));

                services.AddSingleton<ISmsProvider>(sp => new TwilioSmsProvider(
                    sp.GetRequiredService<IOptions<Services.Notifications.SmsNotificationSettings>>(),
                    sp.GetRequiredService<ICredentialProvider>(), 
                    sp.GetRequiredService<ILogger<TwilioSmsProvider>>()));

                services.AddSingleton<SmsNotificationService>(sp => new SmsNotificationService(
                    sp.GetRequiredService<ISmsProvider>(),
                    sp.GetRequiredService<IResourceLocalizer>(),
                    sp.GetRequiredService<ILogger<SmsNotificationService>>(),
                    sp.GetRequiredService<IOptions<Services.Notifications.SmsNotificationSettings>>().Value));

                // Notification orchestrator (uses configured dedupe window)
                services.AddSingleton<NotificationOrchestrator>(sp =>
                {
                    var cfgProvider = sp.GetRequiredService<IConfigurationProvider>();
                    var cfg = cfgProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult()
                              ?? cfgProvider.GetDefaultConfiguration();

                    var emailSvc = sp.GetRequiredService<EmailNotificationService>();
                    var smsSvc = sp.GetRequiredService<SmsNotificationService>();
                    var emailOpts = sp.GetRequiredService<IOptions<Services.Notifications.EmailNotificationSettings>>();
                    var smsOpts = sp.GetRequiredService<IOptions<Services.Notifications.SmsNotificationSettings>>();
                    var orchLogger = sp.GetRequiredService<ILogger<NotificationOrchestrator>>();
                    
                    var dedupeMinutes = Math.Clamp(cfg.Notifications.DedupeMinutes, 1, 60);
                    var dedupeWindow = TimeSpan.FromMinutes(dedupeMinutes);
                    
                    orchLogger.LogInformation("NotificationOrchestrator initialized (email enabled: {Email}, sms enabled: {Sms}, dedupe: {Dedupe} minutes)",
                        emailOpts.Value.Enabled, smsOpts.Value.Enabled, dedupeMinutes);
                    
                    return new NotificationOrchestrator(emailSvc, smsSvc, emailOpts, smsOpts, orchLogger, dedupeWindow);
                });
                
                // The hosted service
                services.AddHostedService<UnifiWatchService>();
            });
    }

    private static ServiceConfiguration LoadConfig(IServiceProvider serviceProvider)
    {
        var cfgProvider = serviceProvider.GetRequiredService<IConfigurationProvider>();
        return cfgProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult()
               ?? cfgProvider.GetDefaultConfiguration();
    }

    private static async Task HandleInstallServiceAsync()
    {
        try
        {
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(exePath))
            {
                // For single-file apps, use AppContext.BaseDirectory
                exePath = System.AppContext.BaseDirectory;
                if (!exePath.EndsWith(System.IO.Path.DirectorySeparatorChar))
                    exePath += System.IO.Path.DirectorySeparatorChar;
                exePath += OperatingSystem.IsWindows() ? "UnifiWatch.exe" : "UnifiWatch.dll";
            }
            else if (exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && OperatingSystem.IsWindows())
            {
                // Windows: Replace .dll with .exe for the service executable path
                // Linux/macOS: Keep .dll as they run via `dotnet assembly.dll`
                exePath = exePath.Substring(0, exePath.Length - 4) + ".exe";
            }

            var installer = ServiceInstallerFactory.CreateInstaller();

            var options = new ServiceInstallOptions
            {
                ServiceName = "UnifiWatch",
                DisplayName = "UnifiWatch Stock Monitor",
                Description = "Background service to monitor Ubiquiti product stock availability",
                ExecutablePath = exePath,
                StartupType = "Automatic",
                DelayedAutoStart = true,
                RestartAttemptsOnFailure = 3,
                RestartDelaySeconds = 10
            };

            Console.WriteLine("Installing UnifiWatch service...");
            var success = await installer.InstallAsync(options);

            if (success)
            {
                Console.WriteLine("✓ UnifiWatch service installed and started successfully");
            }
            else
            {
                Console.WriteLine("✗ Service installation or start failed. Check Event Viewer for details.");
            }
        }
        catch (PlatformNotSupportedException ex)
        {
            Console.WriteLine($"✗ Service installation not supported: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error installing service: {ex.GetBaseException().Message}");
        }
    }

    private static async Task HandleUninstallServiceAsync()
    {
        try
        {
            var installer = ServiceInstallerFactory.CreateInstaller();

            Console.WriteLine("Uninstalling UnifiWatch service...");
            var success = await installer.UninstallAsync();

            if (success)
            {
                Console.WriteLine("✓ UnifiWatch service uninstalled successfully");
            }
            else
            {
                Console.WriteLine("✗ Failed to uninstall UnifiWatch service");
            }
        }
        catch (PlatformNotSupportedException ex)
        {
            Console.WriteLine($"✗ Service uninstallation not supported: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error uninstalling service: {ex.Message}");
        }
    }

    private static async Task HandleStartServiceAsync()
    {
        try
        {
            var installer = ServiceInstallerFactory.CreateInstaller();

            Console.WriteLine("Starting UnifiWatch service...");
            var success = await installer.StartAsync();

            if (success)
            {
                Console.WriteLine("✓ UnifiWatch service started");
            }
            else
            {
                Console.WriteLine("✗ Failed to start UnifiWatch service");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error starting service: {ex.Message}");
        }
    }

    private static async Task HandleStopServiceAsync()
    {
        try
        {
            var installer = ServiceInstallerFactory.CreateInstaller();

            Console.WriteLine("Stopping UnifiWatch service...");
            var success = await installer.StopAsync();

            if (success)
            {
                Console.WriteLine("✓ UnifiWatch service stopped");
            }
            else
            {
                Console.WriteLine("✗ Failed to stop UnifiWatch service");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error stopping service: {ex.Message}");
        }
    }

    private static async Task HandleServiceStatusAsync()
    {
        try
        {
            var installer = ServiceInstallerFactory.CreateInstaller();

            var status = await installer.GetStatusAsync();
            Console.WriteLine($"Service Status: {status}");
            if (!string.IsNullOrEmpty(status.Message))
            {
                Console.WriteLine($"Details: {status.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error checking service status: {ex.Message}");
        }
    }

    private static async Task HandleConfigureAsync(IServiceProvider? serviceProvider)
    {
        try
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var configProvider = new ConfigurationProvider(loggerFactory.CreateLogger<ConfigurationProvider>());
            var credentialProvider = CredentialProviderFactory.CreateProvider("auto", loggerFactory);
            
            // Get localizer
            var localizer = serviceProvider?.GetService<ResourceLocalizer>()
                           ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);

            var wizard = new ConfigurationWizard(configProvider, credentialProvider, localizer);
            var success = await wizard.RunAsync();

            if (success)
            {
                Console.WriteLine($"\n✓ Configuration completed successfully");
            }
            else
            {
                Console.WriteLine($"\n✗ Configuration wizard cancelled or failed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error during configuration: {ex.Message}");
        }
    }

    private static bool IsElevatedMode()
    {
        try
        {
            if (!OperatingSystem.IsWindows())
                return true; // Non-Windows systems handle differently

            var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            var principal = new System.Security.Principal.WindowsPrincipal(identity);
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static async Task HandleShowConfigAsync(IServiceProvider? serviceProvider)
    {
        try
        {
            var loggerFactory = new LoggerFactory();
            var configProvider = new ConfigurationProvider(loggerFactory.CreateLogger<ConfigurationProvider>());
            
            // Get localizer
            var localizer = serviceProvider?.GetService<ResourceLocalizer>()
                           ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);

            var config = await configProvider.LoadAsync(CancellationToken.None)
                         ?? configProvider.GetDefaultConfiguration();

            ConfigurationDisplay.DisplayConfiguration(config, localizer, configProvider.ConfigurationFilePath);
            ConfigurationDisplay.ValidateConfiguration(config, localizer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error displaying configuration: {ex.Message}");
        }
    }
    private static async Task HandleTestEmailAsync(IServiceProvider? serviceProvider)
    {
        try
        {
            if (serviceProvider == null)
            {
                Console.WriteLine("✗ Service provider not available");
                return;
            }

            var emailService = serviceProvider.GetService<EmailNotificationService>();
            if (emailService == null)
            {
                Console.WriteLine("✗ Email notification service not available");
                return;
            }

            var emailSettings = serviceProvider.GetService<IOptions<Services.Notifications.EmailNotificationSettings>>()?.Value;
            if (emailSettings == null || !emailSettings.Enabled || emailSettings.Recipients.Count == 0)
            {
                Console.WriteLine("✗ Email notifications are disabled or no recipients are configured.");
                return;
            }

            Console.WriteLine("\nSending test email...");
            
            var testProduct = new UnifiProduct
            {
                Name = "Test Product",
                Available = true,
                Category = "Test",
                SKU = "TEST-SKU",
                Price = 99900
            };

            var allSuccess = true;
            foreach (var recipient in emailSettings.Recipients)
            {
                var sent = await emailService.SendProductInStockNotificationAsync(
                    testProduct,
                    recipient,
                    store: "TestStore",
                    culture: System.Globalization.CultureInfo.CurrentCulture,
                    cancellationToken: CancellationToken.None);

                if (sent)
                {
                    Console.WriteLine($"✓ Test email sent to {recipient}");
                }
                else
                {
                    Console.WriteLine($"✗ Failed to send test email to {recipient}");
                    allSuccess = false;
                }
            }

            if (allSuccess)
            {
                Console.WriteLine("\n✓ All test emails sent successfully!");
            }
            else
            {
                Console.WriteLine("\n✗ One or more test emails failed. Please check your email configuration.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error sending test email: {ex.Message}");
        }
    }
}

