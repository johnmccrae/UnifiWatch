using FluentAssertions;
using Moq;
using Moq.Protected;
using System.Net;
using UnifiWatch.Configuration;
using UnifiWatch.Models;
using UnifiWatch.Services;
using Xunit;

namespace UnifiWatch.Tests;

public class UnitTest1
{
    [Fact]
    public void UnifiProduct_ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var product = new UnifiProduct();

        // Assert
        product.Name.Should().BeEmpty();
        product.ShortName.Should().BeNull();
        product.Available.Should().BeFalse();
        product.Category.Should().BeEmpty();
        product.Collection.Should().BeNull();
        product.OrganizationalCollectionSlug.Should().BeNull();
        product.SKU.Should().BeEmpty();
        product.SKUName.Should().BeNull();
        product.EarlyAccess.Should().BeFalse();
        product.ProductUrl.Should().BeEmpty();
        product.Price.Should().BeNull();
        product.Created.Should().BeNull();
        product.Updated.Should().BeNull();
        product.Tags.Should().BeNull();
    }

    [Fact]
    public void UnifiProduct_ShouldAllowPropertyAssignment()
    {
        // Arrange
        var product = new UnifiProduct();
        var testDate = DateTime.Now;

        // Act
        product.Name = "Test Product";
        product.ShortName = "Test";
        product.Available = true;
        product.Category = "TestCategory";
        product.Collection = "test-collection";
        product.OrganizationalCollectionSlug = "org-test-collection";
        product.SKU = "TEST-001";
        product.SKUName = "Test SKU";
        product.EarlyAccess = true;
        product.ProductUrl = "https://example.com/product";
        product.Price = 9999;
        product.Created = testDate;
        product.Updated = testDate;
        product.Tags = new[] { "tag1", "tag2" };

        // Assert
        product.Name.Should().Be("Test Product");
        product.ShortName.Should().Be("Test");
        product.Available.Should().BeTrue();
        product.Category.Should().Be("TestCategory");
        product.Collection.Should().Be("test-collection");
        product.OrganizationalCollectionSlug.Should().Be("org-test-collection");
        product.SKU.Should().Be("TEST-001");
        product.SKUName.Should().Be("Test SKU");
        product.EarlyAccess.Should().BeTrue();
        product.ProductUrl.Should().Be("https://example.com/product");
        product.Price.Should().Be(9999);
        product.Created.Should().Be(testDate);
        product.Updated.Should().Be(testDate);
        product.Tags.Should().BeEquivalentTo(new[] { "tag1", "tag2" });
    }

    [Fact]
    public void StoreConfiguration_ModernStores_ShouldContainExpectedStores()
    {
        // Assert
        StoreConfiguration.ModernStores.Should().ContainKey("Europe");
        StoreConfiguration.ModernStores.Should().ContainKey("USA");
        StoreConfiguration.ModernStores.Should().ContainKey("UK");

        StoreConfiguration.ModernStores["Europe"].Should().Be("eu");
        StoreConfiguration.ModernStores["USA"].Should().Be("us");
        StoreConfiguration.ModernStores["UK"].Should().Be("uk");
    }

    [Fact]
    public void StoreConfiguration_ModernStoreLinks_ShouldContainExpectedLinks()
    {
        // Assert
        StoreConfiguration.ModernStoreLinks.Should().ContainKey("Europe");
        StoreConfiguration.ModernStoreLinks.Should().ContainKey("USA");
        StoreConfiguration.ModernStoreLinks.Should().ContainKey("UK");

        StoreConfiguration.ModernStoreLinks["Europe"].Should().Be("https://eu.store.ui.com/eu/en");
        StoreConfiguration.ModernStoreLinks["USA"].Should().Be("https://store.ui.com/us/en");
        StoreConfiguration.ModernStoreLinks["UK"].Should().Be("https://uk.store.ui.com/uk/en");
    }

    [Fact]
    public void StoreConfiguration_LegacyStores_ShouldContainExpectedStores()
    {
        // Assert
        StoreConfiguration.LegacyStores.Should().ContainKey("Brazil");
        StoreConfiguration.LegacyStores.Should().ContainKey("India");
        StoreConfiguration.LegacyStores.Should().ContainKey("Japan");
        StoreConfiguration.LegacyStores.Should().ContainKey("Taiwan");
        StoreConfiguration.LegacyStores.Should().ContainKey("Singapore");
        StoreConfiguration.LegacyStores.Should().ContainKey("Mexico");
        StoreConfiguration.LegacyStores.Should().ContainKey("China");

        StoreConfiguration.LegacyStores["Brazil"].Should().Be("https://br.store.ui.com");
        StoreConfiguration.LegacyStores["India"].Should().Be("https://store-ui.in");
        StoreConfiguration.LegacyStores["Japan"].Should().Be("https://jp.store.ui.com");
        StoreConfiguration.LegacyStores["Taiwan"].Should().Be("https://tw.store.ui.com");
        StoreConfiguration.LegacyStores["Singapore"].Should().Be("https://sg.store.ui.com");
        StoreConfiguration.LegacyStores["Mexico"].Should().Be("https://mx.store.ui.com");
        StoreConfiguration.LegacyStores["China"].Should().Be("https://store.ui.com.cn");
    }

    [Fact]
    public void StoreConfiguration_CollectionToCategory_ShouldContainMappings()
    {
        // Assert
        StoreConfiguration.CollectionToCategory.Should().NotBeEmpty();
        StoreConfiguration.CollectionToCategory.Should().ContainKey("unifi-accessory-tech-cable-box");
        StoreConfiguration.CollectionToCategory["unifi-accessory-tech-cable-box"].Should().Be("CableBox");
    }

    [Fact]
    public void StoreConfiguration_LegacyCollections_ShouldContainExpectedCollections()
    {
        // Assert
        StoreConfiguration.LegacyCollections.Should().ContainKey("Protect");
        StoreConfiguration.LegacyCollections.Should().ContainKey("NetworkOS");
        StoreConfiguration.LegacyCollections.Should().ContainKey("EarlyAccess");

        StoreConfiguration.LegacyCollections["Protect"].Should().Be("unifi-protect");
        StoreConfiguration.LegacyCollections["NetworkOS"].Should().Be("unifi-network-unifi-os-consoles");
        StoreConfiguration.LegacyCollections["EarlyAccess"].Should().Be("early-access");
    }
}
