using System.Collections.Immutable;
using System.Globalization;
using Mfr.Models.Tags.Ape;
using Mfr.Models.Tags.Apple;
using Mfr.Models.Tags.Asf;
using Mfr.Models.Tags.Id3v1;
using Mfr.Models.Tags.Id3v2;
using Mfr.Models.Tags.RiffInfo;
using Mfr.Models.Tags.Xiph;
using Mfr.Utils;

namespace Mfr.Models.Tags
{
    /// <summary>
    /// Broadcasts a <see cref="SemanticAudioTag"/> onto present overlay blocks (no TagLib).
    /// </summary>
    public static class AudioTagSemanticMerge
    {
        /// <summary>
        /// Applies <paramref name="semantic"/> onto every present block; empty→absent; prunes empty modeled blocks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Does not create blocks. Callers that need recommended-block create should use
        /// <see cref="AudioTagOverlay.MergeSemantic"/> instead.
        /// </para>
        /// </remarks>
        /// <param name="overlay">Overlay whose present blocks are updated in place.</param>
        /// <param name="semantic">Desired semantic fields to write into present blocks.</param>
        public static void MergeIntoPresentBlocks(AudioTagOverlay overlay, SemanticAudioTag semantic)
        {
            ArgumentNullException.ThrowIfNull(overlay);
            ArgumentNullException.ThrowIfNull(semantic);

            if (overlay.Id3v1 is not null)
                overlay.Id3v1 = _MergeId3v1(overlay.Id3v1, semantic);

            if (overlay.Id3v2 is not null)
                overlay.Id3v2 = _MergeId3v2(overlay.Id3v2, semantic);

            if (overlay.Xiph is not null)
                overlay.Xiph = _MergeXiph(overlay.Xiph, semantic);

            if (overlay.Ape is not null)
                overlay.Ape = _MergeApe(overlay.Ape, semantic);

            if (overlay.RiffInfo is not null)
                overlay.RiffInfo = _MergeRiff(semantic);

            if (overlay.Asf is not null)
                overlay.Asf = _MergeAsf(overlay.Asf, semantic);

            if (overlay.Apple is not null)
                overlay.Apple = _MergeApple(overlay.Apple, semantic);
        }

        private static Id3v1TagData? _MergeId3v1(Id3v1TagData existing, SemanticAudioTag common)
        {
            var parts = DelimitedText.Split(common.Performers);
            var artist = parts.Length > 0 ? parts[0] : null;
            var genreByte = string.IsNullOrWhiteSpace(common.Genre)
                ? (byte)0
                : Id3v1Genres.AudioToIndex(common.Genre.Trim());
            byte? track = common.Track is null ? null : (byte)Math.Min(common.Track.Value, 255u);

            var merged = new Id3v1TagData
            {
                Title = common.Title.TrimmedOrNull(),
                Artist = artist.TrimmedOrNull(),
                Album = common.Album.TrimmedOrNull(),
                Year = common.Year,
                Comment = common.Comment.TrimmedOrNull(),
                Track = track,
                Genre = genreByte,
            };

            return _IsId3v1Empty(merged) ? null : merged;
        }

        private static Id3v2TagData? _MergeId3v2(Id3v2TagData existing, SemanticAudioTag common)
        {
            var frames = existing.Frames.ToList();
            _SetSingleton(frames, "TIT2", common.Title);
            _SetSingleton(frames, "TALB", common.Album);
            _SetList(frames, "TPE1", common.Performers);
            _SetList(frames, "TPE2", common.AlbumArtists);
            _SetList(frames, "TCOM", common.Composers);
            _SetSingleton(frames, "TCON", common.Genre);
            _SetSingleton(frames, "TCOP", common.Copyright);
            _SetSingleton(frames, "TIT1", common.Grouping);
            _SetPrimaryMulti(frames, "COMM", common.Comment);
            _SetPrimaryMulti(frames, "USLT", common.Lyrics);
            _SetYear(frames, existing.Version, common.Year);
            _SetTrackPair(frames, "TRCK", common.Track, common.TrackCount);
            _SetTrackPair(frames, "TPOS", common.Disc, common.DiscCount);

            frames.Sort(_CompareId3v2Frames);
            // Preserve an intentionally empty Id3v2 block (create/recommended target) until fields are set or the
            // block is explicitly nulled by a remover. Prune only when the prior snapshot already had modeled frames
            // and this merge cleared them all.
            if (frames.Count == 0 && existing.Frames.Length > 0)
                return null;

            return new Id3v2TagData { Version = existing.Version, Frames = [.. frames] };
        }

        private static XiphTagData? _MergeXiph(XiphTagData existing, SemanticAudioTag common)
        {
            var map = _ToMutableMultimap(existing.Fields);
            _SetMapScalar(map, "TITLE", common.Title);
            _SetMapScalar(map, "ALBUM", common.Album);
            _SetMapList(map, "ARTIST", common.Performers);
            _SetMapList(map, "ALBUMARTIST", common.AlbumArtists);
            _SetMapList(map, "COMPOSER", common.Composers);
            _SetMapScalar(map, "GENRE", common.Genre);
            _SetMapScalar(map, "DESCRIPTION", common.Comment);
            map.Remove("COMMENT");
            _SetMapScalar(map, "LYRICS", common.Lyrics);
            map.Remove("UNSYNCEDLYRICS");
            _SetMapScalar(map, "COPYRIGHT", common.Copyright);
            _SetMapScalar(map, "GROUPING", common.Grouping);
            map.Remove("CONTENTGROUP");
            _SetMapScalar(map, "DATE", common.Year?.ToString(CultureInfo.InvariantCulture));
            map.Remove("YEAR");
            _SetMapScalar(map, "TRACKNUMBER", common.Track?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "TRACKTOTAL", common.TrackCount?.ToString(CultureInfo.InvariantCulture));
            map.Remove("TOTALTRACKS");
            _SetMapScalar(map, "DISCNUMBER", common.Disc?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "DISCTOTAL", common.DiscCount?.ToString(CultureInfo.InvariantCulture));
            map.Remove("TOTALDISCS");

            var rows = _SortedRows(map);
            return rows.Length == 0 ? null : new XiphTagData { Fields = rows };
        }

        private static ApeTagData? _MergeApe(ApeTagData existing, SemanticAudioTag common)
        {
            var map = _ToMutableMultimap(existing.Fields);
            _SetMapScalar(map, "Title", common.Title);
            _SetMapScalar(map, "Album", common.Album);
            _SetMapList(map, "Artist", common.Performers);
            _SetMapList(map, "Album Artist", common.AlbumArtists);
            _SetMapList(map, "Composer", common.Composers);
            _SetMapScalar(map, "Genre", common.Genre);
            _SetMapScalar(map, "Comment", common.Comment);
            _SetMapScalar(map, "Lyrics", common.Lyrics);
            _SetMapScalar(map, "Copyright", common.Copyright);
            _SetMapScalar(map, "Grouping", common.Grouping);
            _SetMapScalar(map, "Year", common.Year?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "Track", common.Track?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "TrackCount", common.TrackCount?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "Disc", common.Disc?.ToString(CultureInfo.InvariantCulture));
            _SetMapScalar(map, "DiscCount", common.DiscCount?.ToString(CultureInfo.InvariantCulture));

            var rows = _SortedRows(map);
            return rows.Length == 0 ? null : new ApeTagData { Fields = rows };
        }

        private static RiffInfoTagData? _MergeRiff(SemanticAudioTag common)
        {
            var rows = _RiffRowsFromCommon(common);
            return rows.Length == 0 ? null : new RiffInfoTagData { Fields = rows };
        }

        private static AsfTagData? _MergeAsf(AsfTagData existing, SemanticAudioTag common)
        {
            var rows = existing.Descriptors.ToList();

            // Drop non-canonical names left by older writes or foreign tools for fields we model.
            _RemoveAsf(rows, "WM/Title");
            _RemoveAsf(rows, "WM/Author");
            _RemoveAsf(rows, "WM/Description");
            _RemoveAsf(rows, "WM/ProviderCopyright");
            _RemoveAsf(rows, "WM/TrackTotal");
            _RemoveAsf(rows, "WM/TotalDiscs");

            _SetAsf(rows, AsfDescriptorNames.Title, common.Title);
            _SetAsf(rows, AsfDescriptorNames.Album, common.Album);
            _SetAsf(rows, AsfDescriptorNames.Author, common.Performers);
            _SetAsf(rows, AsfDescriptorNames.AlbumArtist, common.AlbumArtists);
            _SetAsf(rows, AsfDescriptorNames.Composer, common.Composers);
            _SetAsf(rows, AsfDescriptorNames.Genre, common.Genre);
            _SetAsf(rows, AsfDescriptorNames.Comment, common.Comment);
            _SetAsf(rows, AsfDescriptorNames.Lyrics, common.Lyrics);
            _SetAsf(rows, AsfDescriptorNames.Copyright, common.Copyright);
            _SetAsf(rows, AsfDescriptorNames.Grouping, common.Grouping);
            _SetAsf(rows, AsfDescriptorNames.Year, common.Year?.ToString(CultureInfo.InvariantCulture));
            _SetAsf(rows, AsfDescriptorNames.TrackNumber, common.Track?.ToString(CultureInfo.InvariantCulture));
            _SetAsf(rows, AsfDescriptorNames.TrackTotal, common.TrackCount?.ToString(CultureInfo.InvariantCulture));
            _SetAsfPartOfSet(rows, common.Disc, common.DiscCount);

            if (rows.Count == 0)
                return null;

            rows.Sort(static (a, b) =>
            {
                var byName = string.CompareOrdinal(a.Name, b.Name);
                return byName != 0 ? byName : string.CompareOrdinal(a.Value, b.Value);
            });

            return new AsfTagData { Descriptors = [.. rows] };
        }

        private static AppleTagData? _MergeApple(AppleTagData existing, SemanticAudioTag common)
        {
            var atoms = existing.Atoms.ToList();
            _SetAppleAtom(atoms, AppleAtomIds.Title, common.Title);
            _SetAppleAtom(atoms, AppleAtomIds.Album, common.Album);
            _SetAppleAtomList(atoms, AppleAtomIds.Artist, common.Performers);
            _SetAppleAtomList(atoms, AppleAtomIds.AlbumArtist, common.AlbumArtists);
            _SetAppleAtomList(atoms, AppleAtomIds.Composer, common.Composers);
            _SetAppleAtom(atoms, AppleAtomIds.Genre, common.Genre);
            _SetAppleAtom(atoms, AppleAtomIds.Comment, common.Comment);
            _SetAppleAtom(atoms, AppleAtomIds.Lyrics, common.Lyrics);
            _SetAppleAtom(atoms, AppleAtomIds.Copyright, common.Copyright);
            _SetAppleAtom(atoms, AppleAtomIds.Grouping, common.Grouping);
            _SetAppleAtom(atoms, AppleAtomIds.Day, common.Year?.ToString(CultureInfo.InvariantCulture));

            if (atoms.Count == 0)
                return null;

            atoms.Sort(static (a, b) =>
            {
                var byType = a.AtomType.AsSpan().SequenceCompareTo(b.AtomType.AsSpan());
                if (byType != 0)
                    return byType;

                return OrdinalSequence.Compare(a.Values, b.Values);
            });

            return new AppleTagData { Atoms = [.. atoms] };
        }

        private static void _SetSingleton(List<Id3v2ModeledFrame> frames, string frameId, string? value)
        {
            frames.RemoveAll(f => string.Equals(f.FrameId, frameId, StringComparison.Ordinal));
            var text = value.TrimmedOrNull();
            if (text is null)
                return;

            frames.Add(new Id3v2ModeledFrame { FrameId = frameId, TextValues = [text] });
        }

        private static void _SetList(List<Id3v2ModeledFrame> frames, string frameId, string? joined)
        {
            frames.RemoveAll(f => string.Equals(f.FrameId, frameId, StringComparison.Ordinal));
            var values = DelimitedText.Split(joined);
            if (values.Length == 0)
                return;

            frames.Add(new Id3v2ModeledFrame { FrameId = frameId, TextValues = values });
        }

        private static void _SetPrimaryMulti(List<Id3v2ModeledFrame> frames, string frameId, string? value)
        {
            var primaryIndex = frames.FindIndex(f =>
                string.Equals(f.FrameId, frameId, StringComparison.Ordinal)
                && string.IsNullOrEmpty(f.Description));

            var text = value.TrimmedOrNull();
            if (text is null)
            {
                if (primaryIndex >= 0)
                    frames.RemoveAt(primaryIndex);

                return;
            }

            var replacement = new Id3v2ModeledFrame
            {
                FrameId = frameId,
                Language = primaryIndex >= 0 ? frames[primaryIndex].Language : "eng",
                Description = null,
                TextValues = [text],
            };

            if (primaryIndex >= 0)
                frames[primaryIndex] = replacement;
            else
                frames.Add(replacement);
        }

        private static void _SetYear(List<Id3v2ModeledFrame> frames, byte version, uint? year)
        {
            frames.RemoveAll(f =>
                string.Equals(f.FrameId, "TYER", StringComparison.Ordinal)
                || string.Equals(f.FrameId, "TDRC", StringComparison.Ordinal));

            if (year is null)
                return;

            var frameId = version >= 4 ? "TDRC" : "TYER";
            frames.Add(new Id3v2ModeledFrame
            {
                FrameId = frameId,
                TextValues = [year.Value.ToString(CultureInfo.InvariantCulture)],
            });
        }

        private static void _SetTrackPair(List<Id3v2ModeledFrame> frames, string frameId, uint? number, uint? count)
        {
            frames.RemoveAll(f => string.Equals(f.FrameId, frameId, StringComparison.Ordinal));
            if (number is null && count is null)
                return;

            var text = number is null
                ? "0/" + count!.Value.ToString(CultureInfo.InvariantCulture)
                : count is null
                ? number.Value.ToString(CultureInfo.InvariantCulture)
                : number.Value.ToString(CultureInfo.InvariantCulture) + "/" + count.Value.ToString(CultureInfo.InvariantCulture);
            frames.Add(new Id3v2ModeledFrame { FrameId = frameId, TextValues = [text] });
        }

        private static ImmutableArray<RiffInfoFieldRow> _RiffRowsFromCommon(SemanticAudioTag common)
        {
            var rows = new List<RiffInfoFieldRow>();
            _AddRiff(rows, "INAM", common.Title);
            _AddRiff(rows, "IPRD", common.Album);
            _AddRiff(rows, "IART", common.Performers);
            _AddRiff(rows, "IGNR", common.Genre);
            _AddRiff(rows, "ICMT", common.Comment);
            _AddRiff(rows, "ICOP", common.Copyright);
            _AddRiff(rows, "ICRD", common.Year?.ToString(CultureInfo.InvariantCulture));
            _AddRiff(rows, "ITRK", common.Track?.ToString(CultureInfo.InvariantCulture));
            rows.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));
            return [.. rows];
        }

        private static void _AddRiff(List<RiffInfoFieldRow> rows, string key, string? value)
        {
            var text = value.TrimmedOrNull();
            if (text is null)
                return;

            rows.Add(new RiffInfoFieldRow(key, text));
        }

        private static void _SetAsf(List<AsfDescriptorRow> rows, string name, string? value)
        {
            rows.RemoveAll(r => string.Equals(r.Name, name, StringComparison.Ordinal));
            var text = value.TrimmedOrNull();
            if (text is null)
                return;

            rows.Add(new AsfDescriptorRow(name, text));
        }

        private static void _RemoveAsf(List<AsfDescriptorRow> rows, string name)
        {
            rows.RemoveAll(r => string.Equals(r.Name, name, StringComparison.Ordinal));
        }

        private static void _SetAsfPartOfSet(List<AsfDescriptorRow> rows, uint? disc, uint? discCount)
        {
            _RemoveAsf(rows, AsfDescriptorNames.PartOfSet);
            if (disc is null && discCount is null)
                return;

            if (disc is not null && discCount is not null)
            {
                rows.Add(new AsfDescriptorRow(
                    AsfDescriptorNames.PartOfSet,
                    string.Format(CultureInfo.InvariantCulture, "{0}/{1}", disc.Value, discCount.Value)));
                return;
            }

            if (disc is not null)
            {
                rows.Add(new AsfDescriptorRow(
                    AsfDescriptorNames.PartOfSet,
                    disc.Value.ToString(CultureInfo.InvariantCulture)));
                return;
            }

            // TagLib encodes count-only as "0/{count}".
            rows.Add(new AsfDescriptorRow(
                AsfDescriptorNames.PartOfSet,
                string.Format(CultureInfo.InvariantCulture, "0/{0}", discCount!.Value)));
        }

        private static void _SetAppleAtom(List<AppleAtomRow> atoms, ReadOnlySpan<byte> atomType, string? value)
        {
            var typeBytes = atomType.ToArray();
            atoms.RemoveAll(a => a.AtomType.AsSpan().SequenceEqual(typeBytes));
            var text = value.TrimmedOrNull();
            if (text is null)
                return;

            atoms.Add(new AppleAtomRow
            {
                AtomType = ImmutableArray.Create(typeBytes),
                Values = [text],
            });
        }

        private static void _SetAppleAtomList(List<AppleAtomRow> atoms, ReadOnlySpan<byte> atomType, string? joined)
        {
            var typeBytes = atomType.ToArray();
            atoms.RemoveAll(a => a.AtomType.AsSpan().SequenceEqual(typeBytes));
            var values = DelimitedText.Split(joined);
            if (values.Length == 0)
                return;

            atoms.Add(new AppleAtomRow
            {
                AtomType = ImmutableArray.Create(typeBytes),
                Values = values,
            });
        }

        private static Dictionary<string, ImmutableArray<string>> _ToMutableMultimap(ImmutableArray<TextFieldRow> fields)
        {
            var map = new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal);
            foreach (var row in fields)
                map[row.Key] = row.Values;

            return map;
        }

        private static void _SetMapScalar(Dictionary<string, ImmutableArray<string>> map, string key, string? value)
        {
            var text = value.TrimmedOrNull();
            if (text is null)
            {
                map.Remove(key);
                return;
            }

            map[key] = [text];
        }

        private static void _SetMapList(Dictionary<string, ImmutableArray<string>> map, string key, string? joined)
        {
            var values = DelimitedText.Split(joined);
            if (values.Length == 0)
            {
                map.Remove(key);
                return;
            }

            map[key] = values;
        }

        private static ImmutableArray<TextFieldRow> _SortedRows(Dictionary<string, ImmutableArray<string>> map)
        {
            var rows = map
                .Select(static kvp => new TextFieldRow(kvp.Key, kvp.Value))
                .ToList();
            rows.Sort(_CompareTextFieldRows);
            return [.. rows];
        }

        private static int _CompareTextFieldRows(TextFieldRow a, TextFieldRow b)
        {
            var byKey = string.CompareOrdinal(a.Key, b.Key);
            if (byKey != 0)
                return byKey;

            return OrdinalSequence.Compare(a.Values, b.Values);
        }

        private static int _CompareId3v2Frames(Id3v2ModeledFrame a, Id3v2ModeledFrame b)
        {
            var byId = string.CompareOrdinal(a.FrameId, b.FrameId);
            if (byId != 0)
                return byId;

            var byLang = string.CompareOrdinal(a.Language, b.Language);
            if (byLang != 0)
                return byLang;

            var byDesc = string.CompareOrdinal(a.Description, b.Description);
            if (byDesc != 0)
                return byDesc;

            return OrdinalSequence.Compare(a.TextValues, b.TextValues);
        }

        private static bool _IsId3v1Empty(Id3v1TagData data)
        {
            return string.IsNullOrWhiteSpace(data.Title)
                && string.IsNullOrWhiteSpace(data.Artist)
                && string.IsNullOrWhiteSpace(data.Album)
                && data.Year is null
                && string.IsNullOrWhiteSpace(data.Comment)
                && data.Track is null
                && data.Genre == 0;
        }
    }
}
