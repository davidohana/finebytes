using System.Text.Json;

using Mfr.Core;
using Mfr.Models;
using Mfr.Tests.TestSupport;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Tests loading and saving behavior for <see cref="PresetManager"/>.
    /// </summary>
    public class PresetManagerTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <summary>
        /// Disposes temporary test resources created for this test method.
        /// </summary>
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        [Fact]
        /// <summary>
        /// Verifies that loading presets populates the <see cref="PresetManager.NameToPreset"/> dictionary.
        /// </summary>
        public void LoadPresets_Populates_NameToPreset()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var presetsPath = Path.Combine(dir, "presets.json");
            _WritePresetsJson(presetsPath, /*lang=json,strict*/ """
                {
                  "presets": [
                    { "id": "5b5f7bbf-5fc4-45aa-9631-6ca18afae4f7", "name": "Rock", "filters": [] },
                    { "id": "43fdc61b-0a2b-4c8f-a8f4-77c550ea317a", "name": "Pop", "filters": [] }
                  ]
                }
                """);

            var manager = new PresetManager(presetsPath);
            manager.LoadPresets();

            Assert.Equal(2, manager.NameToPreset.Count);
            Assert.True(manager.NameToPreset.ContainsKey("Rock"));
            Assert.True(manager.NameToPreset.ContainsKey("Pop"));
        }

        [Fact]
        /// <summary>
        /// Verifies that loading allows names that differ only by letter case.
        /// </summary>
        public void LoadPresets_Allows_DifferentCase_Names()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var presetsPath = Path.Combine(dir, "presets.json");
            _WritePresetsJson(presetsPath, /*lang=json,strict*/ """
                {
                  "presets": [
                    { "id": "6d770366-7ac5-41f5-857f-08a4f6b7fdcc", "name": "Rock", "filters": [] },
                    { "id": "1a0d6772-996e-4334-8755-054434f53b16", "name": "rock", "filters": [] }
                  ]
                }
                """);

            var manager = new PresetManager(presetsPath);
            manager.LoadPresets();

            Assert.Equal(2, manager.NameToPreset.Count);
            Assert.True(manager.NameToPreset.ContainsKey("Rock"));
            Assert.True(manager.NameToPreset.ContainsKey("rock"));
        }

        [Fact]
        /// <summary>
        /// Verifies that loading rejects duplicate preset names with exact key equality.
        /// </summary>
        public void LoadPresets_Rejects_Exact_Duplicate_Names()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var presetsPath = Path.Combine(dir, "presets.json");
            _WritePresetsJson(presetsPath, /*lang=json,strict*/ """
                {
                  "presets": [
                    { "id": "8fd30889-4950-4c90-a5b3-81f5dd2ef825", "name": "Dup", "filters": [] },
                    { "id": "27dff4b4-4e0b-4bb3-8e4d-1656e5727d70", "name": "Dup", "filters": [] }
                  ]
                }
                """);

            var manager = new PresetManager(presetsPath);
            var ex = Assert.Throws<UserException>(manager.LoadPresets);
            Assert.Contains("Duplicate preset names found", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        /// <summary>
        /// Verifies that calling load again reloads and replaces the in-memory dictionary.
        /// </summary>
        public void LoadPresets_Reloads_From_Disk()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var presetsPath = Path.Combine(dir, "presets.json");
            _WritePresetsJson(presetsPath, /*lang=json,strict*/ """
                {
                  "presets": [
                    { "id": "95d14a63-cfdd-425a-b44e-c946f4fd2a78", "name": "First", "filters": [] }
                  ]
                }
                """);

            var manager = new PresetManager(presetsPath);
            manager.LoadPresets();
            Assert.True(manager.NameToPreset.ContainsKey("First"));

            _WritePresetsJson(presetsPath, /*lang=json,strict*/ """
                {
                  "presets": [
                    { "id": "47f0f380-d44a-4f4d-baa9-0331816cce9f", "name": "Second", "filters": [] }
                  ]
                }
                """);

            manager.LoadPresets();

            Assert.False(manager.NameToPreset.ContainsKey("First"));
            Assert.True(manager.NameToPreset.ContainsKey("Second"));
        }

        [Fact]
        /// <summary>
        /// Verifies that saving writes presets sorted by name.
        /// </summary>
        public void SavePresets_Writes_Sorted_By_Name()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var presetsPath = Path.Combine(dir, "presets.json");
            var manager = new PresetManager(presetsPath);
            manager.NameToPreset["z"] = _CreatePreset("z");
            manager.NameToPreset["A"] = _CreatePreset("A");
            manager.NameToPreset["a"] = _CreatePreset("a");

            manager.SavePresets();

            using var doc = JsonDocument.Parse(File.ReadAllText(presetsPath));
            var presetsProperty = doc.RootElement.EnumerateObject().First(p => string.Equals(p.Name, "presets", StringComparison.OrdinalIgnoreCase));
            var names = presetsProperty.Value
                .EnumerateArray()
                .Select(p =>
                {
                    var nameProperty = p.EnumerateObject().First(prop => string.Equals(prop.Name, "name", StringComparison.OrdinalIgnoreCase));
                    return nameProperty.Value.GetString()!;
                })
                .ToArray();

            Assert.Equal(["A", "a", "z"], names);
        }

        private static void _WritePresetsJson(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        private static FilterPreset _CreatePreset(string name)
        {
            return new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = null,
                Filters = []
            };
        }
    }
}
