using System.Text.Json;
using Xunit;

namespace UnifiWatch.Tests
{
    public class LocalizationParityTests
    {
        private static readonly string[] Cultures = new[]
        {
            "en-CA",
            "fr-CA",
            "de-DE",
            "es-ES",
            "fr-FR",
            "it-IT",
            "pt-BR"
        };

        private static readonly string[] ResourceFiles = new[]
        {
            "CLI",
            "Errors",
            "Notifications"
        };

        [Fact]
        public void AllCultures_HaveCliResourceFiles()
        {
            foreach (var culture in Cultures)
            {
                var filePath = GetResourcePath($"CLI.{culture}.json");
                Assert.True(File.Exists(filePath), $"CLI resource file not found for culture: {culture}");
            }
        }

        [Fact]
        public void AllCultures_HaveErrorResourceFiles()
        {
            foreach (var culture in Cultures)
            {
                var filePath = GetResourcePath($"Errors.{culture}.json");
                Assert.True(File.Exists(filePath), $"Errors resource file not found for culture: {culture}");
            }
        }

        [Fact]
        public void AllCultures_HaveNotificationResourceFiles()
        {
            foreach (var culture in Cultures)
            {
                var filePath = GetResourcePath($"Notifications.{culture}.json");
                Assert.True(File.Exists(filePath), $"Notifications resource file not found for culture: {culture}");
            }
        }

        [Theory]
        [InlineData("CLI")]
        [InlineData("Errors")]
        [InlineData("Notifications")]
        public void AllResourceFiles_HaveValidJson(string resourceType)
        {
            foreach (var culture in Cultures)
            {
                var filePath = GetResourcePath($"{resourceType}.{culture}.json");
                var json = File.ReadAllText(filePath);

                var exception = Record.Exception(() =>
                {
                    using var doc = JsonDocument.Parse(json);
                });

                Assert.Null(exception);
            }
        }

        [Theory]
        [InlineData("CLI")]
        [InlineData("Errors")]
        [InlineData("Notifications")]
        public void AllResourceFiles_AreNotEmpty(string resourceType)
        {
            foreach (var culture in Cultures)
            {
                var filePath = GetResourcePath($"{resourceType}.{culture}.json");
                var json = File.ReadAllText(filePath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                Assert.True(root.ValueKind == JsonValueKind.Object);
                Assert.True(root.EnumerateObject().Any(), $"{resourceType} resource for {culture} contains no keys");
            }
        }

        [Theory]
        [InlineData("CLI")]
        [InlineData("Errors")]
        [InlineData("Notifications")]
        public void AllCultures_HaveSameKeysInResourceFile(string resourceType)
        {
            var masterKeys = GetKeysFromResource("en-CA", resourceType);

            foreach (var culture in Cultures.Skip(1))
            {
                var cultureKeys = GetKeysFromResource(culture, resourceType);
                var missingKeys = masterKeys.Except(cultureKeys).ToList();
                var extraKeys = cultureKeys.Except(masterKeys).ToList();

                Assert.True(missingKeys.Count == 0);
                Assert.True(extraKeys.Count == 0);
            }
        }

        [Fact]
        public void CliResources_ContainExpectedKeys()
        {
            var expectedKeys = new[]
            {
                "RootCommand.Description",
                "StockOption.Description",
                "WaitOption.Description",
                "StoreOption.Description",
                "LegacyApiStoreOption.Description",
                "CollectionsOption.Description",
                "ProductNamesOption.Description",
                "ProductSkusOption.Description",
                "SecondsOption.Description",
                "NoWebsiteOption.Description",
                "NoSoundOption.Description",
                "LanguageOption.Description",
                "Help.UsageHeader",
                "Help.OptionsHeader",
                "Help.ExamplesHeader",
                "Help.Example1",
                "Help.Example2",
                "List.FoundProducts",
                "List.Headers.Name",
                "List.Headers.Available",
                "List.Headers.Category",
                "List.Headers.SKU",
                "List.Headers.Price",
                "List.InStock",
                "List.OutOfStock",
                "List.PriceNA",
                "List.CategoryNA",
                "List.SKUNA",
                "Monitor.MonitoringFor",
                "Monitor.CheckingStock",
                "Monitor.CheckingStockDone",
                "Monitor.MonitoringCancelled",
                "Store.GettingProducts",
                "Store.RetrievedProducts",
                "ConfigWizard.Welcome",
                "ConfigWizard.SelectStore",
                "ConfigWizard.ProductFilters",
                "ConfigWizard.ProductNames",
                "ConfigWizard.ProductSkus",
                "ConfigWizard.CheckInterval",
                "ConfigWizard.Notifications",
                "ConfigWizard.EnableEmail",
                "ConfigWizard.EnableSms",
                "ConfigWizard.DedupeWindow",
                "ConfigWizard.SelectLanguage",
                "ConfigWizard.Saving",
                "ConfigWizard.Saved",
                "ConfigWizard.Summary",
                "ConfigWizard.Store",
                "ConfigWizard.Products",
                "ConfigWizard.CheckIntervalValue",
                "ConfigWizard.Language"
            };

            var actualKeys = GetKeysFromResource("en-CA", "CLI").ToList();
            foreach (var expectedKey in expectedKeys)
            {
                Assert.Contains(expectedKey, actualKeys);
            }
        }

        [Fact]
        public void ErrorResources_ContainExpectedKeys()
        {
            var expectedKeys = new[]
            {
                "NotificationFailed",
                "ConfigNotFound",
                "InvalidStore",
                "CredentialStoreFailed",
                "CredentialLoadFailed",
                "CredentialDeleteFailed",
                "InvalidConfiguration",
                "ConfigurationLoadFailed",
                "ConfigurationSaveFailed",
                "ConfigurationValidationFailed",
                "InvalidCheckInterval",
                "NoProductsSpecified",
                "InvalidEmailConfiguration",
                "InvalidSmsConfiguration",
                "EmailSendFailed",
                "SmsSendFailed",
                "DesktopNotificationFailed",
                "CorruptCredentialsFile",
                "KeychainAccessDenied",
                "SecretServiceUnavailable",
                "PlatformNotSupported"
            };

            var actualKeys = GetKeysFromResource("en-CA", "Errors").ToList();
            foreach (var expectedKey in expectedKeys)
            {
                Assert.Contains(expectedKey, actualKeys);
            }
        }

        [Fact]
        public void NotificationResources_ContainExpectedKeys()
        {
            var expectedKeys = new[]
            {
                "ProductInStock.Title",
                "ProductInStock.Message",
                "ProductInStock.MessageWithPrice",
                "TestNotification.Title",
                "TestNotification.Message",
                "Email.Subject",
                "Email.Body.Header",
                "Email.Body.ProductName",
                "Email.Body.SKU",
                "Email.Body.Price",
                "Email.Body.Store",
                "Email.Body.ViewProduct",
                "Email.Body.Footer",
                "Sms.ProductInStock",
                "Sms.ProductInStockWithPrice",
                "Desktop.ClickToView",
                "Desktop.MultipleProducts",
                "NotificationSent",
                "NotificationSkipped"
            };

            var actualKeys = GetKeysFromResource("en-CA", "Notifications").ToList();
            foreach (var expectedKey in expectedKeys)
            {
                Assert.Contains(expectedKey, actualKeys);
            }
        }

        [Theory]
        [InlineData("CLI")]
        [InlineData("Errors")]
        [InlineData("Notifications")]
        public void NoResourceFile_ContainsDuplicateKeys(string resourceType)
        {
            foreach (var culture in Cultures)
            {
                var filePath = GetResourcePath($"{resourceType}.{culture}.json");
                var json = File.ReadAllText(filePath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var keys = new List<string>();
                foreach (var property in root.EnumerateObject())
                {
                    keys.Add(property.Name);
                }

                var duplicates = keys.GroupBy(x => x)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                Assert.True(duplicates.Count == 0);
            }
        }

        [Theory]
        [InlineData("CLI")]
        [InlineData("Errors")]
        [InlineData("Notifications")]
        public void AllResourceValues_AreNonEmpty(string resourceType)
        {
            foreach (var culture in Cultures)
            {
                var filePath = GetResourcePath($"{resourceType}.{culture}.json");
                var json = File.ReadAllText(filePath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var emptyValues = new List<string>();
                foreach (var property in root.EnumerateObject())
                {
                    if (string.IsNullOrWhiteSpace(property.Value.GetString()))
                    {
                        emptyValues.Add(property.Name);
                    }
                }

                Assert.True(emptyValues.Count == 0);
            }
        }

        [Fact]
        public void ResourceFiles_AreConsistentlyFormatted()
        {
            foreach (var resourceType in ResourceFiles)
            {
                var enCaJson = File.ReadAllText(GetResourcePath($"{resourceType}.en-CA.json"));
                using var doc = JsonDocument.Parse(enCaJson);
                var root = doc.RootElement;
                var propertyCount = root.EnumerateObject().Count();
                Assert.True(propertyCount > 0, $"{resourceType} should contain properties");
            }
        }

        private static string GetResourcePath(string filename)
        {
            var baseDir = AppContext.BaseDirectory; // .../UnifiWatch.Tests/bin/Debug/net9.0/
            var projectRoot = Directory.GetParent(baseDir) // net9.0
                ?.Parent // Debug
                ?.Parent // bin
                ?.Parent // UnifiWatch.Tests
                ?.Parent // UnifiWatch (solution root)
                ?.FullName ?? baseDir;

            return Path.Combine(projectRoot, "Resources", filename);
        }

        private static IEnumerable<string> GetKeysFromResource(string culture, string resourceType)
        {
            var filePath = GetResourcePath($"{resourceType}.{culture}.json");
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return root.EnumerateObject().Select(p => p.Name).ToList();
        }
    }
}
