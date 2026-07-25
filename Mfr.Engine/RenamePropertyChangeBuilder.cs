using System.Text.Json;
using Mfr.Metadata;
using Mfr.Models;
using Mfr.Models.Tags;

namespace Mfr.Engine
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

            var originalSemantic = CommonAudioTag.FromOverlay(original);
            var previewSemantic = CommonAudioTag.FromOverlay(preview);

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
        /// Appends compact rows when structured per–tag snapshots differ (presence and field-level deltas).
        /// </summary>
        private static void _AppendAudioTagNativeBlockLayoutDifferences(
            List<RenamePropertyChange> changes,
            AudioTagOverlay original,
            AudioTagOverlay preview)
        {
            if (original.TagBlocksStructurallyEquals(preview))
                return;

            _AppendBlockPresenceAndFieldDiffs(
                changes,
                "AudioTag.Native.Id3v1",
                original.Id3v1,
                preview.Id3v1,
                _DiffId3v1Fields);
            _AppendBlockPresenceAndFieldDiffs(
                changes,
                "AudioTag.Native.Id3v2",
                original.Id3v2,
                preview.Id3v2,
                _DiffId3v2Fields);
            _AppendBlockPresenceAndFieldDiffs(
                changes,
                "AudioTag.Native.Xiph",
                original.Xiph,
                preview.Xiph,
                _DiffXiphFields);
            _AppendBlockPresenceAndFieldDiffs(
                changes,
                "AudioTag.Native.Ape",
                original.Ape,
                preview.Ape,
                _DiffApeFields);
            _AppendBlockPresenceAndFieldDiffs(
                changes,
                "AudioTag.Native.RiffInfo",
                original.RiffInfo,
                preview.RiffInfo,
                _DiffRiffInfoFields);
            _AppendBlockPresenceAndFieldDiffs(
                changes,
                "AudioTag.Native.Apple",
                original.Apple,
                preview.Apple,
                _DiffAppleFields);
            _AppendBlockPresenceAndFieldDiffs(
                changes,
                "AudioTag.Native.Asf",
                original.Asf,
                preview.Asf,
                _DiffAsfFields);
        }

        private static void _AppendBlockPresenceAndFieldDiffs<T>(
            List<RenamePropertyChange> changes,
            string blockProperty,
            T? original,
            T? preview,
            Action<List<RenamePropertyChange>, string, T, T> appendFieldDiffs)
            where T : class
        {
            if (Equals(original, preview))
                return;

            if (original is null || preview is null)
            {
                changes.Add(new RenamePropertyChange(
                    Property: blockProperty,
                    OldValue: original is null ? "absent" : "present",
                    NewValue: preview is null ? "absent" : "present"));
                return;
            }

            appendFieldDiffs(changes, blockProperty, original, preview);
        }

        private static void _DiffId3v1Fields(
            List<RenamePropertyChange> changes,
            string blockProperty,
            Id3v1TagData original,
            Id3v1TagData preview)
        {
            _AddNativeStringDiff(changes, blockProperty + ".Title", original.Title, preview.Title);
            _AddNativeStringDiff(changes, blockProperty + ".Artist", original.Artist, preview.Artist);
            _AddNativeStringDiff(changes, blockProperty + ".Album", original.Album, preview.Album);
            _AddNativeStringDiff(changes, blockProperty + ".Comment", original.Comment, preview.Comment);
            if (original.Year != preview.Year)
            {
                changes.Add(new RenamePropertyChange(
                    Property: blockProperty + ".Year",
                    OldValue: JsonSerializer.Serialize(original.Year),
                    NewValue: JsonSerializer.Serialize(preview.Year)));
            }

            if (original.Track != preview.Track)
            {
                changes.Add(new RenamePropertyChange(
                    Property: blockProperty + ".Track",
                    OldValue: JsonSerializer.Serialize(original.Track),
                    NewValue: JsonSerializer.Serialize(preview.Track)));
            }

            if (original.Genre != preview.Genre)
            {
                changes.Add(new RenamePropertyChange(
                    Property: blockProperty + ".Genre",
                    OldValue: JsonSerializer.Serialize(original.Genre),
                    NewValue: JsonSerializer.Serialize(preview.Genre)));
            }
        }

        private static void _DiffId3v2Fields(
            List<RenamePropertyChange> changes,
            string blockProperty,
            Id3v2TagData original,
            Id3v2TagData preview)
        {
            if (original.Version != preview.Version)
            {
                changes.Add(new RenamePropertyChange(
                    Property: blockProperty + ".Version",
                    OldValue: original.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    NewValue: preview.Version.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }

            var originalById = original.Frames.ToDictionary(_FrameDiffKey, StringComparer.Ordinal);
            var previewById = preview.Frames.ToDictionary(_FrameDiffKey, StringComparer.Ordinal);

            foreach (var key in originalById.Keys.Union(previewById.Keys).Order(StringComparer.Ordinal))
            {
                originalById.TryGetValue(key, out var oldFrame);
                previewById.TryGetValue(key, out var newFrame);
                if (Equals(oldFrame, newFrame))
                    continue;

                changes.Add(new RenamePropertyChange(
                    Property: blockProperty + "." + key,
                    OldValue: oldFrame is null ? "absent" : string.Join("; ", oldFrame.TextValues),
                    NewValue: newFrame is null ? "absent" : string.Join("; ", newFrame.TextValues)));
            }
        }

        private static string _FrameDiffKey(Id3v2ModeledFrame frame)
        {
            if (frame.FrameId is "COMM" or "USLT" or "TXXX")
            {
                return frame.FrameId
                    + "["
                    + (frame.Language ?? string.Empty)
                    + "|"
                    + (frame.Description ?? string.Empty)
                    + "]";
            }

            return frame.FrameId;
        }

        private static void _DiffXiphFields(
            List<RenamePropertyChange> changes,
            string blockProperty,
            XiphTagData original,
            XiphTagData preview)
        {
            _DiffTextFieldRows(changes, blockProperty, original.Fields, preview.Fields);
        }

        private static void _DiffApeFields(
            List<RenamePropertyChange> changes,
            string blockProperty,
            ApeTagData original,
            ApeTagData preview)
        {
            _DiffTextFieldRows(changes, blockProperty, original.Fields, preview.Fields);
        }

        private static void _DiffTextFieldRows(
            List<RenamePropertyChange> changes,
            string blockProperty,
            System.Collections.Immutable.ImmutableArray<TextFieldRow> original,
            System.Collections.Immutable.ImmutableArray<TextFieldRow> preview)
        {
            var originalMap = original.ToDictionary(r => r.Key, r => r.Values, StringComparer.Ordinal);
            var previewMap = preview.ToDictionary(r => r.Key, r => r.Values, StringComparer.Ordinal);

            foreach (var key in originalMap.Keys.Union(previewMap.Keys).Order(StringComparer.Ordinal))
            {
                originalMap.TryGetValue(key, out var oldValues);
                previewMap.TryGetValue(key, out var newValues);
                var oldText = oldValues.IsDefaultOrEmpty ? null : string.Join("; ", oldValues);
                var newText = newValues.IsDefaultOrEmpty ? null : string.Join("; ", newValues);
                if (string.Equals(oldText, newText, StringComparison.Ordinal))
                    continue;

                changes.Add(new RenamePropertyChange(
                    Property: blockProperty + "." + key,
                    OldValue: oldText ?? "absent",
                    NewValue: newText ?? "absent"));
            }
        }

        private static void _DiffRiffInfoFields(
            List<RenamePropertyChange> changes,
            string blockProperty,
            RiffInfoTagData original,
            RiffInfoTagData preview)
        {
            var originalMap = original.Fields.ToDictionary(r => r.Key, r => r.Value, StringComparer.Ordinal);
            var previewMap = preview.Fields.ToDictionary(r => r.Key, r => r.Value, StringComparer.Ordinal);

            foreach (var key in originalMap.Keys.Union(previewMap.Keys).Order(StringComparer.Ordinal))
            {
                originalMap.TryGetValue(key, out var oldValue);
                previewMap.TryGetValue(key, out var newValue);
                if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                    continue;

                changes.Add(new RenamePropertyChange(
                    Property: blockProperty + "." + key,
                    OldValue: oldValue ?? "absent",
                    NewValue: newValue ?? "absent"));
            }
        }

        private static void _DiffAppleFields(
            List<RenamePropertyChange> changes,
            string blockProperty,
            AppleTagData original,
            AppleTagData preview)
        {
            var originalMap = original.Atoms.ToDictionary(
                a => Convert.ToHexString(a.AtomType.AsSpan()),
                a => string.Join("; ", a.Values),
                StringComparer.Ordinal);
            var previewMap = preview.Atoms.ToDictionary(
                a => Convert.ToHexString(a.AtomType.AsSpan()),
                a => string.Join("; ", a.Values),
                StringComparer.Ordinal);

            foreach (var key in originalMap.Keys.Union(previewMap.Keys).Order(StringComparer.Ordinal))
            {
                originalMap.TryGetValue(key, out var oldValue);
                previewMap.TryGetValue(key, out var newValue);
                if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                    continue;

                changes.Add(new RenamePropertyChange(
                    Property: blockProperty + "." + key,
                    OldValue: oldValue ?? "absent",
                    NewValue: newValue ?? "absent"));
            }
        }

        private static void _DiffAsfFields(
            List<RenamePropertyChange> changes,
            string blockProperty,
            AsfTagData original,
            AsfTagData preview)
        {
            var originalMap = original.Descriptors.ToDictionary(r => r.Name, r => r.Value, StringComparer.Ordinal);
            var previewMap = preview.Descriptors.ToDictionary(r => r.Name, r => r.Value, StringComparer.Ordinal);

            foreach (var key in originalMap.Keys.Union(previewMap.Keys).Order(StringComparer.Ordinal))
            {
                originalMap.TryGetValue(key, out var oldValue);
                previewMap.TryGetValue(key, out var newValue);
                if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                    continue;

                changes.Add(new RenamePropertyChange(
                    Property: blockProperty + "." + key,
                    OldValue: oldValue ?? "absent",
                    NewValue: newValue ?? "absent"));
            }
        }

        private static void _AddNativeStringDiff(
            List<RenamePropertyChange> changes,
            string property,
            string? oldValue,
            string? newValue)
        {
            if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
                return;

            changes.Add(new RenamePropertyChange(
                Property: property,
                OldValue: oldValue ?? "absent",
                NewValue: newValue ?? "absent"));
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
