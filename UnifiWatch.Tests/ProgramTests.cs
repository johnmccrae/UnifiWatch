using FluentAssertions;
using System.CommandLine;
using UnifiWatch;
using Xunit;

namespace UnifiWatch.Tests;

public class ProgramTests
{
    [Fact]
    public async Task Main_WithNoArguments_ShouldReturnError()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(1); // Error exit code
    }

    [Fact(Skip = "Integration test - requires real HTTP service")]
    public async Task Main_WithStockAndStore_ShouldSucceed()
    {
        // Arrange
        var args = new[] { "--stock", "--store", "USA" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0); // Success exit code
    }

    [Fact(Skip = "Integration test - requires real HTTP service")]
    public async Task Main_WithWaitAndStore_ShouldSucceed()
    {
        // Arrange
        var args = new[] { "--wait", "--store", "USA", "--product-names", "Test Product" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0); // Success exit code
    }

    [Fact]
    public async Task Main_WithBothStockAndWait_ShouldReturnError()
    {
        // Arrange
        var args = new[] { "--stock", "--wait", "--store", "USA" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(1); // Error exit code
    }

    [Fact]
    public async Task Main_WithNoStoreSpecified_ShouldReturnError()
    {
        // Arrange
        var args = new[] { "--stock" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(1); // Error exit code
    }

    [Fact]
    public async Task Main_WithBothStoreAndLegacyStore_ShouldReturnError()
    {
        // Arrange
        var args = new[] { "--stock", "--store", "USA", "--legacy-api-store", "Brazil" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(1); // Error exit code
    }

    [Fact(Skip = "Integration test - requires real HTTP service")]
    public async Task Main_WithLegacyStore_ShouldSucceed()
    {
        // Arrange
        var args = new[] { "--stock", "--legacy-api-store", "Brazil" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0); // Success exit code
    }

    [Fact(Skip = "Integration test - requires real HTTP service")]
    public async Task Main_WithCollectionsFilter_ShouldSucceed()
    {
        // Arrange
        var args = new[] { "--stock", "--store", "USA", "--collections", "Network", "Protect" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0); // Success exit code
    }

    [Fact(Skip = "Integration test - requires real HTTP service")]
    public async Task Main_WithProductNamesFilter_ShouldSucceed()
    {
        // Arrange
        var args = new[] { "--wait", "--store", "USA", "--product-names", "U6-LR", "U6-Pro" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0); // Success exit code
    }

    [Fact(Skip = "Integration test - requires real HTTP service")]
    public async Task Main_WithProductSKUsFilter_ShouldSucceed()
    {
        // Arrange
        var args = new[] { "--wait", "--store", "USA", "--product-skus", "U6-LR", "U6-Pro" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0); // Success exit code
    }

    [Fact(Skip = "Integration test - requires real HTTP service")]
    public async Task Main_WithCustomCheckInterval_ShouldSucceed()
    {
        // Arrange
        var args = new[] { "--wait", "--store", "USA", "--product-names", "Test", "--seconds", "30" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0); // Success exit code
    }

    [Fact(Skip = "Integration test - requires real HTTP service")]
    public async Task Main_WithNoWebsiteOption_ShouldSucceed()
    {
        // Arrange
        var args = new[] { "--wait", "--store", "USA", "--product-names", "Test", "--no-website" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0); // Success exit code
    }

    [Fact(Skip = "Integration test - requires real HTTP service")]
    public async Task Main_WithNoSoundOption_ShouldSucceed()
    {
        // Arrange
        var args = new[] { "--wait", "--store", "USA", "--product-names", "Test", "--no-sound" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(0); // Success exit code
    }

    [Fact]
    public async Task Main_WithInvalidStore_ShouldReturnError()
    {
        // Arrange
        var args = new[] { "--stock", "--store", "InvalidStore" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(1); // Error exit code due to invalid store
    }

    [Fact]
    public async Task Main_WithInvalidLegacyStore_ShouldReturnError()
    {
        // Arrange
        var args = new[] { "--stock", "--legacy-api-store", "InvalidStore" };

        // Act
        var result = await Program.Main(args);

        // Assert
        result.Should().Be(1); // Error exit code due to invalid store
    }
}