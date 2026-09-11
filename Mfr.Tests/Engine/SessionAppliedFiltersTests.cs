using System.Text.Json;
using Mfr.Filters.Attributes;
using Mfr.Filters.Case;
using Mfr.Filters.Space;
using Mfr.Utils;

namespace Mfr.Tests.Engine
{
    /// <summary>
    /// Tests session persistence of the working Applied Filters <see cref="FilterChain"/>.
    /// </summary>
    public sealed class SessionAppliedFiltersTests : IDisposable
    {
        private readonly TempDirectoryFixture _tempDirectoryFixture = new();

        /// <inheritdoc />
        public void Dispose()
        {
            _tempDirectoryFixture.Dispose();
        }

        /// <summary>
        /// Verifies enabled flags and filters round-trip through <see cref="SessionStore"/> with
        /// <see cref="SessionJsonOptions"/>.
        /// </summary>
        [Fact]
        public void Save_and_Load_round_trips_applied_filters_chain()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("session.json");
            var letters = new LettersCaseFilter();
            var shrink = new ShrinkSpacesFilter();
            var original = new SessionState
            {
                Version = 1,
                AppliedFilters = new FilterChain
                {
                    Steps =
                    [
                        new FilterChainStep(Enabled: false, Filter: letters),
                        new FilterChainStep(Enabled: true, Filter: shrink),
                    ],
                },
            };

            SessionStore.Save(original, path, SessionJsonOptions.Default);
            var loaded = SessionStore.Load(path, SessionJsonOptions.Default);

            Assert.NotNull(loaded.AppliedFilters);
            Assert.Equal(2, loaded.AppliedFilters.Steps.Count);
            Assert.False(loaded.AppliedFilters.Steps[0].Enabled);
            Assert.Equal("LettersCase", loaded.AppliedFilters.Steps[0].Filter.Type);
            Assert.True(loaded.AppliedFilters.Steps[1].Enabled);
            Assert.Equal("ShrinkSpaces", loaded.AppliedFilters.Steps[1].Filter.Type);

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            Assert.True(doc.RootElement.TryGetProperty("appliedFilters", out var applied));
            Assert.True(applied.TryGetProperty("steps", out var steps));
            Assert.Equal(2, steps.GetArrayLength());
            Assert.True(steps[0].TryGetProperty("enabled", out _));
            Assert.True(steps[0].TryGetProperty("filter", out var filter));
            Assert.Equal("LettersCase", filter.GetProperty("type").GetString());
        }

        /// <summary>
        /// Verifies unknown or invalid steps are dropped while valid steps and the rest of the session remain.
        /// </summary>
        [Fact]
        public void Load_drops_unknown_and_invalid_steps_keeps_valid()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("session.json");
            File.WriteAllText(
                path, /*lang=json,strict*/
                """
                {
                  "version": 1,
                  "renameList": { "previewEnabled": false },
                  "appliedFilters": {
                    "steps": [
                      { "enabled": true, "filter": { "type": "NotARealFilter" } },
                      { "enabled": false, "filter": { "type": "LettersCase", "target": { "kind": "broken" } } },
                      { "enabled": true, "filter": { "type": "ShrinkSpaces" } },
                      { "enabled": true },
                      "not-an-object"
                    ]
                  }
                }
                """
            );

            var loaded = SessionStore.Load(path, SessionJsonOptions.Default);

            Assert.False(loaded.RenameList?.PreviewEnabled);
            Assert.NotNull(loaded.AppliedFilters);
            Assert.Single(loaded.AppliedFilters.Steps);
            Assert.True(loaded.AppliedFilters.Steps[0].Enabled);
            Assert.Equal("ShrinkSpaces", loaded.AppliedFilters.Steps[0].Filter.Type);
        }

        /// <summary>
        /// Verifies a missing appliedFilters section stays null (first-launch default).
        /// </summary>
        [Fact]
        public void Load_omitted_appliedFilters_is_null()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("session.json");
            File.WriteAllText(
                path, /*lang=json,strict*/
                """{"version":1}"""
            );

            var loaded = SessionStore.Load(path, SessionJsonOptions.Default);
            Assert.Null(loaded.AppliedFilters);
        }

        /// <summary>
        /// Verifies an empty steps array round-trips as an empty chain (not null).
        /// </summary>
        [Fact]
        public void Save_and_Load_empty_steps_is_empty_chain()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("session.json");
            SessionStore.Save(
                new SessionState { AppliedFilters = new FilterChain { Steps = [] } },
                path,
                SessionJsonOptions.Default
            );

            var loaded = SessionStore.Load(path, SessionJsonOptions.Default);
            Assert.NotNull(loaded.AppliedFilters);
            Assert.Empty(loaded.AppliedFilters.Steps);
        }

        /// <summary>
        /// Verifies filter option enums in session <c>appliedFilters</c> use preset casing
        /// (<c>Keep</c>), not session-level camelCase (<c>keep</c>).
        /// </summary>
        [Fact]
        public void Save_filter_option_enums_match_preset_casing()
        {
            var path = _tempDirectoryFixture.CreateTempDir().CombinePath("session.json");
            SessionStore.Save(
                new SessionState
                {
                    AppliedFilters = new FilterChain
                    {
                        Steps = [new FilterChainStep(Enabled: true, Filter: new AttributesSetterFilter())],
                    },
                },
                path,
                SessionJsonOptions.Default
            );

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var options = doc
                .RootElement.GetProperty("appliedFilters")
                .GetProperty("steps")[0]
                .GetProperty("filter")
                .GetProperty("Options");

            Assert.Equal("Keep", options.GetProperty("readOnly").GetString());
            Assert.Equal("Keep", options.GetProperty("hidden").GetString());
        }
    }
}
