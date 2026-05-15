using System.Text.Json;
using Mfr.Core;
using Mfr.Models;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Core
{
    /// <summary>
    /// Tests for <see cref="RenamePropertyChangeBuilder.BuildChangeRows"/>.
    /// </summary>
    public sealed class RenamePropertyChangeBuilderTests
    {
        /// <summary>
        /// Identical snapshots should yield no property rows.
        /// </summary>
        [Fact]
        public void BuildChangeRows_IdenticalSnapshots_ReturnsEmpty()
        {
            var original = _CloneBaseline();
            var preview = original.Clone();

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(original, preview);

            Assert.Empty(rows);
        }

        /// <summary>
        /// Prefix deltas use ordinal comparison and raw string values (not JSON).
        /// </summary>
        [Fact]
        public void BuildChangeRows_PrefixChange_ReturnsSingleOrdinalRow()
        {
            var original = _CloneBaseline();
            var preview = original.Clone();
            preview.Prefix = "Song";

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(original, preview);

            var row = Assert.Single(rows);
            Assert.Equal("Prefix", row.Property);
            Assert.Equal("song", row.OldValue);
            Assert.Equal("Song", row.NewValue);
        }

        /// <summary>
        /// Extension changes are surfaced independently from prefix.
        /// </summary>
        [Fact]
        public void BuildChangeRows_ExtensionChange_ReturnsExtensionRow()
        {
            var original = _CloneBaseline();
            var preview = original.Clone();
            preview.Extension = ".flac";

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(original, preview);

            var row = Assert.Single(rows);
            Assert.Equal("Extension", row.Property);
            Assert.Equal(".mp3", row.OldValue);
            Assert.Equal(".flac", row.NewValue);
        }

        /// <summary>
        /// Directory moves are detected via path segments on <see cref="FileMeta"/>.
        /// </summary>
        [Fact]
        public void BuildChangeRows_DirectoryChange_ReturnsDirectoryPathRow()
        {
            var original = _CloneBaseline();
            var preview = original.Clone();
            preview.DirectoryPath = @"D:\Out";

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(original, preview);

            var row = Assert.Single(rows);
            Assert.Equal("DirectoryPath", row.Property);
            Assert.Equal(@"D:\In", row.OldValue);
            Assert.Equal(@"D:\Out", row.NewValue);
        }

        /// <summary>
        /// Directory comparison ignores case so Windows-only casing tweaks do not emit a row.
        /// </summary>
        [Fact]
        public void BuildChangeRows_DirectoryPathCaseOnly_DoesNotEmitDirectoryRow()
        {
            var original = _CloneBaseline(directoryPath: @"D:\Music\Album");
            var preview = original.Clone();
            preview.DirectoryPath = @"d:\music\album";

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(original, preview);

            Assert.Empty(rows);
        }

        /// <summary>
        /// Filesystem attribute edits surface as stringified enum rows.
        /// </summary>
        [Fact]
        public void BuildChangeRows_AttributesChange_ReturnsAttributesRow()
        {
            var original = _CloneBaseline();
            var preview = original.Clone();
            preview.Attributes = FileAttributes.ReadOnly;

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(original, preview);

            var row = Assert.Single(rows);
            Assert.Equal("Attributes", row.Property);
            Assert.Equal(FileAttributes.Normal.ToString(), row.OldValue);
            Assert.Equal(FileAttributes.ReadOnly.ToString(), row.NewValue);
        }

        /// <summary>
        /// Creation, last-write, and last-access times each emit round-trip local <c>O</c> stamps when they differ.
        /// </summary>
        [Fact]
        public void BuildChangeRows_TimestampChanges_ReturnThreeRowsInOrder()
        {
            var t0 = new DateTime(2024, 1, 1, 1, 1, 1, DateTimeKind.Unspecified);
            var t1 = new DateTime(2024, 2, 2, 2, 2, 2, DateTimeKind.Unspecified);
            var t2 = new DateTime(2024, 3, 3, 3, 3, 3, DateTimeKind.Unspecified);
            var t3 = new DateTime(2024, 4, 4, 4, 4, 4, DateTimeKind.Unspecified);
            var original = _CloneBaseline(creationTime: t0, lastWriteTime: t1, lastAccessTime: t2);
            var preview = original.Clone();
            preview.CreationTime = t1;
            preview.LastWriteTime = t2;
            preview.LastAccessTime = t3;

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(original, preview);

            Assert.Equal(3, rows.Count);
            Assert.Equal("CreationTime", rows[0].Property);
            Assert.Equal(t0.ToString("O"), rows[0].OldValue);
            Assert.Equal(t1.ToString("O"), rows[0].NewValue);
            Assert.Equal("LastWriteTime", rows[1].Property);
            Assert.Equal("LastAccessTime", rows[2].Property);
        }

        /// <summary>
        /// Embedded tag strings use JSON serialization so null clears remain distinguishable in text output.
        /// </summary>
        [Fact]
        public void BuildChangeRows_AudioTagTitleChange_UsesJsonEncodedScalars()
        {
            var original = _CloneBaseline();
            var preview = original.Clone();
            preview.AudioTagOverlay.Title = "Next";

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(original, preview);

            var row = Assert.Single(rows);
            Assert.Equal("AudioTag.Title", row.Property);
            Assert.Equal(JsonSerializer.Serialize((string?)null), row.OldValue);
            Assert.Equal(JsonSerializer.Serialize("Next"), row.NewValue);
        }

        /// <summary>
        /// Nullable unsigned tag fields serialize as JSON numbers or null tokens.
        /// </summary>
        [Fact]
        public void BuildChangeRows_AudioTagYearChange_EncodesUIntAndNull()
        {
            var original = _CloneBaseline(configureOverlay: o => o.AudioTagOverlay.Year = 1999);
            var preview = original.Clone();
            preview.AudioTagOverlay.Year = 2001;

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(original, preview);

            var row = Assert.Single(rows);
            Assert.Equal("AudioTag.Year", row.Property);
            Assert.Equal(JsonSerializer.Serialize((uint?)1999), row.OldValue);
            Assert.Equal(JsonSerializer.Serialize((uint?)2001), row.NewValue);
        }

        /// <summary>
        /// Multiline tag text stays JSON-escaped on one logical value line for preview formatting.
        /// </summary>
        [Fact]
        public void BuildChangeRows_AudioTagLyricsWithNewline_SerializesEscapedLineBreak()
        {
            var original = _CloneBaseline();
            var preview = original.Clone();
            preview.AudioTagOverlay.Lyrics = "a\nb";

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(original, preview);

            var row = Assert.Single(rows);
            Assert.Equal("AudioTag.Lyrics", row.Property);
            Assert.False(row.NewValue.Contains('\n', StringComparison.Ordinal));
            Assert.Contains("\\n", row.NewValue, StringComparison.Ordinal);
        }

        /// <summary>
        /// Rows follow structured path fields, then filesystem scalars, then stable audio-tag field order.
        /// </summary>
        [Fact]
        public void BuildChangeRows_MixedDifferences_FollowsStableCategoryOrdering()
        {
            var original = _CloneBaseline(directoryPath: @"D:\A");
            var preview = original.Clone();
            preview.DirectoryPath = @"D:\B";
            preview.Attributes = FileAttributes.ReadOnly;
            preview.AudioTagOverlay.Genre = "Rock";

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(original, preview);

            Assert.Equal(
                ["DirectoryPath", "Attributes", "AudioTag.Genre"],
                rows.Select(r => r.Property).ToArray());
        }

        private static FileMeta _CloneBaseline(
            string? directoryPath = null,
            DateTime? creationTime = null,
            DateTime? lastWriteTime = null,
            DateTime? lastAccessTime = null,
            Action<FileMeta>? configureOverlay = null)
        {
            var item = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                extension: ".mp3",
                directory: directoryPath ?? @"D:\In",
                attributes: FileAttributes.Normal,
                creationTime: creationTime ?? new DateTime(2024, 6, 1, 12, 30, 45, DateTimeKind.Unspecified),
                lastWriteTime: lastWriteTime ?? new DateTime(2024, 6, 1, 12, 30, 46, DateTimeKind.Unspecified),
                lastAccessTime: lastAccessTime ?? new DateTime(2024, 6, 1, 12, 30, 47, DateTimeKind.Unspecified),
                configureOriginal: configureOverlay);

            return item.Original.Clone();
        }
    }
}
