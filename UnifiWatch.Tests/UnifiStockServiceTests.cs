using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using UnifiWatch.Configuration;
using UnifiWatch.Models;
using UnifiWatch.Services;
using Xunit;

namespace UnifiWatch.Tests;

public class unifiwatchServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly unifiwatchService _service;

    public unifiwatchServiceTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _service = new unifiwatchService(_httpClient);
    }

    [Fact]
    public async Task GetStockAsync_WithInvalidStore_ShouldThrowArgumentException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetStockAsync("InvalidStore"));

        exception.Message.Should().Contain("Store 'InvalidStore' is not supported");
    }

    [Fact]
    public async Task GetStockAsync_WithValidStore_ShouldReturnProducts()
    {
        // Arrange
        var store = "USA";
        var mockResponse = CreateMockGraphQLResponse();
        var responseContent = new StringContent(JsonSerializer.Serialize(mockResponse), Encoding.UTF8, "application/json");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });

        // Act
        var products = await _service.GetStockAsync(store);

        // Assert
        products.Should().NotBeNull();
        products.Should().HaveCountGreaterThan(0);
        products.First().Name.Should().Be("Test Product");
        products.First().Available.Should().BeTrue();
        products.First().SKU.Should().Be("TEST-001");
    }

    [Fact]
    public async Task GetStockAsync_WithCollectionsFilter_ShouldFilterProducts()
    {
        // Arrange
        var store = "USA";
        var collections = new[] { "WiFiFlagshipCompact" };
        var mockResponse = CreateMockGraphQLResponse();
        var responseContent = new StringContent(JsonSerializer.Serialize(mockResponse), Encoding.UTF8, "application/json");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });

        // Act
        var products = await _service.GetStockAsync(store, collections);

        // Assert
        products.Should().NotBeNull();
        products.Should().HaveCountGreaterThan(0);
        products.All(p => p.Category == "WiFiFlagshipCompact").Should().BeTrue();
    }

    [Fact]
    public async Task GetStockAsync_WithHttpError_ShouldThrowException()
    {
        // Arrange
        var store = "USA";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.GetStockAsync(store));
    }

    [Fact]
    public async Task GetStockAsync_WithInvalidJsonResponse_ShouldThrowException()
    {
        // Arrange
        var store = "USA";
        var responseContent = new StringContent("invalid json", Encoding.UTF8, "application/json");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = responseContent
            });

        // Act & Assert
        await Assert.ThrowsAsync<JsonException>(() =>
            _service.GetStockAsync(store));
    }

    private static object CreateMockGraphQLResponse()
    {
        return new
        {
            data = new
            {
                storefrontProducts = new
                {
                    pagination = new
                    {
                        total = 1,
                        limit = 250,
                        offset = 0
                    },
                    items = new[]
                    {
                        new
                        {
                            id = "test-id",
                            title = "Test Product",
                            shortTitle = "Test",
                            name = "Test Product",
                            slug = "test-product",
                            collectionSlug = "unifi-wifi-flagship-compact",
                            organizationalCollectionSlug = "unifi-wifi-flagship-compact",
                            tags = new[]
                            {
                                new { name = "tag1" },
                                new { name = "tag2" }
                            },
                            variants = new[]
                            {
                                new
                                {
                                    id = "variant-id",
                                    sku = "TEST-001",
                                    status = "AVAILABLE",
                                    title = "Test Variant",
                                    isEarlyAccess = false,
                                    displayPrice = new
                                    {
                                        amount = 9999,
                                        currency = "USD"
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}