using System.Text.Json;
using Mfr.Utils;

namespace Mfr.Tests.Engine
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
            var presetsPath = dir.CombinePath("presets.json");
            _WritePresetsJson(
                presetsPath, /*lang=json,strict*/
                """
                {
                  "presets": [
                    { "id": "5b5f7bbf-5fc4-45aa-9631-6ca18afae4f7", "name": "Rock", "chain": { "steps": [] } },
                    { "id": "43fdc61b-0a2b-4c8f-a8f4-77c550ea317a", "name": "Pop", "chain": { "steps": [] } }
                  ]
                }
                """
            );

            var manager = new PresetManager(presetsPath);
            manager.LoadPresets();

            Assert.Equal(2, manager.NameToPreset.Count);
            Assert.True(manager.NameToPreset.ContainsKey("Rock"));
            Assert.True(manager.NameToPreset.ContainsKey("Pop"));
            Assert.Equal(["Rock", "Pop"], manager.Presets.Select(preset => preset.Name));
        }

        [Fact]
        /// <summary>
        /// Verifies that loading allows names that differ only by letter case.
        /// </summary>
        public void LoadPresets_Allows_DifferentCase_Names()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var presetsPath = dir.CombinePath("presets.json");
            _WritePresetsJson(
                presetsPath, /*lang=json,strict*/
                """
                {
                  "presets": [
                    { "id": "6d770366-7ac5-41f5-857f-08a4f6b7fdcc", "name": "Rock", "chain": { "steps": [] } },
                    { "id": "1a0d6772-996e-4334-8755-054434f53b16", "name": "rock", "chain": { "steps": [] } }
                  ]
                }
                """
            );

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
            var presetsPath = dir.CombinePath("presets.json");
            _WritePresetsJson(
                presetsPath, /*lang=json,strict*/
                """
                {
                  "presets": [
                    { "id": "8fd30889-4950-4c90-a5b3-81f5dd2ef825", "name": "Dup", "chain": { "steps": [] } },
                    { "id": "27dff4b4-4e0b-4bb3-8e4d-1656e5727d70", "name": "Dup", "chain": { "steps": [] } }
                  ]
                }
                """
            );

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
            var presetsPath = dir.CombinePath("presets.json");
            _WritePresetsJson(
                presetsPath, /*lang=json,strict*/
                """
                {
                  "presets": [
                    { "id": "95d14a63-cfdd-425a-b44e-c946f4fd2a78", "name": "First", "chain": { "steps": [] } }
                  ]
                }
                """
            );

            var manager = new PresetManager(presetsPath);
            manager.LoadPresets();
            Assert.True(manager.NameToPreset.ContainsKey("First"));

            _WritePresetsJson(
                presetsPath, /*lang=json,strict*/
                """
                {
                  "presets": [
                    { "id": "47f0f380-d44a-4f4d-baa9-0331816cce9f", "name": "Second", "chain": { "steps": [] } }
                  ]
                }
                """
            );

            manager.LoadPresets();

            Assert.False(manager.NameToPreset.ContainsKey("First"));
            Assert.True(manager.NameToPreset.ContainsKey("Second"));
            Assert.Equal(["Second"], manager.Presets.Select(preset => preset.Name));
        }

        [Fact]
        /// <summary>
        /// Verifies that saving writes presets in stored list order (not sorted by name).
        /// </summary>
        public void SavePresets_Writes_Stored_Order()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var presetsPath = dir.CombinePath("presets.json");
            var manager = new PresetManager(presetsPath);
            manager.Upsert(_CreatePreset("z"));
            manager.Upsert(_CreatePreset("A"));
            manager.Upsert(_CreatePreset("a"));

            manager.SavePresets();

            Assert.Equal(["z", "A", "a"], _ReadPresetNames(presetsPath));
        }

        [Fact]
        /// <summary>
        /// Verifies load preserves JSON array order through save round-trip.
        /// </summary>
        public void LoadPresets_Preserves_Array_Order_RoundTrip()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var presetsPath = dir.CombinePath("presets.json");
            _WritePresetsJson(
                presetsPath, /*lang=json,strict*/
                """
                {
                  "presets": [
                    { "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "name": "Zebra", "chain": { "steps": [] } },
                    { "id": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", "name": "Alpha", "chain": { "steps": [] } },
                    { "id": "cccccccc-cccc-cccc-cccc-cccccccccccc", "name": "Middle", "chain": { "steps": [] } }
                  ]
                }
                """
            );

            var manager = new PresetManager(presetsPath);
            manager.LoadPresets();
            Assert.Equal(["Zebra", "Alpha", "Middle"], manager.Presets.Select(preset => preset.Name));

            manager.SavePresets();
            Assert.Equal(["Zebra", "Alpha", "Middle"], _ReadPresetNames(presetsPath));
        }

        [Fact]
        /// <summary>
        /// Verifies Upsert appends new names and replaces existing names in place.
        /// </summary>
        public void Upsert_Appends_Or_Replaces_In_Place()
        {
            var manager = PresetManager.CreateEmpty();
            var first = _CreatePreset("First");
            var second = _CreatePreset("Second");
            manager.Upsert(first);
            manager.Upsert(second);

            var replaced = first with { Description = "updated" };
            manager.Upsert(replaced);
            manager.Upsert(_CreatePreset("Third"));

            Assert.Equal(["First", "Second", "Third"], manager.Presets.Select(preset => preset.Name));
            Assert.Equal("updated", manager.NameToPreset["First"].Description);
            Assert.Same(replaced, manager.Presets[0]);
        }

        [Fact]
        /// <summary>
        /// Verifies Remove drops list and lookup entries and keeps remaining order.
        /// </summary>
        public void Remove_Drops_Preset_And_Keeps_Order()
        {
            var manager = PresetManager.CreateEmpty();
            manager.Upsert(_CreatePreset("A"));
            manager.Upsert(_CreatePreset("B"));
            manager.Upsert(_CreatePreset("C"));

            Assert.True(manager.Remove("B"));
            Assert.False(manager.Remove("B"));
            Assert.Equal(["A", "C"], manager.Presets.Select(preset => preset.Name));
            Assert.False(manager.NameToPreset.ContainsKey("B"));
        }

        [Fact]
        /// <summary>
        /// Verifies TryRename keeps list index and updates the lookup key.
        /// </summary>
        public void TryRename_Keeps_Index_And_Updates_Lookup()
        {
            var manager = PresetManager.CreateEmpty();
            manager.Upsert(_CreatePreset("A"));
            var middle = _CreatePreset("B");
            manager.Upsert(middle);
            manager.Upsert(_CreatePreset("C"));

            var renamed = middle with { Name = "B2", Description = "renamed" };
            Assert.True(manager.TryRename("B", renamed));

            Assert.Equal(["A", "B2", "C"], manager.Presets.Select(preset => preset.Name));
            Assert.Same(renamed, manager.Presets[1]);
            Assert.False(manager.NameToPreset.ContainsKey("B"));
            Assert.Same(renamed, manager.NameToPreset["B2"]);
        }

        [Fact]
        /// <summary>
        /// Verifies TryMoveIndicesTo reorders the stored list.
        /// </summary>
        public void TryMoveIndicesTo_Reorders_Presets()
        {
            var manager = PresetManager.CreateEmpty();
            manager.Upsert(_CreatePreset("A"));
            manager.Upsert(_CreatePreset("B"));
            manager.Upsert(_CreatePreset("C"));

            Assert.True(manager.TryMoveIndicesTo([0], targetIndex: 2, out var newIndices));
            Assert.Equal([1], newIndices);
            Assert.Equal(["B", "A", "C"], manager.Presets.Select(preset => preset.Name));
        }

        [Fact]
        /// <summary>
        /// Verifies neighbor moves slide a selected block.
        /// </summary>
        public void TryMoveSelectedTowardNeighbor_Moves_Block()
        {
            var manager = PresetManager.CreateEmpty();
            manager.Upsert(_CreatePreset("A"));
            manager.Upsert(_CreatePreset("B"));
            manager.Upsert(_CreatePreset("C"));

            Assert.True(manager.TryMoveSelectedTowardNeighbor(["A", "B"], offset: 1));
            Assert.Equal(["C", "A", "B"], manager.Presets.Select(preset => preset.Name));
        }

        [Fact]
        /// <summary>
        /// Verifies CanMoveSelectedTowardNeighbor matches whether a neighbor swap is possible.
        /// </summary>
        public void CanMoveSelectedTowardNeighbor_Matches_Edge()
        {
            var manager = PresetManager.CreateEmpty();
            manager.Upsert(_CreatePreset("A"));
            manager.Upsert(_CreatePreset("B"));
            manager.Upsert(_CreatePreset("C"));

            Assert.False(manager.CanMoveSelectedTowardNeighbor(["A"], offset: -1));
            Assert.True(manager.CanMoveSelectedTowardNeighbor(["A"], offset: 1));
            Assert.False(manager.CanMoveSelectedTowardNeighbor(["A", "B", "C"], offset: 1));
            Assert.False(manager.CanMoveSelectedTowardNeighbor([], offset: 1));
        }

        [Fact]
        /// <summary>
        /// Verifies TryRename refuses a name already used by another preset.
        /// </summary>
        public void TryRename_Throws_When_Name_Taken()
        {
            var manager = PresetManager.CreateEmpty();
            manager.Upsert(_CreatePreset("A"));
            manager.Upsert(_CreatePreset("B"));

            var renamed = manager.NameToPreset["A"] with { Name = "B" };
            Assert.Throws<InvalidOperationException>(() => manager.TryRename("A", renamed));
            Assert.True(manager.NameToPreset.ContainsKey("A"));
            Assert.True(manager.NameToPreset.ContainsKey("B"));
        }

        [Fact]
        /// <summary>
        /// Verifies <see cref="PresetManager.OpenOrCreate"/> writes an empty file when missing, then loads.
        /// </summary>
        public void OpenOrCreate_Creates_Empty_File_When_Missing()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var presetsPath = dir.CombinePath("presets.json");
            Assert.False(File.Exists(presetsPath));

            var manager = PresetManager.OpenOrCreate(presetsPath);

            Assert.True(File.Exists(presetsPath));
            Assert.Empty(manager.NameToPreset);
            Assert.Empty(manager.Presets);
            using var doc = JsonDocument.Parse(File.ReadAllText(presetsPath));
            Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("presets").ValueKind);
            Assert.Empty(doc.RootElement.GetProperty("presets").EnumerateArray());
        }

        [Fact]
        /// <summary>
        /// Verifies <see cref="PresetManager.OpenOrCreate"/> still hard-fails on a corrupt presets file.
        /// </summary>
        public void OpenOrCreate_Corrupt_File_Throws_UserException()
        {
            var dir = _tempDirectoryFixture.CreateTempDir();
            var presetsPath = dir.CombinePath("presets.json");
            File.WriteAllText(presetsPath, "{ not valid presets json");

            var ex = Assert.Throws<UserException>(() => PresetManager.OpenOrCreate(presetsPath));
            Assert.Contains("Failed to read presets file", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        /// <summary>
        /// Verifies <see cref="PresetManager.CreateEmpty"/> starts with no presets and does not require a file.
        /// </summary>
        public void CreateEmpty_Has_No_Presets()
        {
            var manager = PresetManager.CreateEmpty();
            Assert.Empty(manager.NameToPreset);
            Assert.Empty(manager.Presets);
            Assert.False(File.Exists(manager.PresetsFilePath));
        }

        private static void _WritePresetsJson(string path, string content)
        {
            File.WriteAllText(path, content);
        }

        private static string[] _ReadPresetNames(string presetsPath)
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(presetsPath));
            var presetsProperty = doc
                .RootElement.EnumerateObject()
                .First(p => string.Equals(p.Name, "presets", StringComparison.OrdinalIgnoreCase));
            return
            [
                .. presetsProperty
                    .Value.EnumerateArray()
                    .Select(p =>
                    {
                        var nameProperty = p.EnumerateObject()
                            .First(prop => string.Equals(prop.Name, "name", StringComparison.OrdinalIgnoreCase));
                        return nameProperty.Value.GetString()!;
                    }),
            ];
        }

        private static FilterPreset _CreatePreset(string name)
        {
            return new FilterPreset
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = null,
                Chain = new FilterChain { Steps = [] },
            };
        }
    }
}
