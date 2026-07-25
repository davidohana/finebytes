using System.Collections.Immutable;
using System.Globalization;
using Mfr.Models.Tags;
using Mfr.Utils;
using TagLib;

namespace Mfr.Metadata
{
    /// <summary>
    /// Common cross-format audio fields derived from structured <see cref="AudioTagOverlay"/> tag blocks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generic read priority when projecting from an overlay: Id3v2 → Id3v1 → Xiph → Ape → RiffInfo → Apple → Asf.
    /// </para>
    /// <para>
    /// Generic write (via <c>MergeSemanticIntoBlocks</c>) broadcasts each field onto every present block.
    /// When the overlay carries no blocks, the container's recommended empty block is created first
    /// (<see cref="AudioTagContainerPolicy.GetRecommendedBlock"/>); sibling tag types are never invented.
    /// </para>
    /// </remarks>
    /// <param name="Title">Visible title, if any tag block supplies one.</param>
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
    public sealed record CommonAudioTag(
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
        /// Projects merged semantic values from structured tag blocks only.
        /// </summary>
        /// <param name="overlay">Overlay whose blocks are interpreted; must not be <see langword="null"/>.</param>
        /// <returns>Projected common fields.</returns>
        public static CommonAudioTag FromOverlay(AudioTagOverlay overlay)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var title = Nullables.FirstNonNull(
                _Id3v2Singleton(overlay.Id3v2, "TIT2"),
                _ReadId3v1String(overlay.Id3v1?.Title),
                _XiphFirst(overlay.Xiph, "TITLE"),
                _ApeFirst(overlay.Ape, "Title"),
                _Riff(overlay.RiffInfo, "INAM"),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.TitleAtom),
                _Asf(overlay.Asf, "WM/Title"));
            var album = Nullables.FirstNonNull(
                _Id3v2Singleton(overlay.Id3v2, "TALB"),
                _ReadId3v1String(overlay.Id3v1?.Album),
                _XiphFirst(overlay.Xiph, "ALBUM"),
                _ApeFirst(overlay.Ape, "Album"),
                _Riff(overlay.RiffInfo, "IPRD"),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.AlbumAtom),
                _Asf(overlay.Asf, "WM/AlbumTitle"));
            var performers = Nullables.FirstNonNull(
                _Id3v2Joined(overlay.Id3v2, "TPE1"),
                _JoinList(_SplitId3v1Performer(overlay.Id3v1?.Artist)),
                _XiphJoined(overlay.Xiph, "ARTIST"),
                _ApeJoined(overlay.Ape, "Artist"),
                _Riff(overlay.RiffInfo, "IART"),
                _JoinList(_ReadAppleJoinedList(overlay.Apple, AppleAtomConstants.ArtistAtom)),
                _Asf(overlay.Asf, "WM/Author") ?? _Asf(overlay.Asf, "WM/AlbumArtist"));
            var albumArtists = Nullables.FirstNonNull(
                _Id3v2Joined(overlay.Id3v2, "TPE2"),
                _XiphJoined(overlay.Xiph, "ALBUMARTIST"),
                _ApeJoined(overlay.Ape, "Album Artist"),
                _JoinList(_ReadAppleJoinedList(overlay.Apple, AppleAtomConstants.AlbumArtistAtom)),
                _Asf(overlay.Asf, "WM/AlbumArtist"));
            var composers = Nullables.FirstNonNull(
                _Id3v2Joined(overlay.Id3v2, "TCOM"),
                _XiphJoined(overlay.Xiph, "COMPOSER"),
                _ApeJoined(overlay.Ape, "Composer"),
                _JoinList(_ReadAppleJoinedList(overlay.Apple, AppleAtomConstants.ComposerAtom)),
                _Asf(overlay.Asf, "WM/Composer"));
            var genre = Nullables.FirstNonNull(
                _Id3v2Singleton(overlay.Id3v2, "TCON"),
                _ReadId3v1Genre(overlay.Id3v1),
                _XiphFirst(overlay.Xiph, "GENRE"),
                _ApeFirst(overlay.Ape, "Genre"),
                _Riff(overlay.RiffInfo, "IGNR"),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.GenreAtom),
                _Asf(overlay.Asf, "WM/Genre"));
            var comment = Nullables.FirstNonNull(
                _Id3v2PrimaryMulti(overlay.Id3v2, "COMM"),
                _ReadId3v1String(overlay.Id3v1?.Comment),
                _XiphFirst(overlay.Xiph, "DESCRIPTION") ?? _XiphFirst(overlay.Xiph, "COMMENT"),
                _ApeFirst(overlay.Ape, "Comment"),
                _Riff(overlay.RiffInfo, "ICMT"),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.CommentAtom),
                _Asf(overlay.Asf, "WM/Description"));
            var lyrics = Nullables.FirstNonNull(
                _Id3v2PrimaryMulti(overlay.Id3v2, "USLT"),
                _XiphFirst(overlay.Xiph, "LYRICS") ?? _XiphFirst(overlay.Xiph, "UNSYNCEDLYRICS"),
                _ApeFirst(overlay.Ape, "Lyrics"),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.LyricsAtom),
                _Asf(overlay.Asf, "WM/Lyrics"));
            var copyright = Nullables.FirstNonNull(
                _Id3v2Singleton(overlay.Id3v2, "TCOP"),
                _XiphFirst(overlay.Xiph, "COPYRIGHT"),
                _ApeFirst(overlay.Ape, "Copyright"),
                _Riff(overlay.RiffInfo, "ICOP"),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.CopyrightAtom),
                _Asf(overlay.Asf, "WM/ProviderCopyright"));
            var grouping = Nullables.FirstNonNull(
                _Id3v2Singleton(overlay.Id3v2, "TIT1"),
                _XiphFirst(overlay.Xiph, "GROUPING") ?? _XiphFirst(overlay.Xiph, "CONTENTGROUP"),
                _ApeFirst(overlay.Ape, "Grouping"),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.GroupingAtom),
                _Asf(overlay.Asf, "WM/ContentGroupDescription"));
            var year = Nullables.FirstNonNull(
                _Id3v2Year(overlay.Id3v2),
                overlay.Id3v1?.Year,
                _ParseUInt(_XiphFirst(overlay.Xiph, "DATE") ?? _XiphFirst(overlay.Xiph, "YEAR")),
                _ParseUInt(_ApeFirst(overlay.Ape, "Year")),
                _ParseUInt(_Riff(overlay.RiffInfo, "ICRD")),
                _ReadAppleYear(overlay.Apple),
                _ParseUInt(_Asf(overlay.Asf, "WM/Year")));
            var (id3Track, id3TrackCount) = _Id3v2TrackPair(overlay.Id3v2, "TRCK");
            var track = Nullables.FirstNonNull(
                id3Track,
                overlay.Id3v1?.Track is null ? null : overlay.Id3v1.Track,
                _ParseUInt(_XiphFirst(overlay.Xiph, "TRACKNUMBER")),
                _ParseUInt(_ApeFirst(overlay.Ape, "Track")),
                _ParseUInt(_Riff(overlay.RiffInfo, "ITRK")),
                _ParseUInt(_Asf(overlay.Asf, "WM/TrackNumber")));
            var trackCount = Nullables.FirstNonNull(
                id3TrackCount,
                _ParseUInt(_XiphFirst(overlay.Xiph, "TRACKTOTAL") ?? _XiphFirst(overlay.Xiph, "TOTALTRACKS")),
                _ParseUInt(_ApeFirst(overlay.Ape, "TrackCount")),
                _ParseUInt(_Asf(overlay.Asf, "WM/TrackTotal")));
            var (id3Disc, id3DiscCount) = _Id3v2TrackPair(overlay.Id3v2, "TPOS");
            var disc = Nullables.FirstNonNull(
                id3Disc,
                _ParseUInt(_XiphFirst(overlay.Xiph, "DISCNUMBER")),
                _ParseUInt(_ApeFirst(overlay.Ape, "Disc")),
                _ParseUInt(_Asf(overlay.Asf, "WM/PartOfSet")));
            var discCount = Nullables.FirstNonNull(
                id3DiscCount,
                _ParseUInt(_XiphFirst(overlay.Xiph, "DISCTOTAL") ?? _XiphFirst(overlay.Xiph, "TOTALDISCS")),
                _ParseUInt(_ApeFirst(overlay.Ape, "DiscCount")),
                _ParseUInt(_Asf(overlay.Asf, "WM/TotalDiscs")));

            return new CommonAudioTag(
                Title: title,
                Album: album,
                Performers: performers,
                AlbumArtists: albumArtists,
                Composers: composers,
                Genre: genre,
                Comment: comment,
                Lyrics: lyrics,
                Copyright: copyright,
                Grouping: grouping,
                Year: year,
                Track: track,
                TrackCount: trackCount,
                Disc: disc,
                DiscCount: discCount);
        }

        /// <summary>
        /// Projects common fields from a live TagLib tag (combined or single-type).
        /// </summary>
        /// <param name="tag">TagLib tag whose string/list/numeric fields are read.</param>
        /// <returns>Common fields reconstructed from the tag's strings/lists and numerics.</returns>
        public static CommonAudioTag FromCombinedTag(Tag tag)
        {
            ArgumentNullException.ThrowIfNull(tag);

            return new CommonAudioTag(
                Title: _NullIfWhitespace(tag.Title),
                Album: _NullIfWhitespace(tag.Album),
                Performers: _JoinList(tag.Performers),
                AlbumArtists: _JoinList(tag.AlbumArtists),
                Composers: _JoinList(tag.Composers),
                Genre: tag.Genres.Length == 0 ? null : _NullIfWhitespace(tag.Genres[0]),
                Comment: _NullIfWhitespace(tag.Comment),
                Lyrics: _NullIfWhitespace(tag.Lyrics),
                Copyright: _NullIfWhitespace(tag.Copyright),
                Grouping: _NullIfWhitespace(tag.Grouping),
                Year: tag.Year == 0 ? null : tag.Year,
                Track: tag.Track == 0 ? null : tag.Track,
                TrackCount: tag.TrackCount == 0 ? null : tag.TrackCount,
                Disc: tag.Disc == 0 ? null : tag.Disc,
                DiscCount: tag.DiscCount == 0 ? null : tag.DiscCount);
        }

        /// <summary>
        /// Returns whether any semantic scalar or list projection is populated.
        /// </summary>
        /// <returns><see langword="true"/> when at least one field is non-absent.</returns>
        public bool ContainsRenderableSemantics()
        {
            return Title is not null
                || Album is not null
                || Performers is not null
                || AlbumArtists is not null
                || Composers is not null
                || Genre is not null
                || Comment is not null
                || Lyrics is not null
                || Copyright is not null
                || Grouping is not null
                || Year is not null
                || Track is not null
                || TrackCount is not null
                || Disc is not null
                || DiscCount is not null;
        }

        private static string? _Id3v2Singleton(Id3v2TagData? data, string frameId)
        {
            if (data is null)
                return null;

            foreach (var frame in data.Frames)
            {
                if (!string.Equals(frame.FrameId, frameId, StringComparison.Ordinal))
                    continue;

                return frame.TextValues.Length == 0 ? null : _NullIfWhitespace(frame.TextValues[0]);
            }

            return null;
        }

        private static string? _Id3v2Joined(Id3v2TagData? data, string frameId)
        {
            if (data is null)
                return null;

            foreach (var frame in data.Frames)
            {
                if (!string.Equals(frame.FrameId, frameId, StringComparison.Ordinal))
                    continue;

                return _JoinList([.. frame.TextValues]);
            }

            return null;
        }

        private static string? _Id3v2PrimaryMulti(Id3v2TagData? data, string frameId)
        {
            if (data is null)
                return null;

            Id3v2ModeledFrame? primary = null;
            foreach (var frame in data.Frames)
            {
                if (!string.Equals(frame.FrameId, frameId, StringComparison.Ordinal))
                    continue;

                if (!string.IsNullOrEmpty(frame.Description))
                    continue;

                primary = frame;
                break;
            }

            if (primary is null)
            {
                foreach (var frame in data.Frames)
                {
                    if (!string.Equals(frame.FrameId, frameId, StringComparison.Ordinal))
                        continue;

                    primary = frame;
                    break;
                }
            }

            if (primary is null || primary.TextValues.Length == 0)
                return null;

            return _NullIfWhitespace(primary.TextValues[0]);
        }

        private static uint? _Id3v2Year(Id3v2TagData? data)
        {
            var text = _Id3v2Singleton(data, "TDRC") ?? _Id3v2Singleton(data, "TYER");
            if (text is null)
                return null;

            // TDRC may be a full timestamp; take leading year digits.
            var yearPart = text.Length >= 4 ? text[..4] : text;
            return uint.TryParse(yearPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out var year) && year != 0
                ? year
                : null;
        }

        private static (uint? Number, uint? Count) _Id3v2TrackPair(Id3v2TagData? data, string frameId)
        {
            var text = _Id3v2Singleton(data, frameId);
            if (text is null)
                return (null, null);

            var slash = text.IndexOf('/');
            if (slash < 0)
            {
                return uint.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n != 0
                    ? (n, null)
                    : (null, null);
            }

            uint? number = null;
            uint? count = null;
            if (slash > 0
                && uint.TryParse(text[..slash], NumberStyles.Integer, CultureInfo.InvariantCulture, out var nParsed)
                && nParsed != 0)
                number = nParsed;

            if (slash + 1 < text.Length
                && uint.TryParse(text[(slash + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var cParsed)
                && cParsed != 0)
                count = cParsed;

            return (number, count);
        }

        private static string? _XiphFirst(XiphTagData? data, string key)
        {
            return _MultimapFirst(data?.Fields ?? default, key);
        }

        private static string? _XiphJoined(XiphTagData? data, string key)
        {
            return _MultimapJoined(data?.Fields ?? default, key);
        }

        private static string? _ApeFirst(ApeTagData? data, string key)
        {
            return _MultimapFirst(data?.Fields ?? default, key);
        }

        private static string? _ApeJoined(ApeTagData? data, string key)
        {
            return _MultimapJoined(data?.Fields ?? default, key);
        }

        private static string? _MultimapFirst(ImmutableArray<TextFieldRow> fields, string key)
        {
            if (fields.IsDefaultOrEmpty)
                return null;

            foreach (var row in fields)
            {
                if (!string.Equals(row.Key, key, StringComparison.Ordinal))
                    continue;

                return row.Values.Length == 0 ? null : _NullIfWhitespace(row.Values[0]);
            }

            return null;
        }

        private static string? _MultimapJoined(ImmutableArray<TextFieldRow> fields, string key)
        {
            if (fields.IsDefaultOrEmpty)
                return null;

            foreach (var row in fields)
            {
                if (!string.Equals(row.Key, key, StringComparison.Ordinal))
                    continue;

                return _JoinList([.. row.Values]);
            }

            return null;
        }

        private static string? _Riff(RiffInfoTagData? data, string key)
        {
            if (data is null)
                return null;

            foreach (var row in data.Fields)
            {
                if (string.Equals(row.Key, key, StringComparison.Ordinal))
                    return _NullIfWhitespace(row.Value);
            }

            return null;
        }

        private static string? _Asf(AsfTagData? data, string name)
        {
            if (data is null)
                return null;

            foreach (var row in data.Descriptors)
            {
                if (string.Equals(row.Name, name, StringComparison.Ordinal))
                    return _NullIfWhitespace(row.Value);
            }

            return null;
        }

        private static uint? _ParseUInt(string? text)
        {
            if (text is null)
                return null;

            return uint.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var u) && u != 0
                ? u
                : null;
        }

        private static string? _ReadId3v1String(string? text)
        {
            return _NullIfWhitespace(text);
        }

        private static string[]? _SplitId3v1Performer(string? artist)
        {
            var trimmed = _NullIfWhitespace(artist);
            return trimmed is null ? null : [trimmed];
        }

        private static string? _ReadId3v1Genre(Id3v1TagData? data)
        {
            if (data is null)
                return null;

            return _NullIfWhitespace(Genres.IndexToAudio(data.Genre));
        }

        private static string? _JoinList(string[]? values)
        {
            if (values is null || values.Length == 0)
                return null;

            var filtered = values
                .Where(static v => !string.IsNullOrWhiteSpace(v))
                .Select(static v => v.Trim())
                .ToArray();

            return filtered.Length == 0 ? null : string.Join("; ", filtered);
        }

        private static string? _ReadApplePlainText(AppleTagData? apple, ReadOnlySpan<byte> atomType)
        {
            var values = _ReadAppleAtomValues(apple, atomType);
            return values.IsDefaultOrEmpty ? null : _NullIfWhitespace(values[0]);
        }

        private static string[]? _ReadAppleJoinedList(AppleTagData? apple, ReadOnlySpan<byte> atomType)
        {
            var values = _ReadAppleAtomValues(apple, atomType);
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

        private static ImmutableArray<string> _ReadAppleAtomValues(AppleTagData? apple, ReadOnlySpan<byte> atomType)
        {
            if (apple is null || apple.Atoms.IsDefaultOrEmpty || atomType.Length != 4)
                return default;

            foreach (var row in apple.Atoms)
            {
                if (row.AtomType.AsSpan().SequenceEqual(atomType))
                    return row.Values;
            }

            return default;
        }

        private static uint? _ReadAppleYear(AppleTagData? apple)
        {
            var day = _ReadApplePlainText(apple, AppleAtomConstants.DayAtom);
            return day is not null && uint.TryParse(day.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) && y != 0
                ? y
                : null;
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
