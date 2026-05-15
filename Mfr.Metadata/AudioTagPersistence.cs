using System.Collections.Immutable;
using Mfr.Models.Tags;
using TagLib;
using TagLib.Mpeg;
using TagLib.Ogg;
using TagLib.Riff;
using AppleTag = TagLib.Mpeg4.AppleTag;

namespace Mfr.Metadata
{
    /// <summary>
    /// Loads and saves canonical structured <see cref="AudioTagOverlay"/> blocks via TagLibSharp across supported formats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call <see cref="Apply"/> only when the rename row’s embedded-tag preview differs from its original snapshot;
    /// compare outside this type (for example in <c>CommitExecutor</c>) before calling. <see cref="Apply"/>
    /// opens the file, builds an overlay snapshot from TagLib (<see cref="Read"/> normalization), compares it to the
    /// preview in full, returns without saving when they match, and otherwise writes blocks and merged TagLib-visible
    /// semantics before saving.
    /// </para>
    /// <para>
    /// For MPEG/MP3 files, ID3v1 and ID3v2 are materialized separately in <see cref="AudioTagOverlay"/>; non-MPEG files
    /// may carry optional <see cref="AudioTagOverlay.Xiph"/>, <see cref="AudioTagOverlay.Ape"/>,
    /// <see cref="AudioTagOverlay.RiffInfo"/> (classic WAV LIST/INAM maps),
    /// <see cref="AudioTagOverlay.Apple"/>, and <see cref="AudioTagOverlay.Asf"/> blocks when present.
    /// </para>
    /// <para>
    /// String fields cleared in the semantic projection are written as empty strings or null TagLib assigns; numerics use
    /// <c>0</c> when cleared; multiline lists use <c>; </c> join/split conventions.
    /// </para>
    /// <para>
    /// After preview, hosts may call <see cref="TryNormalizeNativeBlocks"/> on the preview overlay using the on-disk
    /// source path so per-block snapshots match the <see cref="AudioTagSemanticSurface.FromBlocks"/> projection (for example
    /// <c>Mfr.Core</c> rename preview end-of-chain reconcile).
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
            var ambientCombinedBeforeBlockReads = AudioTagSemanticSurface.FromCombinedTag(file.Tag);
            var overlay = _ReadOverlay(file);
            _MergeAmbientCombinedTagFacadeIntoBlocks(file, overlay, absolutePath, ambientCombinedBeforeBlockReads);
            return overlay;
        }

        /// <summary>
        /// Like <see cref="NormalizeNativeBlocks"/> but returns <see langword="false"/> when TagLib cannot open the path.
        /// </summary>
        public static bool TryNormalizeNativeBlocks(AudioTagOverlay overlay, string embeddedTagSourcePath)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            if (string.IsNullOrWhiteSpace(embeddedTagSourcePath))
                return false;

            try
            {
                NormalizeNativeBlocks(overlay, embeddedTagSourcePath);
                return true;
            }
            catch (UnsupportedFormatException)
            {
                return false;
            }
            catch (CorruptFileException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>
        /// Re-snaps native tag blocks using the semantic projection derived from current blocks (end-of-preview reconcile).
        /// </summary>
        public static void NormalizeNativeBlocks(AudioTagOverlay overlay, string embeddedTagSourcePath)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            _ValidateExistingRegularFile(embeddedTagSourcePath);
            var merged = AudioTagSemanticSurface.FromBlocks(overlay);
            MergeSemanticOntoNativeBlocks(overlay, merged, embeddedTagSourcePath);
        }

        /// <summary>
        /// Like <see cref="MergeSemanticOntoNativeBlocks"/> but ignores TagLib failures (same swallow policy as preview materialize historically).
        /// </summary>
        public static bool TryMergeSemanticOntoNativeBlocks(AudioTagOverlay overlay, AudioTagSemanticSurface merged, string embeddedTagSourcePath)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            try
            {
                MergeSemanticOntoNativeBlocks(overlay, merged, embeddedTagSourcePath);
                return true;
            }
            catch (UnsupportedFormatException)
            {
                return false;
            }
            catch (CorruptFileException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Merges a semantic projection into structured per–<c>TagTypes</c> blocks on <paramref name="overlay"/>.
        /// </summary>
        /// <remarks>
        /// When <paramref name="embeddedTagSourcePath"/> is missing or the file cannot be opened, Mp4 apple atom coalescence is skipped; other containers still update using parsed blobs alone.
        /// <para>
        /// When a live file is available and the incoming overlay had no native blocks at all, the merged semantic surface is
        /// written to TagLib's merged façade and snapshots are re-read (materializes RIFF INFO for façade-only hosts). When
        /// blocks already exist (ASF, Xiph, …), that extra pass is skipped so per-block merges remain authoritative.
        /// </para>
        /// </remarks>
        public static void MergeSemanticOntoNativeBlocks(
            AudioTagOverlay overlay,
            AudioTagSemanticSurface merged,
            string? embeddedTagSourcePath)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var hadAnyNativeBlockBeforeSemanticMerge =
                overlay.Id3v1 is not null
                || overlay.Id3v2 is not null
                || overlay.Xiph is not null
                || overlay.Ape is not null
                || overlay.RiffInfo is not null
                || overlay.Apple is not null
                || overlay.Asf is not null;

            var carrier = CoalesceCarrier.FromOverlay(overlay);
            carrier.ApplySemantic(merged);

            TagLib.File? file = null;
            if (!string.IsNullOrWhiteSpace(embeddedTagSourcePath)
                && Path.IsPathFullyQualified(embeddedTagSourcePath)
                && System.IO.File.Exists(embeddedTagSourcePath)
                && !Directory.Exists(embeddedTagSourcePath))
            {
                try
                {
                    file = TagLib.File.Create(new TagLib.File.LocalFileAbstraction(embeddedTagSourcePath));
                }
                catch (UnsupportedFormatException)
                {
                    file = null;
                }
                catch (CorruptFileException)
                {
                    file = null;
                }
                catch (IOException)
                {
                    file = null;
                }
            }

            try
            {
                _MergeSemanticCarrierIntoBlocks(file, carrier);
                if (file is not null
                    && carrier.ToSemanticSurface().ContainsRenderableSemantics()
                    && !hadAnyNativeBlockBeforeSemanticMerge)
                {
                    _WriteSemanticSurfaceToTag(file.Tag, carrier.ToSemanticSurface());
                    try
                    {
                        _RefreshCarrierNativeSnapshotsFromFile(file, carrier);
                    }
                    catch (CorruptFileException)
                    {
                        // Preserve carrier blocks merged before re-snapshot; some hosts disagree transiently after façade writes.
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // Mirrors Corrupt policy: malformed or transient in-memory state after façade write.
                    }
                }
            }
            finally
            {
                file?.Dispose();
            }

            carrier.CopyBlocksTo(overlay);
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
            _ApplyNativeTagBlocks(file, previewOverlay);

            if (file is AudioFile)
                _ApplyToMpeg(file, previewOverlay);
            else
                _WriteSemanticSurfaceToTag(file.Tag, AudioTagSemanticSurface.FromBlocks(previewOverlay));

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
        /// Builds an overlay snapshot from structured per–<see cref="TagTypes"/> payloads only (no mirrored scalar façade).
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
            var riffInfo = _ReadRiffInfo(file);
            var apple = _ReadAppleSnapshot(file);
            var asf = _ReadAsfSnapshot(file);

            return new AudioTagOverlay
            {
                Id3v1 = id3v1,
                Id3v2 = id3v2,
                Xiph = xiph,
                Ape = ape,
                RiffInfo = riffInfo,
                Apple = apple,
                Asf = asf,
            };
        }

        /// <summary>
        /// Fills fields missing from the block projection using TagLib's merged façade (for example ASF metadata where
        /// descriptors lack <c>WM/Title</c> even though the façade still exposes it, or classic WAV RIFF LIST).
        /// </summary>
        /// <param name="file">Open TagLib session; the ambient snapshot was taken before structured reads on this instance.</param>
        /// <param name="overlay">Overlay populated from native blocks only.</param>
        /// <param name="absolutePath">Same path passed to <see cref="Read"/>; used when materializing merged blocks via TagLib.</param>
        /// <param name="ambientCombinedBeforeBlockReads">
        /// Façade snapshot taken immediately after opening the file, before any structured <c>GetTag</c> reads that can
        /// clear or rewrite combined fields.
        /// </param>
        private static void _MergeAmbientCombinedTagFacadeIntoBlocks(
            TagLib.File file,
            AudioTagOverlay overlay,
            string absolutePath,
            AudioTagSemanticSurface ambientCombinedBeforeBlockReads)
        {
            var ambient = ambientCombinedBeforeBlockReads;
            if (!ambient.ContainsRenderableSemantics())
                return;

            var projected = AudioTagSemanticSurface.FromBlocks(overlay);
            var merged = projected.WithMissingFieldsFilledFrom(ambient);
            if (merged.Equals(projected))
                return;

            MergeSemanticOntoNativeBlocks(overlay, merged, absolutePath);
        }

        /// <summary>
        /// Re-reads per–<see cref="TagTypes"/> snapshots from <paramref name="file"/> after the merged semantic surface is applied to the live TagLib façade.
        /// </summary>
        private static void _RefreshCarrierNativeSnapshotsFromFile(TagLib.File file, CoalesceCarrier carrier)
        {
            var refreshed = _ReadOverlay(file);
            carrier.Id3v1 = refreshed.Id3v1;
            carrier.Id3v2 = refreshed.Id3v2;
            carrier.Xiph = refreshed.Xiph;
            carrier.Ape = refreshed.Ape;
            carrier.RiffInfo = refreshed.RiffInfo;
            carrier.Apple = refreshed.Apple;
            carrier.Asf = refreshed.Asf;
        }

        private static SerializedTagBlob? _ReadXiph(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Xiph, false) is not XiphComment xc || xc.IsEmpty)
                return null;

            var rendered = xc.Render(addFramingBit: false);
            return _SerializedBlob(rendered.Data);
        }

        private static SerializedTagBlob? _ReadApe(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Ape, false) is not TagLib.Ape.Tag ape || ape.IsEmpty)
                return null;

            var rendered = ape.Render();
            return _SerializedBlob(rendered.Data);
        }

        private static SerializedTagBlob? _ReadRiffInfo(TagLib.File file)
        {
            if (file.GetTag(TagTypes.RiffInfo, false) is not InfoTag info || info.IsEmpty)
                return null;

            return _SerializedBlob(info.Render().Data);
        }

        private static AppleTagData? _ReadAppleSnapshot(TagLib.File file)
        {
            if (file.GetTag(TagTypes.Apple, false) is not AppleTag apple || apple.IsEmpty)
                return null;

            return _ReadAppleTagData(apple);
        }

        private sealed class CoalesceCarrier
        {
            public Id3v1TagData? Id3v1 { get; set; }
            public Id3v2TagData? Id3v2 { get; set; }
            public SerializedTagBlob? Xiph { get; set; }
            public SerializedTagBlob? Ape { get; set; }
            public SerializedTagBlob? RiffInfo { get; set; }
            public AppleTagData? Apple { get; set; }
            public AsfTagData? Asf { get; set; }
            public string? Title { get; set; }
            public string? Album { get; set; }
            public string? Performers { get; set; }
            public string? AlbumArtists { get; set; }
            public string? Composers { get; set; }
            public string? Genre { get; set; }
            public string? Comment { get; set; }
            public string? Lyrics { get; set; }
            public string? Copyright { get; set; }
            public string? Grouping { get; set; }
            public uint? Year { get; set; }
            public uint? Track { get; set; }
            public uint? TrackCount { get; set; }
            public uint? Disc { get; set; }
            public uint? DiscCount { get; set; }

            public static CoalesceCarrier FromOverlay(AudioTagOverlay overlay)
            {
                return new CoalesceCarrier
                {
                    Id3v1 = overlay.Id3v1,
                    Id3v2 = overlay.Id3v2,
                    Xiph = overlay.Xiph,
                    Ape = overlay.Ape,
                    RiffInfo = overlay.RiffInfo,
                    Apple = overlay.Apple,
                    Asf = overlay.Asf,
                };
            }

            public AudioTagSemanticSurface ToSemanticSurface()
            {
                return new AudioTagSemanticSurface(
                    Title,
                    Album,
                    Performers,
                    AlbumArtists,
                    Composers,
                    Genre,
                    Comment,
                    Lyrics,
                    Copyright,
                    Grouping,
                    Year,
                    Track,
                    TrackCount,
                    Disc,
                    DiscCount);
            }

            public void ApplySemantic(AudioTagSemanticSurface s)
            {
                Title = s.Title;
                Album = s.Album;
                Performers = s.Performers;
                AlbumArtists = s.AlbumArtists;
                Composers = s.Composers;
                Genre = s.Genre;
                Comment = s.Comment;
                Lyrics = s.Lyrics;
                Copyright = s.Copyright;
                Grouping = s.Grouping;
                Year = s.Year;
                Track = s.Track;
                TrackCount = s.TrackCount;
                Disc = s.Disc;
                DiscCount = s.DiscCount;
            }

            public void CopyBlocksTo(AudioTagOverlay o)
            {
                o.Id3v1 = Id3v1;
                o.Id3v2 = Id3v2;
                o.Xiph = Xiph;
                o.Ape = Ape;
                o.RiffInfo = RiffInfo;
                o.Apple = Apple;
                o.Asf = Asf;
            }
        }

        private static void _MergeSemanticCarrierIntoBlocks(TagLib.File? file, CoalesceCarrier c)
        {
            if (c.Id3v1 is not null)
                _MergeSemanticIntoId3v1(c);

            _MergeSemanticIntoId3v2(c);
            _MergeSemanticIntoXiph(c);
            _MergeSemanticIntoApe(c);
            _MergeSemanticIntoRiff(c);
            _MergeSemanticIntoAsf(c);

            if (file is not null && c.Apple is not null && file.GetTag(TagTypes.Apple, true) is AppleTag apple)
                _MergeSemanticIntoApple(apple, c);
        }

        private static void _MergeSemanticIntoId3v2(CoalesceCarrier c)
        {
            if (c.Id3v2 is null)
                return;

            var id3 = new TagLib.Id3v2.Tag(_ToByteVector(c.Id3v2.CanonicalTagBytes));
            _WriteSemanticSurfaceToTag(id3, c.ToSemanticSurface());
            c.Id3v2 = _SnapshotId3v2Data(id3);
        }

        private static void _MergeSemanticIntoXiph(CoalesceCarrier c)
        {
            if (c.Xiph is null)
                return;

            XiphComment xc;
            try
            {
                xc = new XiphComment(_ToByteVector(c.Xiph.CanonicalTagBytes));
            }
            catch (CorruptFileException)
            {
                xc = new XiphComment();
            }
            catch (ArgumentOutOfRangeException)
            {
                // Same as FromBlocks projection: bogus test doubles / truncated packets must not abort merge.
                xc = new XiphComment();
            }

            _WriteSemanticSurfaceToTag(xc, c.ToSemanticSurface());
            var rendered = xc.Render(addFramingBit: false);
            c.Xiph = _SerializedBlob(rendered.Data);
        }

        private static void _MergeSemanticIntoApe(CoalesceCarrier c)
        {
            if (c.Ape is null)
                return;

            TagLib.Ape.Tag ape;
            try
            {
                ape = new TagLib.Ape.Tag(_ToByteVector(c.Ape.CanonicalTagBytes));
            }
            catch (CorruptFileException)
            {
                ape = new TagLib.Ape.Tag();
            }
            catch (ArgumentOutOfRangeException)
            {
                ape = new TagLib.Ape.Tag();
            }

            _WriteSemanticSurfaceToTag(ape, c.ToSemanticSurface());
            c.Ape = _SerializedBlob(ape.Render().Data);
        }

        private static void _MergeSemanticIntoRiff(CoalesceCarrier c)
        {
            if (c.RiffInfo is null)
                return;

            InfoTag info;
            try
            {
                info = new InfoTag(_ToByteVector(c.RiffInfo.CanonicalTagBytes));
            }
            catch (CorruptFileException)
            {
                info = new InfoTag();
            }
            catch (ArgumentOutOfRangeException)
            {
                info = new InfoTag();
            }

            _WriteSemanticSurfaceToTag(info, c.ToSemanticSurface());
            c.RiffInfo = _SerializedBlob(info.Render().Data);
        }

        private static void _MergeSemanticIntoAsf(CoalesceCarrier c)
        {
            if (c.Asf is null)
                return;

            var surface = c.ToSemanticSurface();
            var asf = new TagLib.Asf.Tag();
            foreach (var row in c.Asf.Descriptors)
                asf.AddDescriptor(new TagLib.Asf.ContentDescriptor(row.Name, row.Value));

            _WriteSemanticSurfaceToTag(asf, surface);
            if (!string.IsNullOrWhiteSpace(surface.Title))
                asf.SetDescriptorString(surface.Title.Trim(), "WM/Title");

            c.Asf = _ReadAsfTagData(asf);
        }

        private static void _MergeSemanticIntoApple(AppleTag apple, CoalesceCarrier c)
        {
            _SetAppleAtomTextRows(apple, c.Apple!);
            _WriteSemanticSurfaceToTag(apple, c.ToSemanticSurface());
            c.Apple = _ReadAppleTagData(apple) ?? new AppleTagData();
        }

        private static void _MergeSemanticIntoId3v1(CoalesceCarrier c)
        {
            if (c.Id3v1 is null)
                return;

            var parts = _SplitJoinedList(c.Performers);
            var artist = parts.Length > 0 ? parts[0] : null;

            var genreByte = string.IsNullOrWhiteSpace(c.Genre)
                ? (byte)0
                : Genres.AudioToIndex(c.Genre.Trim());

            byte? track = c.Track is null ? null : (byte)System.Math.Min(c.Track.Value, 255u);

            c.Id3v1 = new Id3v1TagData
            {
                Title = _NullIfEmpty(c.Title),
                Artist = _NullIfEmpty(artist),
                Album = _NullIfEmpty(c.Album),
                Year = c.Year,
                Comment = _NullIfEmpty(c.Comment),
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

            var hasWmTitle = rows.Exists(static r =>
                string.Equals(r.Name, "WM/Title", StringComparison.Ordinal));

            var titleFromFaçade = string.IsNullOrWhiteSpace(asf.Title) ? null : asf.Title.Trim();
            if (!hasWmTitle && titleFromFaçade is not null)
                rows.Add(new AsfDescriptorRow("WM/Title", titleFromFaçade));

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
            _ApplyRiff(file, overlay);
            _ApplyApple(file, overlay);
            _ApplyAsf(file, overlay);
        }

        /// <summary>
        /// Copies fields from a parsed in-memory tag into the on-disk Xiph comment (same key set as the overlay blob).
        /// </summary>
        private static void _ApplyXiph(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.Xiph is null)
                return;

            if (file.GetTag(TagTypes.Xiph, true) is not XiphComment live)
                return;

            var parsed = new XiphComment(_ToByteVector(overlay.Xiph.CanonicalTagBytes));
            live.Clear();

            foreach (var key in parsed)
            {
                var values = parsed.GetField(key);
                if (values.Length == 0)
                    continue;

                live.SetField(key, values);
            }
        }

        /// <summary>
        /// Copies items from a parsed in-memory APE tag into the on-disk APE block.
        /// </summary>
        private static void _ApplyApe(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.Ape is null)
                return;

            if (file.GetTag(TagTypes.Ape, true) is not TagLib.Ape.Tag live)
                return;

            var parsed = new TagLib.Ape.Tag(_ToByteVector(overlay.Ape.CanonicalTagBytes));
            live.Clear();

            foreach (var key in parsed)
            {
                var item = parsed.GetItem(key);
                if (item is not null)
                    live.SetItem(item);
            }
        }

        /// <summary>
        /// Copies fields from a parsed in-memory RIFF INFO list into the on-disk INFO block.
        /// </summary>
        private static void _ApplyRiff(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.RiffInfo is null)
                return;

            if (file.GetTag(TagTypes.RiffInfo, true) is not InfoTag live)
                return;

            InfoTag parsed;
            try
            {
                parsed = new InfoTag(_ToByteVector(overlay.RiffInfo.CanonicalTagBytes));
            }
            catch (CorruptFileException)
            {
                return;
            }
            catch (ArgumentOutOfRangeException)
            {
                return;
            }

            live.Clear();
            _WriteSemanticSurfaceToTag(live, AudioTagSemanticSurface.FromCombinedTag(parsed));
        }

        private static void _ApplyApple(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.Apple is null)
                return;

            if (file.GetTag(TagTypes.Apple, true) is not AppleTag apple)
                return;

            _SetAppleAtomTextRows(apple, overlay.Apple);
        }

        private static void _SetAppleAtomTextRows(AppleTag apple, AppleTagData appleData)
        {
            foreach (var row in appleData.Atoms)
                apple.SetText([.. row.AtomType.ToArray()], [.. row.Values]);
        }

        private static void _ApplyAsf(TagLib.File file, AudioTagOverlay overlay)
        {
            if (overlay.Asf is null)
                return;

            if (file.GetTag(TagTypes.Asf, true) is not TagLib.Asf.Tag asf)
                return;

            // Do not Clear(): wiping the ASF tag can leave Container objects in TagLib inconsistent (Save throws
            // "Size is less than zero"). Overlay rows update/replace descriptors in place instead.
            foreach (var row in overlay.Asf.Descriptors)
            {
                if (string.IsNullOrEmpty(row.Name))
                    continue;

                asf.RemoveDescriptors(row.Name);
                asf.AddDescriptor(new TagLib.Asf.ContentDescriptor(row.Name, row.Value));
            }
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
                    var vec = _ToByteVector(blob.Data);
                    var frame = TagLib.Id3v2.FrameFactory.CreateFrame(vec, file, ref offset, overlay.Id3v2.Version, false);
                    if (frame is not null)
                        id3v2.AddFrame(frame);
                }
            }

            if (overlay.Id3v1 is not null)
                _WriteId3v1Tag(file, overlay.Id3v1);

            _WriteSemanticSurfaceToTag(file.Tag, AudioTagSemanticSurface.FromBlocks(overlay));
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

        /// <summary>
        /// Applies merged semantic scalar fields onto TagLib's combined façade tag (routing into container-specific payloads).
        /// </summary>
        private static void _WriteSemanticSurfaceToTag(Tag tag, AudioTagSemanticSurface surface)
        {
            tag.Title = _EmptyStringToNull(surface.Title);
            tag.Album = _EmptyStringToNull(surface.Album);
            tag.Performers = _SplitJoinedList(surface.Performers);
            tag.AlbumArtists = _SplitJoinedList(surface.AlbumArtists);
            tag.Composers = _SplitJoinedList(surface.Composers);
            tag.Genres = string.IsNullOrWhiteSpace(surface.Genre)
                ? []
                : [surface.Genre.Trim()];

            tag.Comment = _EmptyStringToNull(surface.Comment);
            tag.Lyrics = _EmptyStringToNull(surface.Lyrics);
            tag.Copyright = _EmptyStringToNull(surface.Copyright);
            tag.Grouping = _EmptyStringToNull(surface.Grouping);

            tag.Year = surface.Year ?? 0;
            tag.Track = surface.Track ?? 0;
            tag.TrackCount = surface.TrackCount ?? 0;
            tag.Disc = surface.Disc ?? 0;
            tag.DiscCount = surface.DiscCount ?? 0;
        }

        private static string? _EmptyStringToNull(string? text)
        {
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private static SerializedTagBlob _SerializedBlob(byte[] data)
        {
            return new SerializedTagBlob { CanonicalTagBytes = ImmutableArray.Create(data) };
        }

        private static ByteVector _ToByteVector(ImmutableArray<byte> bytes)
        {
            return new ByteVector([.. bytes]);
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

        private static string[] _SplitJoinedList(string? joined)
        {
            if (string.IsNullOrWhiteSpace(joined))
                return [];

            return [.. joined.Split(_ListSeparators, StringSplitOptions.TrimEntries)
                .Where(part => !string.IsNullOrEmpty(part))
                .Select(part => part.Trim())];
        }

        private static string? _NullIfEmpty(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }
    }
}
