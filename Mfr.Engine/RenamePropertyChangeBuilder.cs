using System.Text.Json;
using Mfr.Metadata;
using Mfr.Models;
using Mfr.Models.Tags;

namespace Mfr.Core
{
    /// <summary>
    /// Builds <see cref="RenamePropertyChange"/> rows shared between preview formatting and commit outcomes.
    /// </summary>
    internal static class RenamePropertyChangeBuilder
    {
        /// <summary>
        /// Builds property-change rows from original/preview snapshots (structured path, filesystem scalars, embedded tags).
        /// </summary>
        /// <para>
        /// Used for successful commit outcomes and for preview logging/console output so both surfaces stay aligned.
        /// </para>
        /// <param name="renameItem">Rename row holding original and preview metadata plus embedded-strip commit intent.</param>
        /// <returns>Ordered property-level deltas.</returns>
        internal static List<RenamePropertyChange> BuildChangeRows(RenameItem renameItem)
        {
            ArgumentNullException.ThrowIfNull(renameItem);
            var changes = new List<RenamePropertyChange>();
            _AppendStructuredPathDifferences(changes, renameItem.Original, renameItem.Preview);
            _AppendFileMetaScalarDifferences(
                changes,
                renameItem.Original,
                renameItem.Preview,
                renameItem.StripAllEmbeddedTagsOnCommit);
            _AppendAudioTagOverlayDifferences(
                changes,
                renameItem.Original.AudioTagOverlay,
                renameItem.Preview.AudioTagOverlay);
            return changes;
        }

        /// <summary>Appends prefix, extension, and directory deltas between two snapshots.</summary>
        private static void _AppendStructuredPathDifferences(
            List<RenamePropertyChange> changes,
            FileMeta original,
            FileMeta preview)
        {
            _AddRenamePropertyChangeIfStringDiffers(
                changes,
                propertyName: "Prefix",
                oldValue: original.Prefix,
                newValue: preview.Prefix,
                comparison: StringComparison.Ordinal);
            _AddRenamePropertyChangeIfStringDiffers(
                changes,
                propertyName: "Extension",
                oldValue: original.Extension,
                newValue: preview.Extension,
                comparison: StringComparison.Ordinal);
            _AddRenamePropertyChangeIfStringDiffers(
                changes,
                propertyName: "DirectoryPath",
                oldValue: original.DirectoryPath,
                newValue: preview.DirectoryPath,
                comparison: StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Appends attributes and timestamp deltas between two snapshots.</summary>
        private static void _AppendFileMetaScalarDifferences(
            List<RenamePropertyChange> changes,
            FileMeta original,
            FileMeta preview,
            bool stripAllEmbeddedTagsOnCommit)
        {
            if (original.Attributes != preview.Attributes)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "Attributes",
                    OldValue: original.Attributes.ToString(),
                    NewValue: preview.Attributes.ToString()));
            }

            _AddRenamePropertyChangeIfLocalTimestampDiffers(
                changes,
                propertyName: "CreationTime",
                originalValue: original.CreationTime,
                previewValue: preview.CreationTime);
            _AddRenamePropertyChangeIfLocalTimestampDiffers(
                changes,
                propertyName: "LastWriteTime",
                originalValue: original.LastWriteTime,
                previewValue: preview.LastWriteTime);
            _AddRenamePropertyChangeIfLocalTimestampDiffers(
                changes,
                propertyName: "LastAccessTime",
                originalValue: original.LastAccessTime,
                previewValue: preview.LastAccessTime);

            if (stripAllEmbeddedTagsOnCommit)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "StripAllEmbeddedTagsOnCommit",
                    OldValue: JsonSerializer.Serialize(false),
                    NewValue: JsonSerializer.Serialize(true)));
            }
        }

        /// <summary>Appends one row per differing scalar embedded-tag field.</summary>
        private static void _AppendAudioTagOverlayDifferences(
            List<RenamePropertyChange> changes,
            AudioTagOverlay original,
            AudioTagOverlay preview)
        {
            if (original.Equals(preview))
                return;

            var originalSemantic = AudioTagSemanticSurface.FromBlocks(original);
            var previewSemantic = AudioTagSemanticSurface.FromBlocks(preview);

            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Title", originalSemantic.Title, previewSemantic.Title);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Album", originalSemantic.Album, previewSemantic.Album);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Performers", originalSemantic.Performers, previewSemantic.Performers);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.AlbumArtists", originalSemantic.AlbumArtists, previewSemantic.AlbumArtists);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Composers", originalSemantic.Composers, previewSemantic.Composers);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Genre", originalSemantic.Genre, previewSemantic.Genre);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Comment", originalSemantic.Comment, previewSemantic.Comment);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Lyrics", originalSemantic.Lyrics, previewSemantic.Lyrics);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Copyright", originalSemantic.Copyright, previewSemantic.Copyright);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Grouping", originalSemantic.Grouping, previewSemantic.Grouping);
            _AddRenamePropertyChangeIfOverlayUIntDiffers(changes, "AudioTag.Year", originalSemantic.Year, previewSemantic.Year);
            _AddRenamePropertyChangeIfOverlayUIntDiffers(changes, "AudioTag.Track", originalSemantic.Track, previewSemantic.Track);
            _AddRenamePropertyChangeIfOverlayUIntDiffers(changes, "AudioTag.TrackCount", originalSemantic.TrackCount, previewSemantic.TrackCount);
            _AddRenamePropertyChangeIfOverlayUIntDiffers(changes, "AudioTag.Disc", originalSemantic.Disc, previewSemantic.Disc);
            _AddRenamePropertyChangeIfOverlayUIntDiffers(changes, "AudioTag.DiscCount", originalSemantic.DiscCount, previewSemantic.DiscCount);

            _AppendAudioTagNativeBlockLayoutDifferences(changes, original, preview);
        }

        /// <summary>
        /// Appends compact rows when structured per–tag snapshots differ.
        /// </summary>
        private static void _AppendAudioTagNativeBlockLayoutDifferences(
            List<RenamePropertyChange> changes,
            AudioTagOverlay original,
            AudioTagOverlay preview)
        {
            if (original.TagBlocksStructurallyEquals(preview))
                return;

            if (!Equals(original.Id3v1, preview.Id3v1))
            {
                changes.Add(new RenamePropertyChange(
                    Property: "AudioTag.Native.Id3v1",
                    OldValue: JsonSerializer.Serialize(original.Id3v1),
                    NewValue: JsonSerializer.Serialize(preview.Id3v1)));
            }

            if (!Equals(original.Id3v2, preview.Id3v2))
            {
                changes.Add(new RenamePropertyChange(
                    Property: "AudioTag.Native.Id3v2",
                    OldValue: _SummarizeId3v2Block(original.Id3v2),
                    NewValue: _SummarizeId3v2Block(preview.Id3v2)));
            }

            if (!Equals(original.Xiph, preview.Xiph))
            {
                changes.Add(new RenamePropertyChange(
                    Property: "AudioTag.Native.Xiph",
                    OldValue: _SummarizeSerializedBlob(original.Xiph),
                    NewValue: _SummarizeSerializedBlob(preview.Xiph)));
            }

            if (!Equals(original.Ape, preview.Ape))
            {
                changes.Add(new RenamePropertyChange(
                    Property: "AudioTag.Native.Ape",
                    OldValue: _SummarizeSerializedBlob(original.Ape),
                    NewValue: _SummarizeSerializedBlob(preview.Ape)));
            }

            if (!Equals(original.RiffInfo, preview.RiffInfo))
            {
                changes.Add(new RenamePropertyChange(
                    Property: "AudioTag.Native.RiffInfo",
                    OldValue: _SummarizeSerializedBlob(original.RiffInfo),
                    NewValue: _SummarizeSerializedBlob(preview.RiffInfo)));
            }

            if (!Equals(original.Apple, preview.Apple))
            {
                changes.Add(new RenamePropertyChange(
                    Property: "AudioTag.Native.Apple",
                    OldValue: _SummarizeAppleBlock(original.Apple),
                    NewValue: _SummarizeAppleBlock(preview.Apple)));
            }

            if (!Equals(original.Asf, preview.Asf))
            {
                changes.Add(new RenamePropertyChange(
                    Property: "AudioTag.Native.Asf",
                    OldValue: _SummarizeAsfBlock(original.Asf),
                    NewValue: _SummarizeAsfBlock(preview.Asf)));
            }
        }

        private static string _SummarizeId3v2Block(Id3v2TagData? data)
        {
            if (data is null)
                return "absent";

            return $"{data.Frames.Length} frames, {data.CanonicalTagBytes.Length} canonical bytes (ID3v2 v{data.Version})";
        }

        private static string _SummarizeSerializedBlob(SerializedTagBlob? blob)
        {
            if (blob is null)
                return "absent";

            return $"{blob.CanonicalTagBytes.Length} bytes";
        }

        private static string _SummarizeAppleBlock(AppleTagData? data)
        {
            if (data is null)
                return "absent";

            return $"{data.Atoms.Length} atoms";
        }

        private static string _SummarizeAsfBlock(AsfTagData? data)
        {
            if (data is null)
                return "absent";

            return $"{data.Descriptors.Length} descriptors";
        }

        private static void _AddRenamePropertyChangeIfStringDiffers(
            List<RenamePropertyChange> changes,
            string propertyName,
            string oldValue,
            string newValue,
            StringComparison comparison)
        {
            if (string.Equals(oldValue, newValue, comparison))
                return;

            changes.Add(new RenamePropertyChange(
                Property: propertyName,
                OldValue: oldValue,
                NewValue: newValue));
        }

        private static void _AddRenamePropertyChangeIfLocalTimestampDiffers(
            List<RenamePropertyChange> changes,
            string propertyName,
            DateTime originalValue,
            DateTime previewValue)
        {
            if (originalValue == previewValue)
                return;

            changes.Add(new RenamePropertyChange(
                Property: propertyName,
                OldValue: originalValue.ToString("O"),
                NewValue: previewValue.ToString("O")));
        }

        private static void _AddRenamePropertyChangeIfOverlayStringDiffers(
            List<RenamePropertyChange> changes,
            string propertyName,
            string? oldValue,
            string? newValue)
        {
            if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                return;

            changes.Add(new RenamePropertyChange(
                Property: propertyName,
                OldValue: JsonSerializer.Serialize(oldValue),
                NewValue: JsonSerializer.Serialize(newValue)));
        }

        private static void _AddRenamePropertyChangeIfOverlayUIntDiffers(
            List<RenamePropertyChange> changes,
            string propertyName,
            uint? oldValue,
            uint? newValue)
        {
            if (oldValue == newValue)
                return;

            changes.Add(new RenamePropertyChange(
                Property: propertyName,
                OldValue: JsonSerializer.Serialize(oldValue),
                NewValue: JsonSerializer.Serialize(newValue)));
        }
    }
}
