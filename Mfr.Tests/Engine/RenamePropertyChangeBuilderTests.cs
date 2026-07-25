using System.Text.Json;
using Mfr.Engine;
using Mfr.Metadata;
using Mfr.Models;
using Mfr.Models.Tags;
using Mfr.Tests.Models.Filters;

namespace Mfr.Tests.Engine
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
        /// Embedded tag field rows use block-level keys; absent values are the literal <c>absent</c>.
        /// </summary>
        [Fact]
        public void BuildChangeRows_AudioTagTitleChange_EmitsId3v2Tit2Row()
        {
            var original = _CloneBaseline();
            var item = new RenameItem(original);
            var pv = item.Preview.AudioTagOverlay;
            AudioTagPersistence.MergeSemanticIntoBlocks(
                pv,
                CommonAudioTag.FromOverlay(pv) with { Title = "Next" },
                embeddedTagSourcePath: null);

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            var titleRow = Assert.Single(rows);
            Assert.Equal("AudioTag.Block.Id3v2.TIT2", titleRow.Property);
            Assert.Equal("absent", titleRow.OldValue);
            Assert.Equal("Next", titleRow.NewValue);
        }

        /// <summary>
        /// Year edits surface as the modeled ID3v2 year frame (TYER for v2.3).
        /// </summary>
        [Fact]
        public void BuildChangeRows_AudioTagYearChange_EmitsId3v2TyerRow()
        {
            var original = _CloneBaseline(configureOverlay: o =>
                o.AudioTagOverlay = AudioTagOverlayTestBuilder.Id3Overlay(year: 1999));
            var item = new RenameItem(original);
            var pv = item.Preview.AudioTagOverlay;
            AudioTagPersistence.MergeSemanticIntoBlocks(
                pv,
                CommonAudioTag.FromOverlay(pv) with { Year = 2001 },
                embeddedTagSourcePath: null);

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            var yearRow = Assert.Single(rows);
            Assert.Equal("AudioTag.Block.Id3v2.TYER", yearRow.Property);
            Assert.Equal("1999", yearRow.OldValue);
            Assert.Equal("2001", yearRow.NewValue);
        }

        /// <summary>
        /// Multiline lyrics stay on one value line (frame text); newlines are preserved in the value string.
        /// </summary>
        [Fact]
        public void BuildChangeRows_AudioTagLyricsWithNewline_EmitsPrimaryUsltRow()
        {
            var original = _CloneBaseline();
            var item = new RenameItem(original);
            var pv = item.Preview.AudioTagOverlay;
            AudioTagPersistence.MergeSemanticIntoBlocks(
                pv,
                CommonAudioTag.FromOverlay(pv) with { Lyrics = "a\nb" },
                embeddedTagSourcePath: null);

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            var lyricsRow = Assert.Single(rows);
            Assert.Equal("AudioTag.Block.Id3v2.USLT[eng|]", lyricsRow.Property);
            Assert.Equal("absent", lyricsRow.OldValue);
            Assert.Equal("a\nb", lyricsRow.NewValue);
        }

        /// <summary>
        /// When Xiph field keys differ but projected semantics match, emit per-key block rows.
        /// </summary>
        [Fact]
        public void BuildChangeRows_AudioTagBlockXiphChange_AppendsFieldRows()
        {
            var original = _CloneBaseline(configureOverlay: o =>
            {
                o.AudioTagOverlay.Xiph = new XiphTagData
                {
                    Fields = [new TextFieldRow("DATE", ["1999"])],
                };
            });
            var item = new RenameItem(original);
            // Same projected year via alternate known key — field layout differs, semantics align.
            item.Preview.AudioTagOverlay.Xiph = new XiphTagData
            {
                Fields = [new TextFieldRow("YEAR", ["1999"])],
            };

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            Assert.Equal(2, rows.Count);
            Assert.Contains(rows, static r => r.Property == "AudioTag.Block.Xiph.DATE" && r.NewValue == "absent");
            Assert.Contains(rows, static r => r.Property == "AudioTag.Block.Xiph.YEAR" && r.OldValue == "absent");
        }

        /// <summary>
        /// Block field rows follow path rows in stable category order.
        /// </summary>
        [Fact]
        public void BuildChangeRows_MixedPathAndBlock_FollowsStableOrdering()
        {
            var original = _CloneBaseline(directoryPath: @"D:\A", configureOverlay: o =>
            {
                o.AudioTagOverlay.Xiph = new XiphTagData
                {
                    Fields = [new TextFieldRow("TITLE", ["a"])],
                };
            });
            var item = new RenameItem(original);
            item.Preview.DirectoryPath = @"D:\B";
            item.Preview.AudioTagOverlay.Xiph = new XiphTagData
            {
                Fields = [new TextFieldRow("TITLE", ["a"])],
            };
            var pv = item.Preview.AudioTagOverlay;
            AudioTagPersistence.MergeSemanticIntoBlocks(
                pv,
                CommonAudioTag.FromOverlay(pv) with { Genre = "Rock" },
                embeddedTagSourcePath: null);

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            Assert.Equal(
                ["DirectoryPath", "AudioTag.Block.Xiph.GENRE"],
                [.. rows.Select(r => r.Property)]);
        }

        /// <summary>
        /// Rows follow structured path fields, then filesystem scalars, then block field order.
        /// </summary>
        [Fact]
        public void BuildChangeRows_MixedDifferences_FollowsStableCategoryOrdering()
        {
            var original = _CloneBaseline(directoryPath: @"D:\A");
            var item = new RenameItem(original);
            item.Preview.DirectoryPath = @"D:\B";
            item.Preview.Attributes = FileAttributes.ReadOnly;
            var pv = item.Preview.AudioTagOverlay;
            AudioTagPersistence.MergeSemanticIntoBlocks(
                pv,
                CommonAudioTag.FromOverlay(pv) with { Genre = "Rock" },
                embeddedTagSourcePath: null);

            var rows = RenamePropertyChangeBuilder.BuildChangeRows(item);

            Assert.Equal(
                ["DirectoryPath", "Attributes", "AudioTag.Block.Id3v2.TCON"],
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
