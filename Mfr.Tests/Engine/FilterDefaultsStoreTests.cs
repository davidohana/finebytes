using System.Text.Json;
using Mfr.Filters.Case;
using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests load/save and clone behavior for <see cref="FilterDefaultsStore"/>.
    /// </summary>
    public sealed class FilterDefaultsStoreTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies SetDefault + TryLoad round-trips options and Apply To.
        /// </summary>
        [Fact]
        public void SetDefault_round_trips_filter_options()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("filter-defaults.json");
            var store = new FilterDefaultsStore(path);
            var saved = new LettersCaseFilter(
                new FileExtensionTarget(),
                new LettersCaseOptions(LettersCaseMode.UpperCase, [])
            );

            store.SetDefault(saved);

            var reloaded = new FilterDefaultsStore(path);
            reloaded.TryLoad();
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
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("filter-defaults.json");
            var store = new FilterDefaultsStore(path);
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
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("filter-defaults.json");
            var store = new FilterDefaultsStore(path);
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
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("filter-defaults.json");
            File.WriteAllText(
                path, /*lang=json,strict*/
                """
                {
                  "defaults": {
                    "NotARealFilter": { "type": "NotARealFilter", "options": {} },
                    "LettersCase": { "type": "LettersCase", "target": { "kind": "broken" } },
                    "ShrinkSpaces": { "type": "ShrinkSpaces" }
                  }
                }
                """
            );

            var store = new FilterDefaultsStore(path);
            store.TryLoad();

            Assert.False(store.TryGetDefault("NotARealFilter", out _));
            Assert.False(store.TryGetDefault("LettersCase", out _));
            Assert.True(store.TryGetDefault("ShrinkSpaces", out var shrink));
            Assert.Equal("ShrinkSpaces", shrink.Type);
        }

        /// <summary>
        /// Verifies a missing file leaves the store empty.
        /// </summary>
        [Fact]
        public void TryLoad_missing_file_is_empty()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("missing.json");
            var store = new FilterDefaultsStore(path);
            store.TryLoad();
            Assert.False(store.TryGetDefault("LettersCase", out _));
        }

        /// <summary>
        /// Verifies serialized shape uses a <c>defaults</c> map keyed by type.
        /// </summary>
        [Fact]
        public void Save_writes_defaults_object_keyed_by_type()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("filter-defaults.json");
            var store = new FilterDefaultsStore(path);
            store.SetDefault(new LettersCaseFilter());

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            Assert.True(doc.RootElement.TryGetProperty("defaults", out var defaults));
            Assert.True(defaults.TryGetProperty("LettersCase", out var entry));
            Assert.Equal("LettersCase", entry.GetProperty("type").GetString());
        }
    }
}
