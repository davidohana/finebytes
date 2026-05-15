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
        /// Builds change rows for a committed item using path-derived file names plus scalar and tag deltas.
        /// </summary>
        /// <param name="sourcePath">Original source path.</param>
        /// <param name="destinationPath">Destination path.</param>
        /// <param name="originalSnapshot">Original metadata before commit.</param>
        /// <param name="previewSnapshot">Preview metadata to apply.</param>
        /// <returns>Property-level changes for result reporting.</returns>
        internal static List<RenamePropertyChange> BuildCommitChanges(
            string sourcePath,
            string destinationPath,
            FileMeta originalSnapshot,
            FileMeta previewSnapshot)
        {
            var changes = new List<RenamePropertyChange>();
            _AppendFileNameDifference(changes, sourcePath, destinationPath);
            _AppendFileMetaScalarDifferences(changes, originalSnapshot, previewSnapshot);
            _AppendAudioTagOverlayDifferences(
                changes,
                originalSnapshot.AudioTagOverlay,
                previewSnapshot.AudioTagOverlay);
            return changes;
        }

        /// <summary>
        /// Returns structured path, filesystem scalar, and embedded audio-tag deltas between <see cref="RenameItem.Original"/> and <see cref="RenameItem.Preview"/>.
        /// </summary>
        /// <param name="renameItem">The item to inspect.</param>
        /// <returns>Property-level changes; may be empty when paths differ only in ways not modeled here.</returns>
        internal static IReadOnlyList<RenamePropertyChange> GetPreviewPropertyChanges(RenameItem renameItem)
        {
            return _BuildPreviewChanges(renameItem.Original, renameItem.Preview);
        }

        /// <summary>Builds preview rows from structured path fields plus scalar and tag deltas.</summary>
        private static List<RenamePropertyChange> _BuildPreviewChanges(
            FileMeta originalSnapshot,
            FileMeta previewSnapshot)
        {
            var changes = new List<RenamePropertyChange>();
            _AppendStructuredPathDifferences(changes, originalSnapshot, previewSnapshot);
            _AppendFileMetaScalarDifferences(changes, originalSnapshot, previewSnapshot);
            _AppendAudioTagOverlayDifferences(
                changes,
                originalSnapshot.AudioTagOverlay,
                previewSnapshot.AudioTagOverlay);
            return changes;
        }

        /// <summary>Appends prefix, extension, and directory deltas between two snapshots.</summary>
        private static void _AppendStructuredPathDifferences(
            List<RenamePropertyChange> changes,
            FileMeta original,
            FileMeta preview)
        {
            var prefixChanged = !string.Equals(original.Prefix, preview.Prefix, StringComparison.Ordinal);
            if (prefixChanged)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "Prefix",
                    OldValue: original.Prefix,
                    NewValue: preview.Prefix));
            }

            var extensionChanged = !string.Equals(original.Extension, preview.Extension, StringComparison.Ordinal);
            if (extensionChanged)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "Extension",
                    OldValue: original.Extension,
                    NewValue: preview.Extension));
            }

            var directoryChanged = !string.Equals(
                original.DirectoryPath,
                preview.DirectoryPath,
                StringComparison.OrdinalIgnoreCase);
            if (directoryChanged)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "DirectoryPath",
                    OldValue: original.DirectoryPath,
                    NewValue: preview.DirectoryPath));
            }
        }

        /// <summary>Appends a file-name delta derived from full paths (commit reporting).</summary>
        private static void _AppendFileNameDifference(
            List<RenamePropertyChange> changes,
            string sourcePath,
            string destinationPath)
        {
            var sourceFileName = Path.GetFileName(sourcePath);
            var destinationFileName = Path.GetFileName(destinationPath);
            var fileNameChanged = !string.Equals(sourceFileName, destinationFileName, StringComparison.Ordinal);
            if (!fileNameChanged)
                return;

            changes.Add(new RenamePropertyChange(
                Property: "FileName",
                OldValue: sourceFileName,
                NewValue: destinationFileName));
        }

        /// <summary>Appends attributes and timestamp deltas between two snapshots.</summary>
        private static void _AppendFileMetaScalarDifferences(
            List<RenamePropertyChange> changes,
            FileMeta original,
            FileMeta preview)
        {
            if (original.Attributes != preview.Attributes)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "Attributes",
                    OldValue: original.Attributes.ToString(),
                    NewValue: preview.Attributes.ToString()));
            }

            if (original.CreationTime != preview.CreationTime)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "CreationTime",
                    OldValue: original.CreationTime.ToString("O"),
                    NewValue: preview.CreationTime.ToString("O")));
            }

            if (original.LastWriteTime != preview.LastWriteTime)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "LastWriteTime",
                    OldValue: original.LastWriteTime.ToString("O"),
                    NewValue: preview.LastWriteTime.ToString("O")));
            }

            if (original.LastAccessTime != preview.LastAccessTime)
            {
                changes.Add(new RenamePropertyChange(
                    Property: "LastAccessTime",
                    OldValue: original.LastAccessTime.ToString("O"),
                    NewValue: preview.LastAccessTime.ToString("O")));
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

            _AppendAudioTagOverlayFieldChanges(changes, original, preview);
        }

        private static void _AppendAudioTagOverlayFieldChanges(
            List<RenamePropertyChange> changes,
            AudioTagOverlay original,
            AudioTagOverlay preview)
        {
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
