using Mfr.Filters.Formatting.FormatString;
using Mfr.Models.RenameList.Fields.Basic;
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
        /// <see cref="SemanticAudioFieldLabels"/> (resolved at catalog build, not from attribute text).
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

        /// <summary>
        /// Verifies File Name group tokens use <see cref="PathFieldLabels"/> (same as Rename List / Apply-To).
        /// </summary>
        [Theory]
        [InlineData("file-name", PathFieldLabels.FileName)]
        [InlineData("file-extension", PathFieldLabels.FileExtension)]
        [InlineData("full-name", PathFieldLabels.FullFileName)]
        [InlineData("full-path", PathFieldLabels.FullPath)]
        [InlineData("full-path-length", PathFieldLabels.FullPathLength)]
        [InlineData("parent-folder", PathFieldLabels.ParentFolder)]
        [InlineData("file-or-folder", PathFieldLabels.FileOrFolder)]
        [InlineData("file-name-length", PathFieldLabels.FileNameLength)]
        [InlineData("file-name-numeric-value", PathFieldLabels.FileNameNumericValue)]
        public void File_name_token_display_names_match_path_field_labels(string canonicalName, string label)
        {
            var entry = Assert.Single(
                FormatTokenCatalog.Entries,
                e => string.Equals(e.CanonicalName, canonicalName, StringComparison.Ordinal)
            );
            Assert.Equal(label, entry.DisplayName);
            Assert.Equal(PathFieldLabels.FileName, entry.GroupPath);
        }

        /// <summary>
        /// Verifies overlapping Rename List basic columns use the same <see cref="PathFieldLabels"/> strings.
        /// </summary>
        [Theory]
        [InlineData(BasicRenameListFields.Key.ItemType, PathFieldLabels.FileOrFolder)]
        [InlineData(BasicRenameListFields.Key.Folder, PathFieldLabels.ParentDirectory)]
        [InlineData(BasicRenameListFields.Key.FullName, PathFieldLabels.FullFileName)]
        [InlineData(BasicRenameListFields.Key.FullPath, PathFieldLabels.FullPath)]
        [InlineData(BasicRenameListFields.Key.Name, PathFieldLabels.FileName)]
        [InlineData(BasicRenameListFields.Key.Extension, PathFieldLabels.FileExtension)]
        [InlineData(BasicRenameListFields.Key.FileNameNumeric, PathFieldLabels.FileNameNumericValue)]
        [InlineData(BasicRenameListFields.Key.FileNameLength, PathFieldLabels.FileNameLength)]
        [InlineData(BasicRenameListFields.Key.FullPathLength, PathFieldLabels.FullPathLength)]
        public void Basic_rename_list_display_names_use_path_field_labels(string propertyKey, string label)
        {
            var field = Assert.Single(BasicRenameListFields.All, f => f.PropertyKey == propertyKey);
            Assert.Equal(label, field.DisplayName);
        }
    }
}
