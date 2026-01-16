using System.Diagnostics;
using UnifiWatch.Models;

namespace UnifiWatch.Services;

public class StockWatcher
{
    private readonly IUnifiStockService _stockService;
    private readonly string _store;
    public StockWatcher(IUnifiStockService stockService, string store)
    {
        _stockService = stockService;
        _store = store;
    }

    public async Task WaitForStockAsync(
        string[]? productNames,
        string[]? productSkus,
        int checkIntervalSeconds = 60,
        bool doNotOpenWebsite = false,
        bool doNotPlaySound = false,
        CancellationToken cancellationToken = default)
    {
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
                    Console.WriteLine($"Warning: Product Name '{name}' not found in stock. Ignoring");
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
                    Console.WriteLine($"Warning: Product SKU '{sku}' not found in stock. Ignoring");
                }
            }
        }

        applicableProducts = applicableProducts.Distinct().ToList();

        if (applicableProducts.Count == 0)
        {
            Console.WriteLine("Error: No valid products found. Exiting");
            return;
        }

        Console.WriteLine($"Monitoring for: {string.Join(", ", applicableProducts)}");

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

                Console.WriteLine("Checking stock...");

                var currentResults = await _stockService.GetStockAsync(_store, null, cancellationToken);

                var matchingProducts = currentResults
                    .Where(p => applicableProducts.Contains(p.Name) || applicableProducts.Contains(p.SKU))
                    .OrderBy(p => p.Name)
                    .ToList();

                availableProducts = matchingProducts.Where(p => p.Available).ToList();

                Console.WriteLine($"Checking stock... Done, sleeping for {checkIntervalSeconds} seconds");
                count++;
            }
            while (availableProducts.Count == 0);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Stock monitoring cancelled.");
            return;
        }

        // Show notification for all available products
        var productList = string.Join(", ", availableProducts.Select(p => p.Name));
        NotificationService.ShowNotification(
            "UniFi Stock Alert!", 
            $"{availableProducts.Count} product(s) in stock: {productList}");

        foreach (var product in availableProducts)
        {
            Console.WriteLine($"✓ Product '{product.Name}' is in stock! SKU: {product.SKU}");

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
            Console.WriteLine($"Failed to open URL: {ex.Message}");
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
                Console.WriteLine("🔔 Stock alert!");
            }
        }
        catch
        {
            // Beep not supported on this platform
            Console.WriteLine("🔔 Stock alert!");
        }
    }
}
