using System.Collections.Immutable;
using Mfr.Models.Tags;
using TagLib;
using TagLib.Mpeg;
using TagLib.Ogg;
using AppleTag = TagLib.Mpeg4.AppleTag;

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
    /// For MPEG/MP3 files, ID3v1 and ID3v2 are materialized separately in <see cref="AudioTagOverlay"/>; non-MPEG files
    /// also capture optional <see cref="AudioTagOverlay.Xiph"/>, <see cref="AudioTagOverlay.Ape"/>,
    /// <see cref="AudioTagOverlay.Apple"/>, and <see cref="AudioTagOverlay.Asf"/> blocks when present, in addition to the merged façade fields.
    /// </para>
    /// <para>
    /// String fields cleared in the overlay are written as empty strings or null TagLib assigns; numerics use
    /// <c>0</c> when the preview clears a value; multiline lists use overlay <c>; </c> join/split conventions.
    /// </para>
    /// <para>
    /// Before writing, façade fields on <see cref="AudioTagOverlay"/> are merged into any loaded per–<c>TagTypes</c>
    /// blocks (ID3v1/v2, Xiph, APE, Apple, ASF) so semantic edits from filters stay consistent with serialized snapshots.
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
            return _ReadOverlay(file);
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
            var coalescedOverlay = previewOverlay.Clone();
            _CoalesceSemanticIntoNativeBlocks(file, coalescedOverlay);
            _ApplyNativeTagBlocks(file, coalescedOverlay);

            // MPEG/MP3: persist structured Id3v2 frame list + Id3v1 snapshot; those paths also assign merged façade
            // fields onto the file’s combined tag (TagLib routes them into ID3 as appropriate).
            // Other formats: Xiph/APE/Apple/ASF blocks were already written above; only the merged TagLib façade remains.
            if (file is AudioFile)
                _ApplyToMpeg(file, coalescedOverlay);
            else
                _WriteOverlayToTag(file.Tag, coalescedOverlay);

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

        /// <summary>
        /// Builds a full overlay including per–<see cref="TagTypes"/> blocks, reading native tags before the merged façade where needed.
        /// </summary>
        private static AudioTagOverlay _ReadOverlay(TagLib.File file)
        {
            Id3v1TagData? id3v1 = null;
            Id3v2TagData? id3v2 = null;
            if (file is AudioFile)
            {
                // Read structured ID3 tags before touching merged file.Tag; TagLib can adjust Id3v2 render details
                // once the façade Tag has been accessed.
                id3v2 = _ReadId3v2Snapshot(file);
                id3v1 = _ReadId3v1Snapshot(file);
            }

            var xiph = _ReadXiph(file);
            var ape = _ReadApe(file);
            var apple = _ReadAppleSnapshot(file);
            var asf = _ReadAsfSnapshot(file);

            var overlay = _FromTag(file.Tag);
            overlay.Id3v1 = id3v1;
            overlay.Id3v2 = id3v2;
            overlay.Xiph = xiph;
            overlay.Ape = ape;
            overlay.Apple = apple;
            overlay.Asf = asf;
            return overlay;
        }

        private static SerializedTagBlob? _ReadXiph(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Xiph, false) is not XiphComment xc || xc.IsEmpty)
                return null;

            var rendered = xc.Render(addFramingBit: false);
            return new SerializedTagBlob { CanonicalTagBytes = ImmutableArray.Create(rendered.Data) };
        }

        private static SerializedTagBlob? _ReadApe(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Ape, false) is not TagLib.Ape.Tag ape || ape.IsEmpty)
                return null;

            var rendered = ape.Render();
            return new SerializedTagBlob { CanonicalTagBytes = ImmutableArray.Create(rendered.Data) };
        }

        private static AppleTagData? _ReadAppleSnapshot(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Apple, false) is not AppleTag apple || apple.IsEmpty)
                return null;

            return _ReadAppleTagData(apple);
        }

        /// <summary>
        /// Copies visible <see cref="AudioTagOverlay"/> façade fields into tag-type blocks when those blocks are present,
        /// so preview-only semantic edits stay aligned with serialized Xiph/APE/MP4/ASF/ID3 payloads before save.
        /// </summary>
        private static void _CoalesceSemanticIntoNativeBlocks(TagLib.File file, AudioTagOverlay coalescedOverlay)
        {
            if (coalescedOverlay.Id3v1 is not null)
                _CoalesceId3v1FromFacade(coalescedOverlay);

            if (coalescedOverlay.Id3v2 is not null)
            {
                var id3 = new TagLib.Id3v2.Tag(new ByteVector([.. coalescedOverlay.Id3v2.CanonicalTagBytes.ToArray()]));
                _WriteOverlayToTag(id3, coalescedOverlay);
                coalescedOverlay.Id3v2 = _SnapshotId3v2Data(id3);
            }

            if (coalescedOverlay.Xiph is not null)
            {
                var xc = new XiphComment([.. coalescedOverlay.Xiph.CanonicalTagBytes.ToArray()]);
                _WriteOverlayToTag(xc, coalescedOverlay);
                var rendered = xc.Render(addFramingBit: false);
                coalescedOverlay.Xiph = new SerializedTagBlob { CanonicalTagBytes = ImmutableArray.Create(rendered.Data) };
            }

            if (coalescedOverlay.Ape is not null)
            {
                var ape = new TagLib.Ape.Tag(new ByteVector([.. coalescedOverlay.Ape.CanonicalTagBytes.ToArray()]));
                _WriteOverlayToTag(ape, coalescedOverlay);
                coalescedOverlay.Ape = new SerializedTagBlob { CanonicalTagBytes = ImmutableArray.Create(ape.Render().Data) };
            }

            if (coalescedOverlay.Asf is not null)
            {
                var asf = new TagLib.Asf.Tag();
                foreach (var row in coalescedOverlay.Asf.Descriptors)
                    asf.AddDescriptor(new TagLib.Asf.ContentDescriptor(row.Name, row.Value));

                _WriteOverlayToTag(asf, coalescedOverlay);
                coalescedOverlay.Asf = _ReadAsfTagData(asf);
            }

            if (coalescedOverlay.Apple is not null && file.GetTag(TagTypes.Apple, true) is AppleTag apple)
            {
                foreach (var row in coalescedOverlay.Apple.Atoms)
                    apple.SetText([.. row.AtomType.ToArray()], [.. row.Values]);

                _WriteOverlayToTag(apple, coalescedOverlay);
                coalescedOverlay.Apple = _ReadAppleTagData(apple) ?? new AppleTagData();
            }
        }

        private static void _CoalesceId3v1FromFacade(AudioTagOverlay coalescedOverlay)
        {
            var parts = _SplitJoinedList(coalescedOverlay.Performers);
            var artist = parts.Length > 0 ? parts[0] : null;

            var genreByte = string.IsNullOrWhiteSpace(coalescedOverlay.Genre)
                ? (byte)0
                : Genres.AudioToIndex(coalescedOverlay.Genre.Trim());

            byte? track = coalescedOverlay.Track is null ? null : (byte)System.Math.Min(coalescedOverlay.Track.Value, 255u);

            coalescedOverlay.Id3v1 = new Id3v1TagData
            {
                Title = _NullIfEmpty(coalescedOverlay.Title),
                Artist = _NullIfEmpty(artist),
                Album = _NullIfEmpty(coalescedOverlay.Album),
                Year = coalescedOverlay.Year,
                Comment = _NullIfEmpty(coalescedOverlay.Comment),
                Track = track,
                Genre = genreByte,
            };
        }

        private static AppleTagData? _ReadAppleTagData(AppleTag apple)
        {
            if (apple.IsEmpty)
                return null;

            var uniqueTypes = new SortedDictionary<string, ByteVector>(StringComparer.Ordinal);

            foreach (var box in apple)
            {
                var typeData = box.BoxType.Data;
                if (typeData is null || typeData.Length != 4)
                    continue;

                var hex = Convert.ToHexString(typeData);
                if (uniqueTypes.ContainsKey(hex))
                    continue;

                uniqueTypes[hex] = box.BoxType;
            }

            var rows = new List<AppleAtomRow>();

            foreach (var kvp in uniqueTypes)
            {
                var boxType = kvp.Value;
                var texts = apple.GetText(boxType);
                if (texts is null || texts.Length == 0)
                    continue;

                var vals = ImmutableArray.CreateRange(texts.Select(static s => s.Trim()));
                var atomType = ImmutableArray.Create(boxType.Data);
                rows.Add(new AppleAtomRow { AtomType = atomType, Values = vals });
            }

            rows.Sort(static (a, b) =>
            {
                var byType = a.AtomType.AsSpan().SequenceCompareTo(b.AtomType.AsSpan());
                if (byType != 0)
                    return byType;

                return _CompareImmutableStringSeq(a.Values, b.Values);
            });

            return new AppleTagData { Atoms = [.. rows] };
        }

        private static AsfTagData? _ReadAsfSnapshot(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Asf, false) is not TagLib.Asf.Tag asf || asf.IsEmpty)
                return null;

            return _ReadAsfTagData(asf);
        }

        private static AsfTagData _ReadAsfTagData(TagLib.Asf.Tag asf)
        {
            var rows = new List<AsfDescriptorRow>();
            foreach (var d in asf)
                rows.Add(new AsfDescriptorRow(d.Name, d.ToString()));

            rows.Sort(static (a, b) =>
            {
                var byName = string.CompareOrdinal(a.Name, b.Name);
                if (byName != 0)
                    return byName;

                return string.CompareOrdinal(a.Value, b.Value);
            });

            return new AsfTagData { Descriptors = [.. rows] };
        }

        private static int _CompareImmutableStringSeq(ImmutableArray<string> a, ImmutableArray<string> b)
        {
            var len = Math.Min(a.Length, b.Length);
            for (var i = 0; i < len; i++)
            {
                var c = string.CompareOrdinal(a[i], b[i]);
                if (c != 0)
                    return c;
            }

            return a.Length.CompareTo(b.Length);
        }

        private static void _ApplyNativeTagBlocks(TagLib.File file, AudioTagOverlay overlay)
        {
            _ApplyXiph(file, overlay);
            _ApplyApe(file, overlay);
            _ApplyApple(file, overlay);
            _ApplyAsf(file, overlay);
        }

        private static void _ApplyXiph(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.Xiph is null)
                return;

            if (file.GetTag(TagTypes.Xiph, true) is not XiphComment live)
                return;

            var parsed = new XiphComment([.. overlay.Xiph.CanonicalTagBytes.ToArray()]);
            live.Clear();

            foreach (var key in parsed)
            {
                var values = parsed.GetField(key);
                if (values.Length == 0)
                    continue;

                live.SetField(key, values);
            }
        }

        private static void _ApplyApe(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.Ape is null)
                return;

            if (file.GetTag(TagTypes.Ape, true) is not TagLib.Ape.Tag live)
                return;

            var parsed = new TagLib.Ape.Tag([.. overlay.Ape.CanonicalTagBytes.ToArray()]);
            live.Clear();

            foreach (var key in parsed)
            {
                var item = parsed.GetItem(key);
                if (item is not null)
                    live.SetItem(item);
            }
        }

        private static void _ApplyApple(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.Apple is null)
                return;

            if (file.GetTag(TagTypes.Apple, true) is not AppleTag apple)
                return;

            foreach (var row in overlay.Apple.Atoms)
                apple.SetText([.. row.AtomType.ToArray()], [.. row.Values]);
        }

        private static void _ApplyAsf(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.Asf is null)
                return;

            if (file.GetTag(TagTypes.Asf, true) is not TagLib.Asf.Tag asf)
                return;

            asf.Clear();

            foreach (var row in overlay.Asf.Descriptors)
                asf.AddDescriptor(new TagLib.Asf.ContentDescriptor(row.Name, row.Value));
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

            return _SnapshotId3v2Data(id3v2);
        }

        /// <summary>
        /// Serializes a live ID3v2 tag into the overlay model (canonical header bytes + sorted frames).
        /// </summary>
        private static Id3v2TagData _SnapshotId3v2Data(TagLib.Id3v2.Tag id3v2)
        {
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
