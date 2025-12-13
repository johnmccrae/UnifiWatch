using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace UnifiWatch.Tests
{
    public class LocalizationParityTests
    {
        private const string BaselineCulture = "en-CA";

        [Fact]
        public void ResourceFiles_HaveMatchingKeysPerCategory()
        {
            var resourcesDir = Path.Combine(AppContext.BaseDirectory, "Resources");
            Assert.True(Directory.Exists(resourcesDir), $"Resources directory not found at {resourcesDir}");

            var files = Directory.GetFiles(resourcesDir, "*.json");
            Assert.NotEmpty(files);

            var grouped = files
                .Select(file =>
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var parts = name.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    Assert.True(parts.Length >= 2, $"Unexpected resource filename format: {name}");
                    return new { File = file, Category = parts[0], Culture = parts[1] };
                })
                .GroupBy(x => x.Category);

            foreach (var group in grouped)
            {
                var baseline = group.FirstOrDefault(x => string.Equals(x.Culture, BaselineCulture, StringComparison.OrdinalIgnoreCase));
                Assert.NotNull(baseline);

                var baselineKeys = LoadKeys(baseline!.File);
                Assert.NotEmpty(baselineKeys);

                foreach (var entry in group)
                {
                    var keys = LoadKeys(entry.File);
                    Assert.True(baselineKeys.SetEquals(keys),
                        $"Resource key mismatch in {Path.GetFileName(entry.File)} for category {group.Key}");
                }
            }
        }

        private static HashSet<string> LoadKeys(string file)
        {
            var json = File.ReadAllText(file);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            Assert.NotNull(dict);
            return dict!.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
