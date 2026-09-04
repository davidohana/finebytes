using Mfr.Filters.Replace;

namespace Mfr.Tests.Models.Filters.Replace
{
    /// <summary>
    /// Tests for <see cref="ReplaceListFilter"/>.
    /// </summary>
    public sealed class ReplaceListFilterTests
    {
        private static readonly FilePrefixTarget _target = new();

        /// <summary>
        /// Verifies that replacements from the embedded list are applied in order.
        /// </summary>
        [Fact]
        public void Apply_LiteralPairs_AppliesSequentially()
        {
            var filter = _CreateFilter(
                entries: [new ReplaceListEntry("a", "b"), new ReplaceListEntry(".", "_")],
                mode: ReplacerMode.Literal,
                caseSensitive: true,
                replaceAll: true,
                wholeWord: false
            );

            var result = FilterTestHelpers.ApplyToPrefix(filter, "a.a");

            Assert.Equal("b_b", result);
        }

        /// <summary>
        /// Verifies search text may contain spaces when whole-word matching is off.
        /// </summary>
        [Fact]
        public void Apply_SearchWithSpaces_ReplacesLiteralPhrase()
        {
            var filter = _CreateFilter(
                entries: [new ReplaceListEntry("Blue Train", "Blue_Train")],
                mode: ReplacerMode.Literal,
                caseSensitive: true,
                replaceAll: true,
                wholeWord: false
            );

            var result = FilterTestHelpers.ApplyToPrefix(filter, "Blue Train Live");

            Assert.Equal("Blue_Train Live", result);
        }

        /// <summary>
        /// Verifies that an empty replacement strips the search string.
        /// </summary>
        [Fact]
        public void Apply_EmptyReplacement_StripsMatchedSearchString()
        {
            var filter = _CreateFilter(
                entries: [new ReplaceListEntry("x", "")],
                mode: ReplacerMode.Literal,
                caseSensitive: true,
                replaceAll: true,
                wholeWord: false
            );

            var result = FilterTestHelpers.ApplyToPrefix(filter, "abxcx");

            Assert.Equal("abc", result);
        }

        /// <summary>
        /// Verifies an empty list is a no-op.
        /// </summary>
        [Fact]
        public void Apply_EmptyList_IsNoOp()
        {
            var filter = _CreateFilter(
                entries: [],
                mode: ReplacerMode.Literal,
                caseSensitive: true,
                replaceAll: true,
                wholeWord: false
            );

            var result = FilterTestHelpers.ApplyToPrefix(filter, "unchanged");

            Assert.Equal("unchanged", result);
        }

        /// <summary>
        /// Verifies regex mode and formatter tokens in replacement values.
        /// </summary>
        [Fact]
        public void Apply_RegexAndCounterToken_MatchesPromptExampleBehavior()
        {
            var filter = _CreateFilter(
                entries:
                [
                    new ReplaceListEntry("a", "b"),
                    new ReplaceListEntry(@"\.", "_"),
                    new ReplaceListEntry(
                        "[0-9]+",
                        "<counter:initial=10,step=1,padding=none,length=2,resetScope=global>"
                    ),
                ],
                mode: ReplacerMode.Regex,
                caseSensitive: false,
                replaceAll: true,
                wholeWord: false
            );

            var first = FilterTestHelpers.ApplyToPrefix(
                filter: filter,
                inputPrefix: "01.-.Blue.Train",
                renameListIndex: 0
            );
            var second = FilterTestHelpers.ApplyToPrefix(
                filter: filter,
                inputPrefix: "02.-.A.Moment's.Notice",
                renameListIndex: 1
            );

            Assert.Equal("10_-_Blue_Trbin", first);
            Assert.Equal("11_-_b_Moment's_Notice", second);
        }

        /// <summary>
        /// Verifies wildcard mode is supported by replace-list entries.
        /// </summary>
        [Fact]
        public void Apply_WildcardMode_UsesWildcardMatching()
        {
            var filter = _CreateFilter(
                entries: [new ReplaceListEntry("f*o", "X")],
                mode: ReplacerMode.Wildcard,
                caseSensitive: true,
                replaceAll: true,
                wholeWord: false
            );

            var result = FilterTestHelpers.ApplyToPrefix(filter, "foo");

            Assert.Equal("X", result);
        }

        /// <summary>
        /// Verifies compiled entries are reused across Apply calls on the same instance.
        /// </summary>
        [Fact]
        public void Apply_InstanceCache_ReusesAcrossApplyCalls()
        {
            var filter = _CreateFilter(
                entries: [new ReplaceListEntry("a", "x")],
                mode: ReplacerMode.Literal,
                caseSensitive: true,
                replaceAll: true,
                wholeWord: false
            );
            filter.Setup();
            var firstItem = FilterTestHelpers.CreateRenameItem(prefix: "a");
            filter.Apply(firstItem);
            Assert.Equal("x", firstItem.Preview.Prefix);

            var secondItem = FilterTestHelpers.CreateRenameItem(prefix: "a");
            filter.Apply(secondItem);
            Assert.Equal("x", secondItem.Preview.Prefix);
        }

        private static ReplaceListFilter _CreateFilter(
            IReadOnlyList<ReplaceListEntry> entries,
            ReplacerMode mode,
            bool caseSensitive,
            bool replaceAll,
            bool wholeWord
        )
        {
            var options = new ReplaceListOptions(
                Entries: entries,
                Mode: mode,
                CaseSensitive: caseSensitive,
                ReplaceAll: replaceAll,
                WholeWord: wholeWord
            );
            return new ReplaceListFilter(Target: _target, Options: options);
        }
    }
}
