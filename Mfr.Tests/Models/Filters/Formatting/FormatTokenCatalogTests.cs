using Mfr.Filters.Formatting;

namespace Mfr.Tests.Models.Filters.Formatting
{
    /// <summary>
    /// Tests for <see cref="FormatTokenCatalog"/> discovery and metadata.
    /// </summary>
    public sealed class FormatTokenCatalogTests
    {
        /// <summary>
        /// Verifies every concrete <c>IFormatToken</c> appears exactly once in the catalog.
        /// </summary>
        [Fact]
        public void Entries_CoverEveryConcreteTokenExactlyOnce()
        {
            var tokenTypeCount = typeof(FormatTokenCatalog)
                .Assembly.GetTypes()
                .Count(t =>
                    t.IsClass && !t.IsAbstract && typeof(Mfr.Filters.Formatting.Tokens.IFormatToken).IsAssignableFrom(t)
                );

            Assert.Equal(tokenTypeCount, FormatTokenCatalog.Entries.Count);
            Assert.Equal(
                FormatTokenCatalog.Entries.Count,
                FormatTokenCatalog.Entries.Select(e => e.CanonicalName).Distinct(StringComparer.Ordinal).Count()
            );
        }

        /// <summary>
        /// Verifies catalog insert text validates and compiles.
        /// </summary>
        [Fact]
        public void Entries_InsertText_ValidatesAndCompiles()
        {
            foreach (var entry in FormatTokenCatalog.Entries)
            {
                var result = FormatStringSyntax.TryValidate(entry.InsertText);
                Assert.True(result.Success, $"{entry.CanonicalName}: {result.ErrorMessage}");
                Assert.Contains(
                    result.Tokens,
                    t => string.Equals(t.CanonicalName, entry.CanonicalName, StringComparison.Ordinal)
                );

                var compiled = FormatStringCompiler.Compile(entry.InsertText);
                Assert.NotNull(compiled);
            }
        }

        /// <summary>
        /// Verifies each entry's insert text uses that token's canonical name (not an alias).
        /// </summary>
        [Fact]
        public void Entries_InsertText_UsesCanonicalName()
        {
            foreach (var entry in FormatTokenCatalog.Entries)
            {
                var result = FormatStringSyntax.TryValidate(entry.InsertText);
                Assert.True(result.Success, entry.CanonicalName);
                var span = Assert.Single(result.Tokens);
                Assert.Equal(entry.CanonicalName, span.CanonicalName);
                Assert.StartsWith("<" + entry.CanonicalName, entry.InsertText, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Verifies aliases are not duplicated as separate catalog rows.
        /// </summary>
        [Fact]
        public void Entries_ListsCanonicalOnly_NotExtAlias()
        {
            Assert.Contains(FormatTokenCatalog.Entries, e => e.CanonicalName == "file-extension");
            Assert.DoesNotContain(FormatTokenCatalog.Entries, e => e.CanonicalName == "ext");
            Assert.DoesNotContain(
                FormatTokenCatalog.Entries,
                e => e.InsertText.Contains("<ext>", StringComparison.Ordinal)
            );
        }

        /// <summary>
        /// Verifies entries are sorted by group then display name.
        /// </summary>
        [Fact]
        public void Entries_AreSortedByGroupThenDisplayName()
        {
            var ordered = FormatTokenCatalog
                .Entries.OrderBy(e => e.GroupPath, StringComparer.OrdinalIgnoreCase)
                .ThenBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Assert.Equal(ordered, [.. FormatTokenCatalog.Entries]);
        }
    }
}
