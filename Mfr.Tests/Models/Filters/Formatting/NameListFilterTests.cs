using System.Text.Json;
using Mfr.Filters.Formatting;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="NameListFilter"/>.
    /// </summary>
    public sealed class NameListFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies list line N maps to global index N.
        /// </summary>
        [Fact]
        public void Apply_MapsLineIndexToGlobalIndex()
        {
            var f = _CreateFilter(["Alpha", "Beta", "Gamma"]);
            Assert.Equal("Alpha", FilterTestHelpers.ApplyToPrefix(f, "old0", renameListIndex: 0));
            Assert.Equal("Beta", FilterTestHelpers.ApplyToPrefix(f, "old1", renameListIndex: 1));
            Assert.Equal("Gamma", FilterTestHelpers.ApplyToPrefix(f, "old2", renameListIndex: 2));
        }

        /// <summary>
        /// Verifies prefix and suffix templates resolve with formatter tokens.
        /// </summary>
        [Fact]
        public void Apply_PrefixSuffixAndCounterToken()
        {
            var f = new NameListFilter(
                Target: _target,
                Options: new NameListOptions(
                    Entries: ["One"],
                    Prefix: "<counter:initial=10,step=1,padding=none,length=2,resetScope=global>_",
                    Suffix: "_end"
                )
            );
            Assert.Equal("10_One_end", FilterTestHelpers.ApplyToPrefix(f, "x", renameListIndex: 0));
        }

        /// <summary>
        /// Verifies blank lines are preserved as entries.
        /// </summary>
        [Fact]
        public void Apply_BlankLines_AreEntries()
        {
            var f = _CreateFilter(["First", "", "Second"]);
            Assert.Equal("First", FilterTestHelpers.ApplyToPrefix(f, "a", renameListIndex: 0));
            Assert.Equal(string.Empty, FilterTestHelpers.ApplyToPrefix(f, "b", renameListIndex: 1));
            Assert.Equal("Second", FilterTestHelpers.ApplyToPrefix(f, "c", renameListIndex: 2));
        }

        /// <summary>
        /// Verifies a null list element applies as an empty name.
        /// </summary>
        [Fact]
        public void Apply_NullEntry_IsEmptyName()
        {
            var f = _CreateFilter(["First", null!, "Second"]);
            Assert.Equal(string.Empty, FilterTestHelpers.ApplyToPrefix(f, "b", renameListIndex: 1));
        }

        /// <summary>
        /// Verifies an empty list leaves the original value unchanged.
        /// </summary>
        [Fact]
        public void Apply_EmptyList_IsNoOp()
        {
            var f = _CreateFilter([]);
            Assert.Equal("old", FilterTestHelpers.ApplyToPrefix(f, "old", renameListIndex: 0));
        }

        /// <summary>
        /// Verifies out-of-range index throws <see cref="UserException"/>.
        /// </summary>
        [Fact]
        public void Apply_TooFewLines_ThrowsUserException()
        {
            var f = _CreateFilter(["Only"]);
            var ex = Assert.Throws<UserException>(() => FilterTestHelpers.ApplyToPrefix(f, "old", renameListIndex: 1));
            Assert.Equal(
                "Name-list has 1 line(s) but rename item is 2. Add lines or adjust the rename list.",
                ex.Message
            );
        }

        /// <summary>
        /// Verifies the NameList.md sample preset shape deserializes.
        /// </summary>
        [Fact]
        public void JsonDeserialize_SamplePreset_EntriesArray()
        {
            var json = /*lang=json,strict*/
                """
                {
                  "type": "NameList",
                  "target": {
                    "targetType": "FilePrefix"
                  },
                  "options": {
                    "entries": ["Alpha", "Beta", "Gamma"],
                    "prefix": "",
                    "suffix": ""
                  }
                }
                """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            var typed = Assert.IsType<NameListFilter>(filter);
            Assert.Equal(["Alpha", "Beta", "Gamma"], typed.Options.Entries);
            Assert.Equal("Beta", FilterTestHelpers.ApplyToPrefix(typed, "old1", renameListIndex: 1));
        }

        /// <summary>
        /// Verifies omitting <c>entries</c> deserializes as empty and is a no-op.
        /// </summary>
        [Fact]
        public void JsonDeserialize_MissingEntries_IsEmptyNoOp()
        {
            var json = /*lang=json,strict*/
                """
                {
                  "type": "NameList",
                  "target": {
                    "targetType": "FilePrefix"
                  },
                  "options": {
                    "prefix": "",
                    "suffix": ""
                  }
                }
                """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            var typed = Assert.IsType<NameListFilter>(filter);
            Assert.Empty(typed.Options.Entries);
            Assert.Equal("old", FilterTestHelpers.ApplyToPrefix(typed, "old", renameListIndex: 0));
        }

        /// <summary>
        /// Verifies null <c>prefix</c>/<c>suffix</c> coerce to empty and still apply.
        /// </summary>
        [Fact]
        public void JsonDeserialize_NullPrefixSuffix_CoerceToEmpty()
        {
            var json = /*lang=json,strict*/
                """
                {
                  "type": "NameList",
                  "target": {
                    "targetType": "FilePrefix"
                  },
                  "options": {
                    "entries": ["Only"],
                    "prefix": null,
                    "suffix": null
                  }
                }
                """;

            var filter = JsonSerializer.Deserialize<BaseFilter>(json, PresetJsonOptions.Default);
            var typed = Assert.IsType<NameListFilter>(filter);
            Assert.Equal(string.Empty, typed.Options.Prefix);
            Assert.Equal(string.Empty, typed.Options.Suffix);
            Assert.Equal("Only", FilterTestHelpers.ApplyToPrefix(typed, "old", renameListIndex: 0));
        }

        /// <summary>
        /// Verifies constructor null prefix/suffix coerce to empty.
        /// </summary>
        [Fact]
        public void Options_NullPrefixSuffix_CoerceToEmpty()
        {
            var options = new NameListOptions(Entries: ["A"], Prefix: null!, Suffix: null!);
            Assert.Equal(string.Empty, options.Prefix);
            Assert.Equal(string.Empty, options.Suffix);
        }

        private static NameListFilter _CreateFilter(IReadOnlyList<string> entries)
        {
            return new NameListFilter(
                Target: _target,
                Options: new NameListOptions(Entries: entries, Prefix: "", Suffix: "")
            );
        }
    }
}
