using System.Collections.Immutable;
using Mfr.Models;
using TagLib;
using TagLib.Mpeg;

namespace Mfr.Metadata
{
    /// <summary>
    /// Loads and saves canonical <see cref="AudioTagOverlay"/> values via TagLibSharp across supported formats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call <see cref="Apply"/> only when the rename row’s embedded-tag preview differs from its original snapshot;
    /// compare outside this type (for example in <c>CommitExecutor</c>) before calling. <see cref="Apply"/>
    /// opens the file, builds an overlay snapshot from TagLib (<see cref="Read"/> normalization), compares it to the
    /// preview in full, returns without saving when they match, and otherwise writes modeled fields onto TagLib before saving.
    /// </para>
    /// <para>
    /// For MPEG/MP3 files, ID3v1 and ID3v2 are materialized separately in <see cref="AudioTagOverlay"/> (phase 1); other
    /// formats still use the merged TagLib <see cref="Tag"/> façade only.
    /// </para>
    /// <para>
    /// String fields cleared in the overlay are written as empty strings or null TagLib assigns; numerics use
    /// <c>0</c> when the preview clears a value; multiline lists use overlay <c>; </c> join/split conventions.
    /// </para>
    /// </remarks>
    public static class AudioTagPersistence
    {
        private static readonly string[] _ListSeparators = [";"];

        /// <summary>
        /// Reads embedded audio tags into a detached <see cref="AudioTagOverlay"/>.
        /// </summary>
        /// <param name="absolutePath">Fully qualified filesystem path to an existing file.</param>
        /// <returns>A new overlay built from embedded tags.</returns>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">TagLib cannot open or read the file.</exception>
        /// <exception cref="CorruptFileException">Thrown by TagLib when the embedded structure is unreadable.</exception>
        /// <exception cref="UnsupportedFormatException">Thrown by TagLib when the format cannot be loaded.</exception>
        public static AudioTagOverlay Read(string absolutePath)
        {
            _ValidateExistingRegularFile(absolutePath);

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            if (file is AudioFile)
                return _ReadMpegOverlay(file);

            return _FromTag(file.Tag);
        }

        /// <summary>
        /// Loads the file’s normalized tag overlay via TagLib and, when <paramref name="previewOverlay"/> differs from that overlay, assigns modeled fields from <paramref name="previewOverlay"/> to TagLib tags and saves.
        /// </summary>
        /// <param name="absolutePath">Path to an existing regular file (typically the post-move destination).</param>
        /// <param name="previewOverlay">Desired tag values.</param>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">The file cannot be opened or saved.</exception>
        public static void Apply(string absolutePath, AudioTagOverlay previewOverlay)
        {
            _ValidateExistingRegularFile(absolutePath);

            var baselineOverlay = Read(absolutePath);
            if (previewOverlay.Equals(baselineOverlay))
                return;

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            if (file is AudioFile)
            {
                _ApplyToMpeg(file, previewOverlay);
                file.Save();
                return;
            }

            _WriteOverlayToTag(file.Tag, previewOverlay);

            file.Save();
        }

        /// <summary>
        /// Removes all embedded tag blobs TagLib associates with the file (ID3, Vorbis comments, MP4 tags, RIFF lists, image markers, etc.).
        /// </summary>
        /// <param name="absolutePath">Path to an existing regular file (typically after rename, at the preview destination).</param>
        /// <exception cref="ArgumentException"><paramref name="absolutePath"/> is empty, relative, missing, or a directory.</exception>
        /// <exception cref="IOException">The file cannot be opened or saved.</exception>
        public static void RemoveAllEmbeddedTags(string absolutePath)
        {
            _ValidateExistingRegularFile(absolutePath);

            using var file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(absolutePath));
            file.RemoveTags(TagTypes.AllTags);
            file.Save();
        }

        private static AudioTagOverlay _ReadMpegOverlay(TagLib.File file)
        {
            // Read structured ID3 tags before touching merged file.Tag; TagLib can adjust Id3v2 render details
            // once the façade Tag has been accessed.
            var id3v2 = _ReadId3v2Snapshot(file);
            var id3v1 = _ReadId3v1Snapshot(file);
            var overlay = _FromTag(file.Tag);
            overlay.Id3v2 = id3v2;
            overlay.Id3v1 = id3v1;
            return overlay;
        }

        private static Id3v1TagData? _ReadId3v1Snapshot(TagLib.File file)
        {
            var tag = file.GetTag(TagTypes.Id3v1, false);
            if (tag is not TagLib.Id3v1.Tag id3v1)
                return null;

            if (_IsId3v1EffectivelyEmpty(id3v1))
                return null;

            var genreByte = id3v1.FirstGenre is null
                ? (byte)0
                : Genres.AudioToIndex(id3v1.FirstGenre);

            return new Id3v1TagData
            {
                Title = _NullIfEmpty(id3v1.Title),
                Artist = _NullIfEmpty(id3v1.FirstPerformer),
                Album = _NullIfEmpty(id3v1.Album),
                Year = id3v1.Year == 0 ? null : id3v1.Year,
                Comment = _NullIfEmpty(id3v1.Comment),
                Track = id3v1.Track == 0 ? null : (byte)System.Math.Min(id3v1.Track, 255u),
                Genre = genreByte,
            };
        }

        private static bool _IsId3v1EffectivelyEmpty(TagLib.Id3v1.Tag id3v1)
        {
            return string.IsNullOrWhiteSpace(id3v1.Title)
                && (id3v1.Performers.Length == 0 || string.IsNullOrWhiteSpace(id3v1.FirstPerformer))
                && string.IsNullOrWhiteSpace(id3v1.Album)
                && id3v1.Year == 0
                && string.IsNullOrWhiteSpace(id3v1.Comment)
                && id3v1.Track == 0
                && (id3v1.Genres.Length == 0 || string.IsNullOrWhiteSpace(id3v1.FirstGenre));
        }

        private static Id3v2TagData? _ReadId3v2Snapshot(TagLib.File file)
        {
            var raw = file.GetTag(TagTypes.Id3v2, false);
            if (raw is not TagLib.Id3v2.Tag id3v2)
                return null;

            var fullRender = id3v2.Render();
            var canonicalTagBytes = ImmutableArray.Create(fullRender.Data);
            var canonicalTag = new TagLib.Id3v2.Tag(fullRender);
            var version = canonicalTag.Version;
            var list = new List<Id3v2SerializedFrame>();

            foreach (var frame in canonicalTag)
            {
                var rendered = frame.Render(version);
                var frameId = frame.FrameId.ToString(StringType.Latin1);
                list.Add(new Id3v2SerializedFrame
                {
                    FrameId = frameId,
                    Data = ImmutableArray.Create(rendered.Data),
                });
            }

            list.Sort(_CompareSerializedFrames);

            return new Id3v2TagData
            {
                Version = version,
                CanonicalTagBytes = canonicalTagBytes,
                Frames = [.. list],
            };
        }

        private static int _CompareSerializedFrames(Id3v2SerializedFrame a, Id3v2SerializedFrame b)
        {
            var id = string.CompareOrdinal(a.FrameId, b.FrameId);
            if (id != 0)
                return id;

            return a.Data.AsSpan().SequenceCompareTo(b.Data.AsSpan());
        }

        private static void _ApplyToMpeg(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.Id3v2 is not null)
            {
                var id3v2 = (TagLib.Id3v2.Tag)file.GetTag(TagTypes.Id3v2, true);
                id3v2.Clear();

                foreach (var blob in overlay.Id3v2.Frames)
                {
                    var offset = 0;
                    var vec = new ByteVector([.. blob.Data]);
                    var frame = TagLib.Id3v2.FrameFactory.CreateFrame(vec, file, ref offset, overlay.Id3v2.Version, false);
                    if (frame is not null)
                        id3v2.AddFrame(frame);
                }
            }

            if (overlay.Id3v1 is not null)
                _WriteId3v1Tag(file, overlay.Id3v1);

            _WriteOverlayToTag(file.Tag, overlay);
        }

        private static void _WriteId3v1Tag(TagLib.File file, Id3v1TagData data)
        {
            var v1 = (TagLib.Id3v1.Tag)file.GetTag(TagTypes.Id3v1, true);
            v1.Title = data.Title ?? string.Empty;
            v1.Performers = string.IsNullOrWhiteSpace(data.Artist) ? [] : [data.Artist.Trim()];
            v1.Album = data.Album ?? string.Empty;
            v1.Year = data.Year ?? 0;
            v1.Comment = data.Comment ?? string.Empty;
            v1.Track = data.Track ?? 0;

            var genreName = Genres.IndexToAudio(data.Genre);
            v1.Genres = string.IsNullOrEmpty(genreName) ? [] : [genreName];
        }

        private static void _WriteOverlayToTag(Tag tag, AudioTagOverlay overlay)
        {
            tag.Title = _EmptyStringToNull(overlay.Title);
            tag.Album = _EmptyStringToNull(overlay.Album);
            tag.Performers = _SplitJoinedList(overlay.Performers);
            tag.AlbumArtists = _SplitJoinedList(overlay.AlbumArtists);
            tag.Composers = _SplitJoinedList(overlay.Composers);
            tag.Genres = string.IsNullOrWhiteSpace(overlay.Genre)
                ? []
                : [overlay.Genre.Trim()];

            tag.Comment = _EmptyStringToNull(overlay.Comment);
            tag.Lyrics = _EmptyStringToNull(overlay.Lyrics);
            tag.Copyright = _EmptyStringToNull(overlay.Copyright);
            tag.Grouping = _EmptyStringToNull(overlay.Grouping);

            tag.Year = overlay.Year ?? 0;
            tag.Track = overlay.Track ?? 0;
            tag.TrackCount = overlay.TrackCount ?? 0;
            tag.Disc = overlay.Disc ?? 0;
            tag.DiscCount = overlay.DiscCount ?? 0;
        }

        private static string? _EmptyStringToNull(string? text)
        {
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private static void _ValidateExistingRegularFile(string absolutePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(absolutePath);

            if (!Path.IsPathFullyQualified(absolutePath))
                throw new ArgumentException("Path must be fully qualified.", nameof(absolutePath));

            if (Directory.Exists(absolutePath))
                throw new ArgumentException($"'{absolutePath}' is a directory.", nameof(absolutePath));

            if (!System.IO.File.Exists(absolutePath))
                throw new ArgumentException($"File does not exist: '{absolutePath}'.", nameof(absolutePath));
        }

        private static AudioTagOverlay _FromTag(Tag tag)
        {
            return new AudioTagOverlay
            {
                Title = _NullIfEmpty(tag.Title),
                Album = _NullIfEmpty(tag.Album),
                Performers = _JoinList(tag.Performers),
                AlbumArtists = _JoinList(tag.AlbumArtists),
                Composers = _JoinList(tag.Composers),
                Genre = _NullIfEmpty(tag.FirstGenre),
                Comment = _NullIfEmpty(tag.Comment),
                Lyrics = _NullIfEmpty(tag.Lyrics),
                Copyright = _NullIfEmpty(tag.Copyright),
                Grouping = _NullIfEmpty(tag.Grouping),
                Year = tag.Year == 0 ? null : tag.Year,
                Track = tag.Track == 0 ? null : tag.Track,
                TrackCount = tag.TrackCount == 0 ? null : tag.TrackCount,
                Disc = tag.Disc == 0 ? null : tag.Disc,
                DiscCount = tag.DiscCount == 0 ? null : tag.DiscCount,
            };
        }

        private static string[] _SplitJoinedList(string? joined)
        {
            if (string.IsNullOrWhiteSpace(joined))
                return [];

            return [.. joined.Split(_ListSeparators, StringSplitOptions.TrimEntries)
                .Where(part => !string.IsNullOrEmpty(part))
                .Select(part => part.Trim())];
        }

        private static string? _JoinList(string[] values)
        {
            var filtered =
                values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(static v => v.Trim()).ToArray();

            return filtered.Length == 0 ? null : string.Join("; ", filtered);
        }

        private static string? _NullIfEmpty(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
    }
}
