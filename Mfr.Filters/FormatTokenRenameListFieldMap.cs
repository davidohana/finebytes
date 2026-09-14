using System.Diagnostics;
using Mfr.Models.RenameList.Fields.AudioTag;
using Mfr.Models.RenameList.Fields.Basic;
using Mfr.Models.RenameList.Fields.Extended;
using Mfr.Models.RenameList.Fields.Image;
using Mfr.Models.RenameList.Fields.Jpeg;
using Mfr.Models.RenameList.Fields.Media;
using Mfr.Models.RenameList.Fields.Mpeg;

namespace Mfr.Filters
{
    /// <summary>
    /// Maps formatter token canonical names to Rename List catalog fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Meta/session/generator tokens (<c>counter</c>, <c>substr</c>, <c>token</c>, <c>now</c>, …) are omitted.
    /// Generic <c>exif</c> (tag-id lookup) is omitted — only named <c>exif-*</c> / <c>exif-date</c> map.
    /// </para>
    /// </remarks>
    internal static class FormatTokenRenameListFieldMap
    {
        /// <summary>
        /// Case-insensitive <c>file-date</c> date-kind keywords (same as <c>FileDateToken</c> / preset JSON).
        /// </summary>
        private static readonly Dictionary<string, TimestampField> _fileDateKindToField = new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            ["creation"] = TimestampField.Creation,
            ["lastWrite"] = TimestampField.LastWrite,
            ["lastAccess"] = TimestampField.LastAccess,
        };

        private static readonly Dictionary<string, (string GroupId, string PropertyKey)> _canonicalNameToField = new(
            StringComparer.Ordinal
        )
        {
            // Basic / file name
            ["file-or-folder"] = (BasicRenameListField.Group, BasicRenameListFields.Key.ItemType),
            ["file-name"] = (BasicRenameListField.Group, BasicRenameListFields.Key.Name),
            ["file-extension"] = (BasicRenameListField.Group, BasicRenameListFields.Key.Extension),
            ["full-name"] = (BasicRenameListField.Group, BasicRenameListFields.Key.FullName),
            ["full-path"] = (BasicRenameListField.Group, BasicRenameListFields.Key.FullPath),
            ["parent-folder"] = (BasicRenameListField.Group, BasicRenameListFields.Key.Folder),
            ["file-name-numeric-value"] = (BasicRenameListField.Group, BasicRenameListFields.Key.FileNameNumeric),
            ["file-name-length"] = (BasicRenameListField.Group, BasicRenameListFields.Key.FileNameLength),
            ["full-path-length"] = (BasicRenameListField.Group, BasicRenameListFields.Key.FullPathLength),
            // Extended / file properties (except file-date, which needs args)
            ["file-size"] = (ExtendedRenameListFields.Group, "Size"),
            ["file-count"] = (ExtendedRenameListFields.Group, "FileCount"),
            // Audio tag semantic fields
            ["audio-title"] = (AudioTagRenameListFields.Group, "Title"),
            ["audio-artist"] = (AudioTagRenameListFields.Group, "Performers"),
            ["audio-album-artist"] = (AudioTagRenameListFields.Group, "AlbumArtists"),
            ["audio-album"] = (AudioTagRenameListFields.Group, "Album"),
            ["audio-year"] = (AudioTagRenameListFields.Group, "Year"),
            ["audio-genre"] = (AudioTagRenameListFields.Group, "Genres"),
            ["audio-track"] = (AudioTagRenameListFields.Group, "Track"),
            ["audio-track-count"] = (AudioTagRenameListFields.Group, "TrackCount"),
            ["audio-disc"] = (AudioTagRenameListFields.Group, "Disc"),
            ["audio-disc-count"] = (AudioTagRenameListFields.Group, "DiscCount"),
            ["audio-comment"] = (AudioTagRenameListFields.Group, "Comment"),
            ["audio-composer"] = (AudioTagRenameListFields.Group, "Composers"),
            ["audio-lyrics"] = (AudioTagRenameListFields.Group, "Lyrics"),
            ["audio-copyright"] = (AudioTagRenameListFields.Group, "Copyright"),
            ["audio-grouping"] = (AudioTagRenameListFields.Group, "Grouping"),
            ["audio-bpm"] = (AudioTagRenameListFields.Group, "BeatsPerMinute"),
            ["audio-conductor"] = (AudioTagRenameListFields.Group, "Conductor"),
            ["audio-mb-artist-id"] = (AudioTagRenameListFields.Group, "MusicBrainzArtistId"),
            ["audio-mb-release-id"] = (AudioTagRenameListFields.Group, "MusicBrainzReleaseId"),
            ["audio-mb-release-artist-id"] = (AudioTagRenameListFields.Group, "MusicBrainzReleaseArtistId"),
            ["audio-mb-track-id"] = (AudioTagRenameListFields.Group, "MusicBrainzTrackId"),
            ["audio-mb-disc-id"] = (AudioTagRenameListFields.Group, "MusicBrainzDiscId"),
            ["audio-mb-release-status"] = (AudioTagRenameListFields.Group, "MusicBrainzReleaseStatus"),
            ["audio-mb-release-type"] = (AudioTagRenameListFields.Group, "MusicBrainzReleaseType"),
            ["audio-mb-release-country"] = (AudioTagRenameListFields.Group, "MusicBrainzReleaseCountry"),
            ["audio-musicip-id"] = (AudioTagRenameListFields.Group, "MusicIpId"),
            ["audio-amazon-id"] = (AudioTagRenameListFields.Group, "AmazonId"),
            // Media properties
            ["media-mime"] = (MediaRenameListFields.Group, "MimeType"),
            ["media-corrupt"] = (MediaRenameListFields.Group, "PossiblyCorrupt"),
            ["media-duration"] = (MediaRenameListFields.Group, "Duration"),
            ["media-duration-sec"] = (MediaRenameListFields.Group, "DurationSeconds"),
            ["media-types"] = (MediaRenameListFields.Group, "MediaTypes"),
            ["media-description"] = (MediaRenameListFields.Group, "Description"),
            ["media-audio-bitrate"] = (MediaRenameListFields.Group, "AudioBitrate"),
            ["media-samplerate"] = (MediaRenameListFields.Group, "AudioSampleRate"),
            ["media-bits-per-sample"] = (MediaRenameListFields.Group, "BitsPerSample"),
            ["media-channels"] = (MediaRenameListFields.Group, "AudioChannels"),
            ["media-video-width"] = (MediaRenameListFields.Group, "VideoWidth"),
            ["media-video-height"] = (MediaRenameListFields.Group, "VideoHeight"),
            ["media-photo-width"] = (MediaRenameListFields.Group, "PhotoWidth"),
            ["media-photo-height"] = (MediaRenameListFields.Group, "PhotoHeight"),
            ["media-photo-quality"] = (MediaRenameListFields.Group, "PhotoQuality"),
            // MPEG audio properties
            ["mpeg-bitrate"] = (MpegRenameListFields.Group, "Bitrate"),
            ["mpeg-copyright"] = (MpegRenameListFields.Group, "Copyright"),
            ["mpeg-duration"] = (MpegRenameListFields.Group, "Duration"),
            ["mpeg-duration-sec"] = (MpegRenameListFields.Group, "DurationSecs"),
            ["mpeg-encoding"] = (MpegRenameListFields.Group, "VBR"),
            ["mpeg-frequency"] = (MpegRenameListFields.Group, "Frequency"),
            ["mpeg-layer"] = (MpegRenameListFields.Group, "Layer"),
            ["mpeg-ver"] = (MpegRenameListFields.Group, "Level"),
            ["mpeg-mode"] = (MpegRenameListFields.Group, "Mode"),
            ["mpeg-original"] = (MpegRenameListFields.Group, "Original"),
            ["mpeg-protection"] = (MpegRenameListFields.Group, "Protection"),
            // Image properties
            ["image-width"] = (ImageRenameListFields.Group, "Width"),
            ["image-height"] = (ImageRenameListFields.Group, "Height"),
            ["image-bit-depth"] = (ImageRenameListFields.Group, "BitDepth"),
            ["image-format"] = (ImageRenameListFields.Group, "Format"),
            ["image-horz-res"] = (ImageRenameListFields.Group, "HorzRes"),
            ["image-vert-res"] = (ImageRenameListFields.Group, "VertRes"),
            ["image-frame-count"] = (ImageRenameListFields.Group, "Frames"),
            // EXIF / Jpeg Tag (named tokens + date; generic exif tag-id omitted)
            ["exif-make"] = (JpegRenameListFields.Group, "ExifDirectory*271"),
            ["exif-model"] = (JpegRenameListFields.Group, "ExifDirectory*272"),
            ["exif-exposure"] = (JpegRenameListFields.Group, "ExifDirectory*33434"),
            ["exif-fnumber"] = (JpegRenameListFields.Group, "ExifDirectory*33437"),
            ["exif-iso"] = (JpegRenameListFields.Group, "ExifDirectory*34855"),
            ["exif-focal"] = (JpegRenameListFields.Group, "ExifDirectory*37386"),
            ["exif-focal-35"] = (JpegRenameListFields.Group, "ExifDirectory*41989"),
            ["exif-date"] = (JpegRenameListFields.Group, "ExifDirectory*36867"),
        };

        /// <summary>
        /// Resolves a validated formatter token to a catalog field when mapped.
        /// </summary>
        /// <param name="canonicalName">Primary token name from validation.</param>
        /// <param name="args">Raw argument text after the first <c>:</c>, or empty.</param>
        /// <param name="groupId">Catalog group id when mapped.</param>
        /// <param name="propertyKey">Catalog property key when mapped.</param>
        /// <returns><see langword="true"/> when the token maps to a Rename List field.</returns>
        internal static bool TryMap(string canonicalName, string args, out string groupId, out string propertyKey)
        {
            if (string.Equals(canonicalName, "file-date", StringComparison.Ordinal))
            {
                return _TryMapFileDate(args, out groupId, out propertyKey);
            }

            if (_canonicalNameToField.TryGetValue(canonicalName, out var mapped))
            {
                groupId = mapped.GroupId;
                propertyKey = mapped.PropertyKey;
                return true;
            }

            groupId = string.Empty;
            propertyKey = string.Empty;
            return false;
        }

        /// <summary>
        /// Maps a filesystem <see cref="TimestampField"/> to the Extended date column.
        /// </summary>
        /// <param name="timestampField">Which file timestamp the filter or token targets.</param>
        /// <param name="groupId">Catalog group id when mapped.</param>
        /// <param name="propertyKey">Catalog property key when mapped.</param>
        /// <returns>Always <see langword="true"/> for known enum values.</returns>
        /// <exception cref="UnreachableException">Unexpected enum value.</exception>
        internal static bool TryMapTimestampField(
            TimestampField timestampField,
            out string groupId,
            out string propertyKey
        )
        {
            groupId = ExtendedRenameListFields.Group;
            propertyKey = timestampField switch
            {
                TimestampField.Creation => "CreationDate",
                TimestampField.LastWrite => "LastWriteDate",
                TimestampField.LastAccess => "LastAccessDate",
                _ => throw new UnreachableException(),
            };
            return true;
        }

        /// <summary>
        /// Maps <c>file-date</c> args (<c>format,date-kind</c>) to an Extended date column.
        /// </summary>
        private static bool _TryMapFileDate(string args, out string groupId, out string propertyKey)
        {
            groupId = string.Empty;
            propertyKey = string.Empty;
            if (string.IsNullOrWhiteSpace(args))
            {
                return false;
            }

            var lastComma = args.LastIndexOf(',');
            if (lastComma < 0 || lastComma >= args.Length - 1)
            {
                return false;
            }

            var dateKind = args[(lastComma + 1)..].Trim();
            if (!_fileDateKindToField.TryGetValue(dateKind, out var timestampField))
            {
                return false;
            }

            return TryMapTimestampField(timestampField, out groupId, out propertyKey);
        }
    }
}
