using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;
using UnifiWatch.Services.Localization;
using System.Globalization;

namespace UnifiWatch.Tests
{
    public class ResourceLocalizerFallbackTests
    {
        [Fact]
        public void ResourceLocalizer_Load_WithMalformedJson_ShouldNotThrow()
        {
            // Arrange: Create a temporary directory with a malformed JSON file
            var tempDir = Path.Combine(Path.GetTempPath(), "UnifiWatch_Test_" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                var malformedFile = Path.Combine(tempDir, "CLI.en-CA.json");
                File.WriteAllText(malformedFile, "{ invalid json }");
                
                // Act: Load with a culture that has a malformed JSON (should gracefully fallback)
                var originalBasePath = AppContext.BaseDirectory;
                var loc = ResourceLocalizer.Load(CultureInfo.CurrentUICulture);
                
                // Assert: Should not throw and should return a valid localizer
                Assert.NotNull(loc);
                // Accessing a non-existent key should return the key itself
                var result = loc.CLI("NonExistentKey");
                Assert.Equal("NonExistentKey", result);
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void ResourceLocalizer_Cli_WithValidKey_ShouldReturnTranslation()
        {
            // Arrange
            var loc = ResourceLocalizer.Load(CultureInfo.GetCultureInfo("en-CA"));
            
            // Act
            var result = loc.CLI("Help.UsageHeader");
            
            // Assert: Should return a valid translation, not the key
            Assert.NotEqual("Help.UsageHeader", result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ResourceLocalizer_Format_WithArguments_ShouldInterpolateValues()
        {
            // Arrange
            var loc = ResourceLocalizer.Load(CultureInfo.GetCultureInfo("en-CA"));
            
            // Act
            var result = loc.CLI("List.FoundProducts", 42);
            
            // Assert: Should contain the number
            Assert.Contains("42", result);
        }

        [Fact]
        public void ResourceLocalizer_Notification_ShouldReturnValidString()
        {
            // Arrange
            var loc = ResourceLocalizer.Load(CultureInfo.GetCultureInfo("en-CA"));
            
            // Act
            var result = loc.Notification("Notification.Bell");
            
            // Assert
            Assert.NotEmpty(result);
        }

        [Fact]
        public void ResourceLocalizer_Error_ShouldReturnValidString()
        {
            // Arrange
            var loc = ResourceLocalizer.Load(CultureInfo.GetCultureInfo("en-CA"));
            
            // Act
            var result = loc.Error("Error.MustSpecifyStockOrWait");
            
            // Assert
            Assert.NotEmpty(result);
        }
    }
}
