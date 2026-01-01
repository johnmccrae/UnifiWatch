using System.Diagnostics;
using UnifiWatch.Models;
using UnifiWatch.Services.Localization;
using UnifiWatch.Services.Notifications;

namespace UnifiWatch.Services;

public class StockWatcher
{
    private readonly IunifiwatchService _stockService;
    private readonly string _store;
    private readonly NotificationOrchestrator? _notifier;

    public StockWatcher(IunifiwatchService stockService, string store, NotificationOrchestrator? notifier = null)
    {
        _stockService = stockService;
        _store = store;
        _notifier = notifier; // optional; tests can omit, CLI passes via DI
    }

    public async Task WaitForStockAsync(
        string[]? productNames,
        string[]? productSkus,
        int checkIntervalSeconds = 60,
        bool doNotOpenWebsite = false,
        bool doNotPlaySound = false,
        CancellationToken cancellationToken = default)
    {
        var loc = ServiceProviderHolder.GetService<ResourceLocalizer>()
                  ?? ResourceLocalizerHolder.Instance
                  ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
        // Get initial stock to validate products
        var currentStock = await _stockService.GetStockAsync(_store, null, cancellationToken);

        var cache = currentStock.ToDictionary(p => p.Name, p => p);
        foreach (var product in currentStock)
        {
            if (!cache.ContainsKey(product.SKU))
                cache[product.SKU] = product;
        }

        var applicableProducts = new List<string>();

        // Validate product names
        if (productNames != null)
        {
            foreach (var name in productNames)
            {
                var found = false;
                foreach (var stockName in currentStock.Select(p => p.Name))
                {
                    if (stockName.Contains(name, StringComparison.OrdinalIgnoreCase))
                    {
                        applicableProducts.Add(stockName);
                        found = true;
                    }
                }
                if (!found)
                {
                    Console.WriteLine(loc.Error("Warning.ProductNameNotFound", name));
                }
            }
        }

        // Validate product SKUs
        if (productSkus != null)
        {
            foreach (var sku in productSkus)
            {
                if (currentStock.Any(p => p.SKU == sku))
                {
                    applicableProducts.Add(sku);
                }
                else
                {
                    Console.WriteLine(loc.Error("Warning.ProductSkuNotFound", sku));
                }
            }
        }

        applicableProducts = applicableProducts.Distinct().ToList();

        if (applicableProducts.Count == 0)
        {
            Console.WriteLine(loc.Error("Error.NoValidProductsFound"));
            return;
        }

        Console.WriteLine(loc.CLI("Monitor.MonitoringFor", string.Join(", ", applicableProducts)));

        var count = 0;
        List<UnifiProduct> availableProducts;

        try
        {
            do
            {
                if (count > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(checkIntervalSeconds), cancellationToken);
                }

                Console.WriteLine(loc.CLI("Monitor.CheckingStock"));

                var currentResults = await _stockService.GetStockAsync(_store, null, cancellationToken);

                var matchingProducts = currentResults
                    .Where(p => applicableProducts.Contains(p.Name) || applicableProducts.Contains(p.SKU))
                    .OrderBy(p => p.Name)
                    .ToList();

                availableProducts = matchingProducts.Where(p => p.Available).ToList();

                Console.WriteLine(loc.CLI("Monitor.CheckingStockDone", checkIntervalSeconds));
                count++;
            }
            while (availableProducts.Count == 0);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine(loc.CLI("Monitor.MonitoringCancelled"));
            return;
        }

        // Show notification for all available products
        var productList = string.Join(", ", availableProducts.Select(p => p.Name));
        var title = loc.Notification("StockAlert.Title");
        var message = loc.Notification("Notification.ProductsAvailable", availableProducts.Count, productList);
        NotificationService.ShowNotification(title, message);

        if (_notifier != null)
        {
            await _notifier.NotifyInStockAsync(availableProducts, _store, cancellationToken);
        }

        foreach (var product in availableProducts)
        {
            Console.WriteLine(loc.Notification("Product.InStock", product.Name, product.SKU));

            if (!doNotOpenWebsite)
            {
                OpenUrl(product.ProductUrl);
            }

            if (!doNotPlaySound)
            {
                PlayNotificationSound();
            }
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            var loc = ServiceProviderHolder.GetService<ResourceLocalizer>()
                      ?? ResourceLocalizerHolder.Instance
                      ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
            Console.WriteLine(loc.Error("Error.OpenUrlFailed", ex.Message));
        }
    }

    private static void PlayNotificationSound()
    {
        try
        {
            // Windows-only beep
            if (OperatingSystem.IsWindows())
            {
                Console.Beep(500, 300);
            }
            else
            {
                var loc = ServiceProviderHolder.GetService<ResourceLocalizer>()
                          ?? ResourceLocalizerHolder.Instance
                          ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
                Console.WriteLine(loc.Notification("Notification.Bell"));
            }
        }
        catch
        {
            // Beep not supported on this platform
            var loc = ServiceProviderHolder.GetService<ResourceLocalizer>()
                      ?? ResourceLocalizerHolder.Instance
                      ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
            Console.WriteLine(loc.Notification("Notification.Bell"));
        }
    }
}
