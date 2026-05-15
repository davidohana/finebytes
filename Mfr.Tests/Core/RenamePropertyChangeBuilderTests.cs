using System.Text.Json;
using Mfr.Core;
using Mfr.Metadata;
using Mfr.Models;
using Mfr.Models.Tags;
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
            var item = new RenameItem(original);

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            Assert.Empty(rows);
        }

        /// <summary>
        /// Prefix deltas use ordinal comparison and raw string values (not JSON).
        /// </summary>
        [Fact]
        public void BuildChangeRows_PrefixChange_ReturnsSingleOrdinalRow()
        {
            var original = _CloneBaseline();
            var item = new RenameItem(original);
            item.Preview.Prefix = "Song";

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

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
            var item = new RenameItem(original);
            item.Preview.Extension = ".flac";

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

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
            var item = new RenameItem(original);
            item.Preview.DirectoryPath = @"D:\Out";

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

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
            var item = new RenameItem(original);
            item.Preview.DirectoryPath = @"d:\music\album";

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            Assert.Empty(rows);
        }

        /// <summary>
        /// Filesystem attribute edits surface as stringified enum rows.
        /// </summary>
        [Fact]
        public void BuildChangeRows_AttributesChange_ReturnsAttributesRow()
        {
            var original = _CloneBaseline();
            var item = new RenameItem(original);
            item.Preview.Attributes = FileAttributes.ReadOnly;

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

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
            var item = new RenameItem(original);
            item.Preview.CreationTime = t1;
            item.Preview.LastWriteTime = t2;
            item.Preview.LastAccessTime = t3;

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

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
            var item = new RenameItem(original);
            var pv = item.Preview.AudioTagOverlay;
            AudioTagPersistence.MergeSemanticOntoNativeBlocks(
                pv,
                AudioTagSemanticSurface.FromBlocks(pv) with { Title = "Next" },
                embeddedTagSourcePath: null);

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            Assert.Equal(2, rows.Count);
            Assert.Contains(rows, static r => r.Property == "AudioTag.Title");
            Assert.Contains(rows, static r => r.Property == "AudioTag.Native.Id3v2");

            var titleRow = Assert.Single(rows, static r => r.Property == "AudioTag.Title");
            Assert.Equal(JsonSerializer.Serialize((string?)null), titleRow.OldValue);
            Assert.Equal(JsonSerializer.Serialize("Next"), titleRow.NewValue);
        }

        /// <summary>
        /// Nullable unsigned tag fields serialize as JSON numbers or null tokens.
        /// </summary>
        [Fact]
        public void BuildChangeRows_AudioTagYearChange_EncodesUIntAndNull()
        {
            var original = _CloneBaseline(configureOverlay: o =>
                o.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(year: 1999));
            var item = new RenameItem(original);
            var pv = item.Preview.AudioTagOverlay;
            AudioTagPersistence.MergeSemanticOntoNativeBlocks(
                pv,
                AudioTagSemanticSurface.FromBlocks(pv) with { Year = 2001 },
                embeddedTagSourcePath: null);

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            Assert.Equal(2, rows.Count);
            var yearRow = Assert.Single(rows, static r => r.Property == "AudioTag.Year");
            Assert.Equal(JsonSerializer.Serialize((uint?)1999), yearRow.OldValue);
            Assert.Equal(JsonSerializer.Serialize((uint?)2001), yearRow.NewValue);
        }

        /// <summary>
        /// Multiline tag text stays JSON-escaped on one logical value line for preview formatting.
        /// </summary>
        [Fact]
        public void BuildChangeRows_AudioTagLyricsWithNewline_SerializesEscapedLineBreak()
        {
            var original = _CloneBaseline();
            var item = new RenameItem(original);
            var pv = item.Preview.AudioTagOverlay;
            AudioTagPersistence.MergeSemanticOntoNativeBlocks(
                pv,
                AudioTagSemanticSurface.FromBlocks(pv) with { Lyrics = "a\nb" },
                embeddedTagSourcePath: null);

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            var lyricsRow = Assert.Single(rows, static r => r.Property == "AudioTag.Lyrics");
            Assert.False(lyricsRow.NewValue.Contains('\n', StringComparison.Ordinal));
            Assert.Contains("\\n", lyricsRow.NewValue, StringComparison.Ordinal);
        }

        /// <summary>
        /// When native tag bytes differ but projected semantics match, emit a compact native summary row.
        /// </summary>
        [Fact]
        public void BuildChangeRows_AudioTagNativeXiphChange_AppendsSummaryRow()
        {
            var original = _CloneBaseline(configureOverlay: o =>
            {
                o.AudioTagOverlay.Xiph = new SerializedTagBlob { CanonicalTagBytes = [1, 2] };
            });
            var item = new RenameItem(original);
            item.Preview.AudioTagOverlay.Xiph = new SerializedTagBlob { CanonicalTagBytes = [1, 2, 3] };

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            var row = Assert.Single(rows);
            Assert.Equal("AudioTag.Native.Xiph", row.Property);
            Assert.Contains("2 bytes", row.OldValue, StringComparison.Ordinal);
            Assert.Contains("3 bytes", row.NewValue, StringComparison.Ordinal);
        }

        /// <summary>
        /// Native block rows follow merged embedded-tag scalar rows in stable order.
        /// </summary>
        [Fact]
        public void BuildChangeRows_MixedScalarAndNativeBlock_FollowsStableOrdering()
        {
            var original = _CloneBaseline(directoryPath: @"D:\A", configureOverlay: o =>
            {
                o.AudioTagOverlay.Xiph = new SerializedTagBlob { CanonicalTagBytes = [1] };
            });
            var item = new RenameItem(original);
            item.Preview.DirectoryPath = @"D:\B";
            item.Preview.AudioTagOverlay.Xiph = new SerializedTagBlob { CanonicalTagBytes = [1, 2] };
            var pv = item.Preview.AudioTagOverlay;
            AudioTagPersistence.MergeSemanticOntoNativeBlocks(
                pv,
                AudioTagSemanticSurface.FromBlocks(pv) with { Genre = "Rock" },
                embeddedTagSourcePath: null);

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            Assert.Equal(
                ["DirectoryPath", "AudioTag.Genre", "AudioTag.Native.Xiph"],
                [.. rows.Select(r => r.Property)]);
        }

        /// <summary>
        /// Rows follow structured path fields, then filesystem scalars, then stable audio-tag field order.
        /// </summary>
        [Fact]
        public void BuildChangeRows_MixedDifferences_FollowsStableCategoryOrdering()
        {
            var original = _CloneBaseline(directoryPath: @"D:\A");
            var item = new RenameItem(original);
            item.Preview.DirectoryPath = @"D:\B";
            item.Preview.Attributes = FileAttributes.ReadOnly;
            var pv = item.Preview.AudioTagOverlay;
            AudioTagPersistence.MergeSemanticOntoNativeBlocks(
                pv,
                AudioTagSemanticSurface.FromBlocks(pv) with { Genre = "Rock" },
                embeddedTagSourcePath: null);

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            Assert.Equal(
                ["DirectoryPath", "Attributes", "AudioTag.Genre", "AudioTag.Native.Id3v2"],
                [.. rows.Select(r => r.Property)]);
        }

        /// <summary>
        /// Strip-all flag changes surface after timestamp rows in the scalar section.
        /// </summary>
        [Fact]
        public void BuildChangeRows_StripAllEmbeddedTagsOnCommit_FlagEmitsRow()
        {
            var original = _CloneBaseline();
            var item = new RenameItem(original)
            {
                StripAllEmbeddedTagsOnCommit = true
            };

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            var stripRow = Assert.Single(rows);
            Assert.Equal("StripAllEmbeddedTagsOnCommit", stripRow.Property);
            Assert.Equal(JsonSerializer.Serialize(false), stripRow.OldValue);
            Assert.Equal(JsonSerializer.Serialize(true), stripRow.NewValue);
        }

        private static FileMeta _CloneBaseline(
            string? directoryPath = null,
            DateTime? creationTime = null,
            DateTime? lastWriteTime = null,
            DateTime? lastAccessTime = null,
            Action<FileMeta>? configureOverlay = null)
        {
            var testItem = FilterTestHelpers.CreateRenameItem(
                prefix: "song",
                extension: ".mp3",
                directory: directoryPath ?? @"D:\In",
                attributes: FileAttributes.Normal,
                creationTime: creationTime ?? new DateTime(2024, 6, 1, 12, 30, 45, DateTimeKind.Unspecified),
                lastWriteTime: lastWriteTime ?? new DateTime(2024, 6, 1, 12, 30, 46, DateTimeKind.Unspecified),
                lastAccessTime: lastAccessTime ?? new DateTime(2024, 6, 1, 12, 30, 47, DateTimeKind.Unspecified),
                configureOriginal: configureOverlay);

            return testItem.Original.Clone();
        }
    }
}
