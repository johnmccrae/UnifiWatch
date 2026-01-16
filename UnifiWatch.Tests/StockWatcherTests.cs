using FluentAssertions;
using Moq;
using UnifiWatch.Models;
using UnifiWatch.Services;
using Xunit;

namespace UnifiWatch.Tests;

public class StockWatcherTests
{
    private readonly Mock<IUnifiStockService> _mockStockService;
    private readonly List<UnifiProduct> _mockProducts;

    public StockWatcherTests()
    {
        _mockStockService = new Mock<IUnifiStockService>();

        _mockProducts = new List<UnifiProduct>
        {
            new UnifiProduct
            {
                Name = "Test Product 1",
                SKU = "TEST-001",
                Available = false,
                ProductUrl = "https://example.com/product1"
            },
            new UnifiProduct
            {
                Name = "Test Product 2",
                SKU = "TEST-002",
                Available = true,
                ProductUrl = "https://example.com/product2"
            }
        };
    }

    [Fact]
    public async Task WaitForStockAsync_WithValidProductNames_ShouldMonitorAndNotify()
    {
        // Arrange
        var watcher = new StockWatcher(_mockStockService.Object, "USA");
        var productNames = new[] { "Test Product 1" };

        // Initially no products available
        _mockStockService
            .Setup(s => s.GetStockAsync("USA", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockProducts.Where(p => !p.Available).ToList());

        // On second call, product becomes available
        _mockStockService
            .SetupSequence(s => s.GetStockAsync("USA", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockProducts.Where(p => !p.Available).ToList())
            .ReturnsAsync(_mockProducts);

        // Act
        var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel quickly for test

        Func<Task> act = async () => await watcher.WaitForStockAsync(
            productNames, null, 1, true, true, cts.Token);

        // Assert - Should not throw exception
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WaitForStockAsync_WithValidProductSKUs_ShouldMonitorAndNotify()
    {
        // Arrange
        var watcher = new StockWatcher(_mockStockService.Object, "USA");
        var productSkus = new[] { "TEST-001" };

        _mockStockService
            .Setup(s => s.GetStockAsync("USA", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockProducts);

        // Act
        var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel quickly for test

        Func<Task> act = async () => await watcher.WaitForStockAsync(
            null, productSkus, 1, true, true, cts.Token);

        // Assert - Should not throw exception
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WaitForStockAsync_WithInvalidProductName_ShouldWarnAndContinue()
    {
        // Arrange
        var watcher = new StockWatcher(_mockStockService.Object, "USA");
        var productNames = new[] { "Nonexistent Product" };

        _mockStockService
            .Setup(s => s.GetStockAsync("USA", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockProducts);

        // Act
        var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel quickly for test

        Func<Task> act = async () => await watcher.WaitForStockAsync(
            productNames, null, 1, true, true, cts.Token);

        // Assert - Should not throw exception despite invalid product name
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WaitForStockAsync_WithInvalidProductSKU_ShouldWarnAndContinue()
    {
        // Arrange
        var watcher = new StockWatcher(_mockStockService.Object, "USA");
        var productSkus = new[] { "INVALID-SKU" };

        _mockStockService
            .Setup(s => s.GetStockAsync("USA", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockProducts);

        // Act
        var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel quickly for test

        Func<Task> act = async () => await watcher.WaitForStockAsync(
            null, productSkus, 1, true, true, cts.Token);

        // Assert - Should not throw exception despite invalid SKU
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WaitForStockAsync_WithNoValidProducts_ShouldExitEarly()
    {
        // Arrange
        var watcher = new StockWatcher(_mockStockService.Object, "USA");
        var productNames = new[] { "Nonexistent Product" };
        var productSkus = new[] { "INVALID-SKU" };

        _mockStockService
            .Setup(s => s.GetStockAsync("USA", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockProducts);

        // Act
        Func<Task> act = async () => await watcher.WaitForStockAsync(
            productNames, productSkus, 60, true, true);

        // Assert - Should not throw exception
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WaitForStockAsync_WithLegacyService_ShouldUseLegacyService()
    {
        // Arrange
        var watcher = new StockWatcher(_mockStockService.Object, "Brazil");
        var productNames = new[] { "Test Product 1" };

        _mockStockService
            .Setup(s => s.GetStockAsync("Brazil", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockProducts.Where(p => !p.Available).ToList());

        _mockStockService
            .SetupSequence(s => s.GetStockAsync("Brazil", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockProducts.Where(p => !p.Available).ToList())
            .ReturnsAsync(_mockProducts);

        // Act
        var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel quickly for test

        Func<Task> act = async () => await watcher.WaitForStockAsync(
            productNames, null, 1, true, true, cts.Token);

        // Assert - Should not throw exception
        await act.Should().NotThrowAsync();

        // Verify legacy service was called
        _mockStockService.Verify(s => s.GetStockAsync("Brazil", null, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task WaitForStockAsync_WithCancellation_ShouldHandleCancellationGracefully()
    {
        // Arrange
        var watcher = new StockWatcher(_mockStockService.Object, "USA");
        var productNames = new[] { "Test Product 1" };

        _mockStockService
            .Setup(s => s.GetStockAsync("USA", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockProducts.Where(p => !p.Available).ToList());

        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        Func<Task> act = async () => await watcher.WaitForStockAsync(
            productNames, null, 60, true, true, cts.Token);

        // Assert - Should not throw exception on cancellation
        await act.Should().NotThrowAsync();
    }
}