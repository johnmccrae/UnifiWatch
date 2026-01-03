using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using UnifiStockTracker.Services.Credentials;
using Xunit;

namespace UnifiStockTracker.Tests;

public class CredentialProviderTests
{
    private readonly Mock<ILogger<EncryptedFileCredentialProvider>> _mockLogger;

    public CredentialProviderTests()
    {
        _mockLogger = new Mock<ILogger<EncryptedFileCredentialProvider>>();
    }

    [Fact]
    public async Task StoreAsync_WithValidKeyAndSecret_ShouldSucceed()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);
        var key = "test-key-" + Guid.NewGuid();
        var secret = "super-secret-value-123!@#";

        // Act
        var result = await provider.StoreAsync(key, secret, "Test Credential");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task StoreAsync_ThenRetrieveAsync_ShouldReturnSameValue()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);
        var key = "test-key-" + Guid.NewGuid();
        var secret = "super-secret-value-123!@#";

        // Act
        await provider.StoreAsync(key, secret, "Test Credential");
        var retrieved = await provider.RetrieveAsync(key);

        // Assert
        retrieved.Should().Be(secret);
    }

    [Fact]
    public async Task StoreAsync_WithEmptyKey_ShouldFail()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);

        // Act
        var result = await provider.StoreAsync("", "secret");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task StoreAsync_WithEmptySecret_ShouldFail()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);

        // Act
        var result = await provider.StoreAsync("key", "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RetrieveAsync_WithNonExistentKey_ShouldReturnNull()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);

        // Act
        var result = await provider.RetrieveAsync("non-existent-key-" + Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_AfterStore_ShouldReturnTrue()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);
        var key = "test-key-" + Guid.NewGuid();

        // Act
        await provider.StoreAsync(key, "secret");
        var exists = await provider.ExistsAsync(key);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistentKey_ShouldReturnFalse()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);

        // Act
        var exists = await provider.ExistsAsync("non-existent-" + Guid.NewGuid());

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_AfterStore_ShouldSucceed()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);
        var key = "test-key-" + Guid.NewGuid();
        await provider.StoreAsync(key, "secret");

        // Act
        var deleted = await provider.DeleteAsync(key);
        var exists = await provider.ExistsAsync(key);

        // Assert
        deleted.Should().BeTrue();
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentKey_ShouldReturnTrue()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);

        // Act
        var result = await provider.DeleteAsync("non-existent-" + Guid.NewGuid());

        // Assert
        // Returns true because key is already in desired state (not present)
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ListAsync_AfterMultipleStores_ShouldReturnAllKeys()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);
        var keys = new[] { "key1-" + Guid.NewGuid(), "key2-" + Guid.NewGuid(), "key3-" + Guid.NewGuid() };

        // Act
        foreach (var key in keys)
        {
            await provider.StoreAsync(key, "secret-" + key);
        }

        var listedKeys = await provider.ListAsync();

        // Assert
        foreach (var key in keys)
        {
            listedKeys.Should().Contain(key);
        }
    }

    [Fact]
    public async Task StoreAsync_WithLargeSecret_ShouldSucceed()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);
        var key = "test-key-" + Guid.NewGuid();
        var largeSecret = new string('x', 10000); // 10KB secret

        // Act
        var stored = await provider.StoreAsync(key, largeSecret);
        var retrieved = await provider.RetrieveAsync(key);

        // Assert
        stored.Should().BeTrue();
        retrieved.Should().Be(largeSecret);
    }

    [Fact]
    public async Task StoreAsync_WithSpecialCharacters_ShouldSucceed()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);
        var key = "test-key-" + Guid.NewGuid();
        var secretWithSpecialChars = "!@#$%^&*()_+-=[]{}|;:',.<>?/`~\n\r\t";

        // Act
        await provider.StoreAsync(key, secretWithSpecialChars);
        var retrieved = await provider.RetrieveAsync(key);

        // Assert
        retrieved.Should().Be(secretWithSpecialChars);
    }

    [Fact]
    public async Task StoreAsync_UpdateExistingKey_ShouldOverwrite()
    {
        // Arrange
        var provider = new EncryptedFileCredentialProvider(_mockLogger.Object);
        var key = "test-key-" + Guid.NewGuid();

        // Act
        await provider.StoreAsync(key, "original-secret");
        await provider.StoreAsync(key, "updated-secret");
        var retrieved = await provider.RetrieveAsync(key);

        // Assert
        retrieved.Should().Be("updated-secret");
    }

    [Fact]
    public void CredentialProviderFactory_CreateProvider_WithAutoStorage_ShouldSelectDefault()
    {
        // Arrange
        var mockLoggerFactory = new Mock<Microsoft.Extensions.Logging.ILoggerFactory>();
        
        // Setup generic CreateLogger<T> for all possible credential provider types
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<Microsoft.Extensions.Logging.ILogger>().Object);

        // Act
        var provider = CredentialProviderFactory.CreateProvider("auto", mockLoggerFactory.Object);

        // Assert
        provider.Should().NotBeNull();
        provider.StorageMethod.Should().NotBeEmpty();
    }

    [Fact]
    public void CredentialProviderFactory_CreateProvider_WithEnvironmentVariables_ShouldSucceed()
    {
        // Arrange
        var mockLoggerFactory = new Mock<Microsoft.Extensions.Logging.ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<Microsoft.Extensions.Logging.ILogger>().Object);

        // Act
        var provider = CredentialProviderFactory.CreateProvider("environment-variables", mockLoggerFactory.Object);

        // Assert
        provider.Should().NotBeNull();
        provider.StorageMethod.Should().Be("environment-variables");
    }

    [Fact]
    public void CredentialProviderFactory_CreateProvider_WithEncryptedFile_ShouldSucceed()
    {
        // Arrange
        var mockLoggerFactory = new Mock<Microsoft.Extensions.Logging.ILoggerFactory>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<Microsoft.Extensions.Logging.ILogger>().Object);

        // Act
        var provider = CredentialProviderFactory.CreateProvider("encrypted-file", mockLoggerFactory.Object);

        // Assert
        provider.Should().NotBeNull();
        provider.StorageMethod.Should().Be("encrypted-file");
    }

    [Fact]
    public void CredentialProviderFactory_CreateProvider_WithInvalidMethod_ShouldThrow()
    {
        // Arrange
        var mockLoggerFactory = new Mock<Microsoft.Extensions.Logging.ILoggerFactory>();

        // Act & Assert
        var action = () => CredentialProviderFactory.CreateProvider("invalid-method", mockLoggerFactory.Object);
        action.Should().Throw<NotSupportedException>();
    }
}
