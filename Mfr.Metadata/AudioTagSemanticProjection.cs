using System.Collections.Immutable;
using Mfr.Models.Tags;
using TagLib;
using TagLib.Ogg;

namespace Mfr.Metadata
{
    /// <summary>
    /// Semantic values derived from <see cref="AudioTagOverlay"/> native blocks (Phase 4), with optional merged façade fill-in via <see cref="FromOverlay"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="FromOverlay(AudioTagOverlay)"/> prefers structured blocks (ID3v2 over ID3v1, then Xiph, APE,
    /// Apple text atoms, ASF descriptors); when blocks omit a field, merged façade scalars fill the remainder.
    /// Use <see cref="FromNativeBlocksOnly(AudioTagOverlay)"/> when callers must ignore stored façade duplicates.
    /// </para>
    /// </remarks>
    /// <param name="Title">Visible title, if any native block supplies one.</param>
    /// <param name="Album">Album name.</param>
    /// <param name="Performers">Performers joined with <c>; </c> (TagLib list convention).</param>
    /// <param name="AlbumArtists">Album artists joined with <c>; </c>.</param>
    /// <param name="Composers">Composers joined with <c>; </c>.</param>
    /// <param name="Genre">Primary genre string.</param>
    /// <param name="Comment">Comment.</param>
    /// <param name="Lyrics">Lyrics.</param>
    /// <param name="Copyright">Copyright.</param>
    /// <param name="Grouping">Grouping.</param>
    /// <param name="Year">Year when non-zero in source tags.</param>
    /// <param name="Track">Track number.</param>
    /// <param name="TrackCount">Track count.</param>
    /// <param name="Disc">Disc number.</param>
    /// <param name="DiscCount">Disc count.</param>
    public sealed record AudioTagSemanticSurface(
        string? Title,
        string? Album,
        string? Performers,
        string? AlbumArtists,
        string? Composers,
        string? Genre,
        string? Comment,
        string? Lyrics,
        string? Copyright,
        string? Grouping,
        uint? Year,
        uint? Track,
        uint? TrackCount,
        uint? Disc,
        uint? DiscCount)
    {
        /// <summary>
        /// Builds effective semantics from native blocks on <paramref name="overlay"/>, then fills remaining fields from merged façade scalars.
        /// </summary>
        /// <param name="overlay">Overlay whose blocks and façade are interpreted; must not be <see langword="null"/>.</param>
        /// <returns>Projected surface; façade values apply only where no native block supplied a value.</returns>
        public static AudioTagSemanticSurface FromOverlay(AudioTagOverlay overlay)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var id3v2 = _TryParseId3v2(overlay.Id3v2);
            var xiph = _TryParseXiph(overlay.Xiph);
            var ape = _TryParseApe(overlay.Ape);
            var asf = _TryBuildAsfTag(overlay.Asf);

            var id3v1 = overlay.Id3v1;

            return new AudioTagSemanticSurface(
                Title: _CoalesceUnicode(
                    _TagTitle(id3v2),
                    _Id3v1String(id3v1?.Title),
                    _TagTitle(xiph),
                    _TagTitle(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.TitleAtom),
                    _AsfUnicode(asf, "WM/Title"),
                    _NullIfWhitespace(overlay.Title)),
                Album: _CoalesceUnicode(
                    _TagAlbum(id3v2),
                    _Id3v1String(id3v1?.Album),
                    _TagAlbum(xiph),
                    _TagAlbum(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.AlbumAtom),
                    _AsfUnicode(asf, "WM/AlbumTitle"),
                    _NullIfWhitespace(overlay.Album)),
                Performers: _CoalesceJoinedList(
                    id3v2?.Performers,
                    _Id3v1SplitPerformer(id3v1?.Artist),
                    xiph?.Performers,
                    ape?.Performers,
                    _AppleJoinedList(overlay.Apple, AppleAtomConstants.ArtistAtom),
                    _AsfJoinedPerformers(asf),
                    _SplitJoinedListForOverlay(overlay.Performers)),
                AlbumArtists: _CoalesceJoinedList(
                    id3v2?.AlbumArtists,
                    null,
                    xiph?.AlbumArtists,
                    ape?.AlbumArtists,
                    _AppleJoinedList(overlay.Apple, AppleAtomConstants.AlbumArtistAtom),
                    _AsfJoinedList(asf, "WM/AlbumArtist"),
                    _SplitJoinedListForOverlay(overlay.AlbumArtists)),
                Composers: _CoalesceJoinedList(
                    id3v2?.Composers,
                    null,
                    xiph?.Composers,
                    ape?.Composers,
                    _AppleJoinedList(overlay.Apple, AppleAtomConstants.ComposerAtom),
                    _AsfJoinedList(asf, "WM/Composer"),
                    _SplitJoinedListForOverlay(overlay.Composers)),
                Genre: _CoalesceUnicode(
                    _TagFirstGenre(id3v2),
                    _Id3v1Genre(id3v1),
                    _TagFirstGenre(xiph),
                    _TagFirstGenre(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.GenreAtom),
                    _AsfUnicode(asf, "WM/Genre"),
                    _NullIfWhitespace(overlay.Genre)),
                Comment: _CoalesceUnicode(
                    _TagComment(id3v2),
                    _Id3v1String(id3v1?.Comment),
                    _TagComment(xiph),
                    _TagComment(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.CommentAtom),
                    _AsfUnicode(asf, "WM/Description"),
                    _NullIfWhitespace(overlay.Comment)),
                Lyrics: _CoalesceUnicode(
                    _TagLyrics(id3v2),
                    null,
                    _TagLyrics(xiph),
                    _TagLyrics(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.LyricsAtom),
                    _AsfUnicode(asf, "WM/Lyrics"),
                    _NullIfWhitespace(overlay.Lyrics)),
                Copyright: _CoalesceUnicode(
                    _TagCopyright(id3v2),
                    null,
                    _TagCopyright(xiph),
                    _TagCopyright(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.CopyrightAtom),
                    _AsfUnicode(asf, "WM/ProviderCopyright"),
                    _NullIfWhitespace(overlay.Copyright)),
                Grouping: _CoalesceUnicode(
                    _TagGrouping(id3v2),
                    null,
                    _TagGrouping(xiph),
                    _TagGrouping(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.GroupingAtom),
                    _AsfUnicode(asf, "WM/ContentGroupDescription"),
                    _NullIfWhitespace(overlay.Grouping)),
                Year: _CoalesceUInt(
                    _TagYear(id3v2),
                    id3v1?.Year,
                    _TagYear(xiph),
                    _TagYear(ape),
                    _AppleYear(overlay.Apple),
                    _AsfUInt(asf, "WM/Year"),
                    overlay.Year),
                Track: _CoalesceUInt(
                    _TagTrack(id3v2),
                    id3v1?.Track is null ? null : id3v1.Track,
                    _TagTrack(xiph),
                    _TagTrack(ape),
                    _AppleTrack(overlay.Apple),
                    _AsfUInt(asf, "WM/TrackNumber"),
                    overlay.Track),
                TrackCount: _CoalesceUInt(
                    _TagTrackCount(id3v2),
                    null,
                    _TagTrackCount(xiph),
                    _TagTrackCount(ape),
                    _AppleTrackCount(overlay.Apple),
                    _AsfUInt(asf, "WM/TrackTotal"),
                    overlay.TrackCount),
                Disc: _CoalesceUInt(
                    _TagDisc(id3v2),
                    null,
                    _TagDisc(xiph),
                    _TagDisc(ape),
                    _AppleDisc(overlay.Apple),
                    _AsfUInt(asf, "WM/PartOfSet"),
                    overlay.Disc),
                DiscCount: _CoalesceUInt(
                    _TagDiscCount(id3v2),
                    null,
                    _TagDiscCount(xiph),
                    _TagDiscCount(ape),
                    _AppleDiscCount(overlay.Apple),
                    _AsfUInt(asf, "WM/TotalDiscs"),
                    overlay.DiscCount));
        }

        /// <summary>
        /// Like <see cref="FromOverlay"/> but uses only structured tag blocks (no merged façade fallback).
        /// </summary>
        /// <param name="overlay">Overlay whose blocks are interpreted; must not be <see langword="null"/>.</param>
        /// <returns>Projected surface from blocks alone.</returns>
        public static AudioTagSemanticSurface FromNativeBlocksOnly(AudioTagOverlay overlay)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var id3v2 = _TryParseId3v2(overlay.Id3v2);
            var xiph = _TryParseXiph(overlay.Xiph);
            var ape = _TryParseApe(overlay.Ape);
            var asf = _TryBuildAsfTag(overlay.Asf);

            var id3v1 = overlay.Id3v1;

            return new AudioTagSemanticSurface(
                Title: _CoalesceUnicode(
                    _TagTitle(id3v2),
                    _Id3v1String(id3v1?.Title),
                    _TagTitle(xiph),
                    _TagTitle(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.TitleAtom),
                    _AsfUnicode(asf, "WM/Title")),
                Album: _CoalesceUnicode(
                    _TagAlbum(id3v2),
                    _Id3v1String(id3v1?.Album),
                    _TagAlbum(xiph),
                    _TagAlbum(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.AlbumAtom),
                    _AsfUnicode(asf, "WM/AlbumTitle")),
                Performers: _CoalesceJoinedList(
                    id3v2?.Performers,
                    _Id3v1SplitPerformer(id3v1?.Artist),
                    xiph?.Performers,
                    ape?.Performers,
                    _AppleJoinedList(overlay.Apple, AppleAtomConstants.ArtistAtom),
                    _AsfJoinedPerformers(asf)),
                AlbumArtists: _CoalesceJoinedList(
                    id3v2?.AlbumArtists,
                    null,
                    xiph?.AlbumArtists,
                    ape?.AlbumArtists,
                    _AppleJoinedList(overlay.Apple, AppleAtomConstants.AlbumArtistAtom),
                    _AsfJoinedList(asf, "WM/AlbumArtist")),
                Composers: _CoalesceJoinedList(
                    id3v2?.Composers,
                    null,
                    xiph?.Composers,
                    ape?.Composers,
                    _AppleJoinedList(overlay.Apple, AppleAtomConstants.ComposerAtom),
                    _AsfJoinedList(asf, "WM/Composer")),
                Genre: _CoalesceUnicode(
                    _TagFirstGenre(id3v2),
                    _Id3v1Genre(id3v1),
                    _TagFirstGenre(xiph),
                    _TagFirstGenre(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.GenreAtom),
                    _AsfUnicode(asf, "WM/Genre")),
                Comment: _CoalesceUnicode(
                    _TagComment(id3v2),
                    _Id3v1String(id3v1?.Comment),
                    _TagComment(xiph),
                    _TagComment(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.CommentAtom),
                    _AsfUnicode(asf, "WM/Description")),
                Lyrics: _CoalesceUnicode(
                    _TagLyrics(id3v2),
                    null,
                    _TagLyrics(xiph),
                    _TagLyrics(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.LyricsAtom),
                    _AsfUnicode(asf, "WM/Lyrics")),
                Copyright: _CoalesceUnicode(
                    _TagCopyright(id3v2),
                    null,
                    _TagCopyright(xiph),
                    _TagCopyright(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.CopyrightAtom),
                    _AsfUnicode(asf, "WM/ProviderCopyright")),
                Grouping: _CoalesceUnicode(
                    _TagGrouping(id3v2),
                    null,
                    _TagGrouping(xiph),
                    _TagGrouping(ape),
                    _ApplePlainText(overlay.Apple, AppleAtomConstants.GroupingAtom),
                    _AsfUnicode(asf, "WM/ContentGroupDescription")),
                Year: _CoalesceUInt(
                    _TagYear(id3v2),
                    id3v1?.Year,
                    _TagYear(xiph),
                    _TagYear(ape),
                    _AppleYear(overlay.Apple),
                    _AsfUInt(asf, "WM/Year")),
                Track: _CoalesceUInt(
                    _TagTrack(id3v2),
                    id3v1?.Track is null ? null : id3v1.Track,
                    _TagTrack(xiph),
                    _TagTrack(ape),
                    _AppleTrack(overlay.Apple),
                    _AsfUInt(asf, "WM/TrackNumber")),
                TrackCount: _CoalesceUInt(
                    _TagTrackCount(id3v2),
                    null,
                    _TagTrackCount(xiph),
                    _TagTrackCount(ape),
                    _AppleTrackCount(overlay.Apple),
                    _AsfUInt(asf, "WM/TrackTotal")),
                Disc: _CoalesceUInt(
                    _TagDisc(id3v2),
                    null,
                    _TagDisc(xiph),
                    _TagDisc(ape),
                    _AppleDisc(overlay.Apple),
                    _AsfUInt(asf, "WM/PartOfSet")),
                DiscCount: _CoalesceUInt(
                    _TagDiscCount(id3v2),
                    null,
                    _TagDiscCount(xiph),
                    _TagDiscCount(ape),
                    _AppleDiscCount(overlay.Apple),
                    _AsfUInt(asf, "WM/TotalDiscs")));
        }

        private static readonly string[] _OverlayListSeparators = [";"];

        private static string[]? _SplitJoinedListForOverlay(string? joined)
        {
            if (string.IsNullOrWhiteSpace(joined))
                return null;

            var parts = joined.Split(_OverlayListSeparators, StringSplitOptions.TrimEntries);
            var filtered = parts.Where(static p => !string.IsNullOrWhiteSpace(p)).Select(static p => p.Trim()).ToArray();
            return filtered.Length == 0 ? null : filtered;
        }

        private static TagLib.Id3v2.Tag? _TryParseId3v2(Id3v2TagData? data)
        {
            if (data is null || data.CanonicalTagBytes.IsDefaultOrEmpty)
                return null;

            try
            {
                return new TagLib.Id3v2.Tag(new ByteVector([.. data.CanonicalTagBytes]));
            }
            catch (CorruptFileException)
            {
                return null;
            }
        }

        private static XiphComment? _TryParseXiph(SerializedTagBlob? blob)
        {
            if (blob is null || blob.CanonicalTagBytes.IsDefaultOrEmpty)
                return null;

            try
            {
                return new XiphComment(new ByteVector([.. blob.CanonicalTagBytes]));
            }
            catch (CorruptFileException)
            {
                return null;
            }
        }

        private static TagLib.Ape.Tag? _TryParseApe(SerializedTagBlob? blob)
        {
            if (blob is null || blob.CanonicalTagBytes.IsDefaultOrEmpty)
                return null;

            try
            {
                return new TagLib.Ape.Tag(new ByteVector([.. blob.CanonicalTagBytes]));
            }
            catch (CorruptFileException)
            {
                return null;
            }
        }

        private static TagLib.Asf.Tag? _TryBuildAsfTag(AsfTagData? data)
        {
            if (data is null || data.Descriptors.IsDefaultOrEmpty)
                return null;

            var asf = new TagLib.Asf.Tag();
            foreach (var row in data.Descriptors)
                asf.AddDescriptor(new TagLib.Asf.ContentDescriptor(row.Name, row.Value));

            return asf;
        }

        private static string? _TagTitle(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Title);
        }

        private static string? _TagAlbum(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Album);
        }

        private static string? _TagComment(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Comment);
        }

        private static string? _TagLyrics(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Lyrics);
        }

        private static string? _TagCopyright(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Copyright);
        }

        private static string? _TagGrouping(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Grouping);
        }

        private static string? _TagFirstGenre(Tag? tag)
        {
            if (tag is null)
                return null;

            return _NullIfWhitespace(tag.FirstGenre);
        }

        private static uint? _TagYear(Tag? tag)
        {
            if (tag is null || tag.Year == 0)
                return null;

            return tag.Year;
        }

        private static uint? _TagTrack(Tag? tag)
        {
            if (tag is null || tag.Track == 0)
                return null;

            return tag.Track;
        }

        private static uint? _TagTrackCount(Tag? tag)
        {
            if (tag is null || tag.TrackCount == 0)
                return null;

            return tag.TrackCount;
        }

        private static uint? _TagDisc(Tag? tag)
        {
            if (tag is null || tag.Disc == 0)
                return null;

            return tag.Disc;
        }

        private static uint? _TagDiscCount(Tag? tag)
        {
            if (tag is null || tag.DiscCount == 0)
                return null;

            return tag.DiscCount;
        }

        private static string? _Id3v1String(string? text)
        {
            return _NullIfWhitespace(text);
        }

        private static string[]? _Id3v1SplitPerformer(string? artist)
        {
            var trimmed = _NullIfWhitespace(artist);
            if (trimmed is null)
                return null;

            return [trimmed];
        }

        private static string? _Id3v1Genre(Id3v1TagData? data)
        {
            if (data is null)
                return null;

            var name = Genres.IndexToAudio(data.Genre);
            return _NullIfWhitespace(name);
        }

        private static string? _CoalesceUnicode(params string?[] candidates)
        {
            foreach (var c in candidates)
            {
                if (c is not null)
                    return c;
            }

            return null;
        }

        private static uint? _CoalesceUInt(params uint?[] candidates)
        {
            foreach (var c in candidates)
            {
                if (c is not null)
                    return c;
            }

            return null;
        }

        private static string? _CoalesceJoinedList(params string[]?[] sources)
        {
            foreach (var arr in sources)
            {
                var joined = _JoinPerformerList(arr);
                if (joined is not null)
                    return joined;
            }

            return null;
        }

        private static string? _JoinPerformerList(string[]? values)
        {
            if (values is null || values.Length == 0)
                return null;

            var filtered = values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(static v => v.Trim())
                .ToArray();

            if (filtered.Length == 0)
                return null;

            return string.Join("; ", filtered);
        }

        private static string? _AsfUnicode(TagLib.Asf.Tag? asfTag, string descriptorName)
        {
            if (asfTag is null)
                return null;

            foreach (var d in asfTag)
            {
                if (!string.Equals(d.Name, descriptorName, StringComparison.Ordinal))
                    continue;

                return _NullIfWhitespace(d.ToString());
            }

            return null;
        }

        private static string[]? _AsfJoinedList(TagLib.Asf.Tag? tag, string descriptorName)
        {
            var text = _AsfUnicode(tag, descriptorName);
            if (text is null)
                return null;

            return [text];
        }

        private static string[]? _AsfJoinedPerformers(TagLib.Asf.Tag? tag)
        {
            var author = _AsfUnicode(tag, "WM/Author");
            if (author is not null)
                return [author];

            return _AsfJoinedList(tag, "WM/AlbumArtist");
        }

        private static uint? _AsfUInt(TagLib.Asf.Tag? tag, string descriptorName)
        {
            var text = _AsfUnicode(tag, descriptorName);
            if (text is null)
                return null;

            return uint.TryParse(text.Trim(), out var u) ? u : null;
        }

        private static string? _ApplePlainText(AppleTagData? apple, ReadOnlySpan<byte> atomType)
        {
            var values = _AppleAtomValues(apple, atomType);
            if (values.IsDefaultOrEmpty)
                return null;

            return _NullIfWhitespace(values[0]);
        }

        private static string[]? _AppleJoinedList(AppleTagData? apple, ReadOnlySpan<byte> atomType)
        {
            var values = _AppleAtomValues(apple, atomType);
            if (values.IsDefaultOrEmpty)
                return null;

            var filtered = new List<string>();
            foreach (var v in values)
            {
                var t = _NullIfWhitespace(v);
                if (t is not null)
                    filtered.Add(t);
            }

            return filtered.Count == 0 ? null : [.. filtered];
        }

        private static ImmutableArray<string> _AppleAtomValues(AppleTagData? apple, ReadOnlySpan<byte> atomType)
        {
            if (apple is null || apple.Atoms.IsDefaultOrEmpty || atomType.Length != 4)
                return default;

            foreach (var row in apple.Atoms)
            {
                if (!row.AtomType.AsSpan().SequenceEqual(atomType))
                    continue;

                return row.Values;
            }

            return default;
        }

        private static uint? _AppleYear(AppleTagData? apple)
        {
            var day = _ApplePlainText(apple, AppleAtomConstants.DayAtom);
            return day is not null && uint.TryParse(day.Trim(), out var y) ? y : null;
        }

        /// <remarks>
        /// MP4 track/disc atoms are binary; omit Apple when TagLib-derived numbers are unavailable from text atoms alone.
        /// </remarks>
        private static uint? _AppleTrack(AppleTagData? apple)
        {
            _ = apple;
            return null;
        }

        private static uint? _AppleTrackCount(AppleTagData? apple)
        {
            _ = apple;
            return null;
        }

        private static uint? _AppleDisc(AppleTagData? apple)
        {
            _ = apple;
            return null;
        }

        private static uint? _AppleDiscCount(AppleTagData? apple)
        {
            _ = apple;
            return null;
        }

        private static string? _NullIfWhitespace(string? text)
        {
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        private static class AppleAtomConstants
        {
            public static ReadOnlySpan<byte> TitleAtom => [0xA9, (byte)'n', (byte)'a', (byte)'m'];
            public static ReadOnlySpan<byte> AlbumAtom => [0xA9, (byte)'a', (byte)'l', (byte)'b'];
            public static ReadOnlySpan<byte> ArtistAtom => [0xA9, (byte)'A', (byte)'R', (byte)'T'];
            public static ReadOnlySpan<byte> AlbumArtistAtom => [(byte)'a', (byte)'A', (byte)'R', (byte)'T'];
            public static ReadOnlySpan<byte> ComposerAtom => [0xA9, (byte)'w', (byte)'r', (byte)'t'];
            public static ReadOnlySpan<byte> GenreAtom => [0xA9, (byte)'g', (byte)'e', (byte)'n'];
            public static ReadOnlySpan<byte> CommentAtom => [0xA9, (byte)'c', (byte)'m', (byte)'t'];
            public static ReadOnlySpan<byte> LyricsAtom => [0xA9, (byte)'l', (byte)'y', (byte)'r'];
            public static ReadOnlySpan<byte> CopyrightAtom => [(byte)'c', (byte)'p', (byte)'r', (byte)'t'];
            public static ReadOnlySpan<byte> GroupingAtom => [0xA9, (byte)'g', (byte)'r', (byte)'p'];
            public static ReadOnlySpan<byte> DayAtom => [0xA9, (byte)'d', (byte)'a', (byte)'y'];
        }
    }
}
