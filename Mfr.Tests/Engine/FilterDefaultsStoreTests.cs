using System.Text.Json;
using System.Text.Json.Nodes;
using Mfr.Filters.Case;
using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests load/save and clone behavior for <see cref="FilterDefaultsStore"/> via <see cref="ConfigStore"/>.
    /// </summary>
    [Collection(ConfigStoreCollection.Name)]
    public sealed class FilterDefaultsStoreTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();
        private readonly string _configPath;

        /// <summary>
        /// Creates a temp <c>config.json</c> and loads it into <see cref="ConfigStore"/>.
        /// </summary>
        public FilterDefaultsStoreTests()
        {
            _configPath = _tempDirectoryFixture.CreateTempDir().CombinePath("config.json");
            File.WriteAllText(_configPath, """{}""");
            ConfigStore.Load(_configPath);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            ConfigStoreTestReset.LoadEmpty();
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies SetDefault + TryLoad round-trips options and Apply To.
        /// </summary>
        [Fact]
        public void SetDefault_round_trips_filter_options()
        {
            var store = FilterDefaultsStore.CreateEmpty();
            var saved = new LettersCaseFilter(
                new FileExtensionTarget(),
                new LettersCaseOptions(LettersCaseMode.UpperCase, [])
            );

            store.SetDefault(saved);

            var reloaded = FilterDefaultsStore.OpenDefault();
            Assert.True(reloaded.TryGetDefault("LettersCase", out var loaded));
            var typed = Assert.IsType<LettersCaseFilter>(loaded);
            Assert.IsType<FileExtensionTarget>(typed.Target);
            Assert.Equal(LettersCaseMode.UpperCase, typed.Options.Mode);
            Assert.NotSame(saved, loaded);
        }

        /// <summary>
        /// Verifies a second SetDefault for the same type overwrites the previous snapshot.
        /// </summary>
        [Fact]
        public void SetDefault_overwrites_same_type()
        {
            var store = FilterDefaultsStore.CreateEmpty();
            store.SetDefault(
                new LettersCaseFilter(new FilePrefixTarget(), new LettersCaseOptions(LettersCaseMode.LowerCase, []))
            );
            var second = new LettersCaseFilter(
                new FileExtensionTarget(),
                new LettersCaseOptions(LettersCaseMode.UpperCase, [])
            );
            store.SetDefault(second);

            Assert.True(store.TryGetDefault("LettersCase", out var loaded));
            var typed = Assert.IsType<LettersCaseFilter>(loaded);
            Assert.IsType<FileExtensionTarget>(typed.Target);
            Assert.Equal(LettersCaseMode.UpperCase, typed.Options.Mode);
        }

        /// <summary>
        /// Verifies TryGetDefault returns a clone so mutating the result does not affect the cache.
        /// </summary>
        [Fact]
        public void TryGetDefault_returns_clone()
        {
            var store = FilterDefaultsStore.CreateEmpty();
            store.SetDefault(new LettersCaseFilter());

            Assert.True(store.TryGetDefault("LettersCase", out var first));
            Assert.True(store.TryGetDefault("LettersCase", out var second));
            Assert.NotSame(first, second);
            Assert.Equal(((LettersCaseFilter)first).Options.Mode, ((LettersCaseFilter)second).Options.Mode);
        }

        /// <summary>
        /// Verifies unknown type discriminators and bad payloads are skipped on load.
        /// </summary>
        [Fact]
        public void TryLoad_skips_unknown_and_invalid_entries()
        {
            ConfigStore.FilterDefaultsJson =
                JsonNode.Parse(
                    /*lang=json,strict*/
                    """
                    {
                      "NotARealFilter": { "type": "NotARealFilter", "options": {} },
                      "LettersCase": { "type": "LettersCase", "target": { "kind": "broken" } },
                      "ShrinkSpaces": { "type": "ShrinkSpaces" }
                    }
                    """
                ) as JsonObject
                ?? [];

            var store = FilterDefaultsStore.CreateEmpty();
            store.TryLoad();

            Assert.False(store.TryGetDefault("NotARealFilter", out _));
            Assert.False(store.TryGetDefault("LettersCase", out _));
            Assert.True(store.TryGetDefault("ShrinkSpaces", out var shrink));
            Assert.Equal("ShrinkSpaces", shrink.Type);
        }

        /// <summary>
        /// Verifies an empty map leaves the store empty.
        /// </summary>
        [Fact]
        public void TryLoad_empty_map_is_empty()
        {
            ConfigStore.FilterDefaultsJson = [];
            var store = FilterDefaultsStore.CreateEmpty();
            store.TryLoad();
            Assert.False(store.TryGetDefault("LettersCase", out _));
        }

        /// <summary>
        /// Verifies Clear empties the cache without requiring a file delete API.
        /// </summary>
        [Fact]
        public void Clear_empties_cache()
        {
            var store = FilterDefaultsStore.CreateEmpty();
            store.SetDefault(new LettersCaseFilter());
            Assert.True(store.TryGetDefault("LettersCase", out _));

            store.Clear();

            Assert.False(store.TryGetDefault("LettersCase", out _));
        }

        /// <summary>
        /// Verifies Save writes a flat type map under <c>filterDefaults</c> (not nested under <c>defaults</c>).
        /// </summary>
        [Fact]
        public void Save_writes_filterDefaults_object_keyed_by_type()
        {
            var store = FilterDefaultsStore.CreateEmpty();
            store.SetDefault(new LettersCaseFilter());

            using var doc = JsonDocument.Parse(File.ReadAllText(_configPath));
            Assert.True(doc.RootElement.TryGetProperty("filterDefaults", out var defaults));
            Assert.True(defaults.TryGetProperty("LettersCase", out var entry));
            Assert.Equal("LettersCase", entry.GetProperty("type").GetString());
            Assert.False(defaults.TryGetProperty("defaults", out _));
        }
    }
}
