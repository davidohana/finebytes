using System.Text.Json;
using Mfr.Models;

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

            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Title", original.Title, preview.Title);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Album", original.Album, preview.Album);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Performers", original.Performers, preview.Performers);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.AlbumArtists", original.AlbumArtists, preview.AlbumArtists);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Composers", original.Composers, preview.Composers);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Genre", original.Genre, preview.Genre);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Comment", original.Comment, preview.Comment);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Lyrics", original.Lyrics, preview.Lyrics);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Copyright", original.Copyright, preview.Copyright);
            _AddRenamePropertyChangeIfOverlayStringDiffers(changes, "AudioTag.Grouping", original.Grouping, preview.Grouping);
            _AddRenamePropertyChangeIfOverlayUIntDiffers(changes, "AudioTag.Year", original.Year, preview.Year);
            _AddRenamePropertyChangeIfOverlayUIntDiffers(changes, "AudioTag.Track", original.Track, preview.Track);
            _AddRenamePropertyChangeIfOverlayUIntDiffers(changes, "AudioTag.TrackCount", original.TrackCount, preview.TrackCount);
            _AddRenamePropertyChangeIfOverlayUIntDiffers(changes, "AudioTag.Disc", original.Disc, preview.Disc);
            _AddRenamePropertyChangeIfOverlayUIntDiffers(changes, "AudioTag.DiscCount", original.DiscCount, preview.DiscCount);
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
