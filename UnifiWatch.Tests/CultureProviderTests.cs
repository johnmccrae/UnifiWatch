using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using UnifiWatch.Configuration;
using ConfigProvider = UnifiWatch.Configuration.IConfigurationProvider;
using ServiceConfig = UnifiWatch.Configuration.ServiceConfiguration;
using ServiceSettings = UnifiWatch.Configuration.ServiceSettings;
using UnifiWatch.Services.Localization;
using Microsoft.Extensions.Logging;

namespace UnifiWatch.Tests
{
    public class CultureProviderTests
    {
        [Fact]
        public async Task GetUserCultureAsync_WithConfigLanguage_ShouldReturnSpecifiedCulture()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ConfigurationProvider>>();
            var mockConfigProvider = new Mock<ConfigProvider>();
            var config = new ServiceConfig
            {
                Service = new ServiceSettings
                {
                    Language = "fr-CA"
                }
            };
            mockConfigProvider.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(config);
            
            var provider = new CultureProvider(mockConfigProvider.Object);
            
            // Act
            var result = await provider.GetUserCultureAsync(CancellationToken.None);
            
            // Assert
            result.Name.Should().Be("fr-CA");
        }

        [Fact]
        public async Task GetUserCultureAsync_WithAutoLanguage_ShouldReturnSystemCulture()
        {
            // Arrange
            var mockConfigProvider = new Mock<ConfigProvider>();
            var config = new ServiceConfig
            {
                Service = new ServiceSettings
                {
                    Language = "auto"
                }
            };
            mockConfigProvider.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(config);
            
            var provider = new CultureProvider(mockConfigProvider.Object);
            
            // Act
            var result = await provider.GetUserCultureAsync(CancellationToken.None);
            
            // Assert
            result.Should().NotBeNull();
            // Should be system culture or fallback
            (result.Name == CultureInfo.CurrentUICulture.Name || result.Name == "en-CA").Should().BeTrue();
        }

        [Fact]
        public async Task GetUserCultureAsync_WithNullLanguage_ShouldReturnSystemCulture()
        {
            // Arrange
            var mockConfigProvider = new Mock<ConfigProvider>();
            var config = new ServiceConfig
            {
                Service = new ServiceSettings
                {
                    Language = null
                }
            };
            mockConfigProvider.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(config);
            
            var provider = new CultureProvider(mockConfigProvider.Object);
            
            // Act
            var result = await provider.GetUserCultureAsync(CancellationToken.None);
            
            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetUserCultureAsync_WithInvalidLanguage_ShouldFallbackToSystemCulture()
        {
            // Arrange
            var mockConfigProvider = new Mock<ConfigProvider>();
            var config = new ServiceConfig
            {
                Service = new ServiceSettings
                {
                    Language = "invalid-XX"
                }
            };
            mockConfigProvider.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(config);
            
            var provider = new CultureProvider(mockConfigProvider.Object);
            
            // Act
            var result = await provider.GetUserCultureAsync(CancellationToken.None);
            
            // Assert
            result.Should().NotBeNull();
            // Should fallback gracefully
        }

        [Fact]
        public async Task GetUserCultureAsync_WithConfigLoadFailure_ShouldFallbackToSystemCulture()
        {
            // Arrange
            var mockConfigProvider = new Mock<ConfigProvider>();
            mockConfigProvider.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Config load failed"));
            
            var provider = new CultureProvider(mockConfigProvider.Object);
            
            // Act
            var result = await provider.GetUserCultureAsync(CancellationToken.None);
            
            // Assert
            result.Should().NotBeNull();
            // Should return system culture or fallback (en-CA)
            (result.Name == CultureInfo.CurrentUICulture.Name || result.Name == "en-CA").Should().BeTrue();
        }

        [Theory]
        [InlineData("de-DE", "de-DE")]
        [InlineData("es-ES", "es-ES")]
        [InlineData("it-IT", "it-IT")]
        [InlineData("fr-FR", "fr-FR")]
        public async Task GetUserCultureAsync_WithVariousCultures_ShouldReturnCorrectCulture(string configLang, string expectedCulture)
        {
            // Arrange
            var mockConfigProvider = new Mock<ConfigProvider>();
                var config = new ServiceConfig
            {
                Service = new ServiceSettings
                {
                    Language = configLang
                }
            };
            mockConfigProvider.Setup(x => x.LoadAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(config);
            
            var provider = new CultureProvider(mockConfigProvider.Object);
            
            // Act
            var result = await provider.GetUserCultureAsync(CancellationToken.None);
            
            // Assert
            result.Name.Should().Be(expectedCulture);
        }
    }
}
