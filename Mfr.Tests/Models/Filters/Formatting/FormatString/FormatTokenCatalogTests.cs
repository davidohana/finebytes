using Mfr.Filters.Formatting.FormatString;
using Mfr.Models.Tags;

namespace Mfr.Tests.Models.Filters.Formatting.FormatString
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
                Assert.Equal(entry.CanonicalName, span.WrittenName);
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

        /// <summary>
        /// Verifies every <c>Audio\\Tag</c> token maps to a semantic field and uses
        /// <see cref="SemanticAudioFieldLabels"/>.
        /// </summary>
        [Fact]
        public void Audio_tag_display_names_match_semantic_labels()
        {
            var canonicalNameToField = new Dictionary<string, SemanticAudioField>(StringComparer.Ordinal)
            {
                ["audio-title"] = SemanticAudioField.Title,
                ["audio-artist"] = SemanticAudioField.Performers,
                ["audio-album-artist"] = SemanticAudioField.AlbumArtists,
                ["audio-album"] = SemanticAudioField.Album,
                ["audio-year"] = SemanticAudioField.Year,
                ["audio-genre"] = SemanticAudioField.Genre,
                ["audio-track"] = SemanticAudioField.Track,
                ["audio-track-count"] = SemanticAudioField.TrackCount,
                ["audio-disc"] = SemanticAudioField.Disc,
                ["audio-disc-count"] = SemanticAudioField.DiscCount,
                ["audio-comment"] = SemanticAudioField.Comment,
                ["audio-composer"] = SemanticAudioField.Composers,
                ["audio-lyrics"] = SemanticAudioField.Lyrics,
                ["audio-copyright"] = SemanticAudioField.Copyright,
                ["audio-grouping"] = SemanticAudioField.Grouping,
                ["audio-bpm"] = SemanticAudioField.BeatsPerMinute,
                ["audio-conductor"] = SemanticAudioField.Conductor,
                ["audio-mb-artist-id"] = SemanticAudioField.MusicBrainzArtistId,
                ["audio-mb-release-id"] = SemanticAudioField.MusicBrainzReleaseId,
                ["audio-mb-release-artist-id"] = SemanticAudioField.MusicBrainzReleaseArtistId,
                ["audio-mb-track-id"] = SemanticAudioField.MusicBrainzTrackId,
                ["audio-mb-disc-id"] = SemanticAudioField.MusicBrainzDiscId,
                ["audio-mb-release-status"] = SemanticAudioField.MusicBrainzReleaseStatus,
                ["audio-mb-release-type"] = SemanticAudioField.MusicBrainzReleaseType,
                ["audio-mb-release-country"] = SemanticAudioField.MusicBrainzReleaseCountry,
                ["audio-musicip-id"] = SemanticAudioField.MusicIpId,
                ["audio-amazon-id"] = SemanticAudioField.AmazonId,
            };

            Assert.Equal(Enum.GetValues<SemanticAudioField>().Length, canonicalNameToField.Count);
            Assert.Equal(
                canonicalNameToField.Count,
                FormatTokenCatalog.Entries.Count(e => e.GroupPath == "Audio\\Tag")
            );

            foreach (var (canonicalName, field) in canonicalNameToField)
            {
                var entry = Assert.Single(
                    FormatTokenCatalog.Entries,
                    e => string.Equals(e.CanonicalName, canonicalName, StringComparison.Ordinal)
                );
                Assert.Equal(SemanticAudioFieldLabels.For(field), entry.DisplayName);
            }
        }
    }
}
