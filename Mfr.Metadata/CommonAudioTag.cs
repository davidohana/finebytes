using System.Collections.Immutable;
using Mfr.Models.Tags;
using Mfr.Utils;
using TagLib;
using TagLib.Ogg;
using TagLib.Riff;

namespace Mfr.Metadata
{
    /// <summary>
    /// Common cross-format audio fields derived from structured <see cref="AudioTagOverlay"/> native blocks.
    /// </summary>
    /// <remarks>
    /// Precedence mirrors TagLib merged-tag behavior: ID3v2 over ID3v1, then Xiph, APE, RIFF INFO (WAV LIST), Apple text atoms, ASF descriptors.
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

            var id3v2 = _TryParseId3v2(overlay.Id3v2);
            var id3v1 = overlay.Id3v1;
            var xiph = _TryParseXiph(overlay.Xiph);
            var ape = _TryParseApe(overlay.Ape);
            var riff = _TryParseRiffInfo(overlay.RiffInfo);
            var asf = _TryBuildAsfTag(overlay.Asf);

            var title = Nullables.FirstNonNull(
                _ReadTagTitle(id3v2),
                _ReadId3v1String(id3v1?.Title),
                _ReadTagTitle(xiph),
                _ReadTagTitle(ape),
                _ReadTagTitle(riff),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.TitleAtom),
                _ReadAsfString(asf, "WM/Title"),
                _ReadTagTitle(asf));
            var album = Nullables.FirstNonNull(
                _ReadTagAlbum(id3v2),
                _ReadId3v1String(id3v1?.Album),
                _ReadTagAlbum(xiph),
                _ReadTagAlbum(ape),
                _ReadTagAlbum(riff),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.AlbumAtom),
                _ReadAsfString(asf, "WM/AlbumTitle"));
            var performers = Nullables.FirstNonNull(
                _JoinList(id3v2?.Performers),
                _JoinList(_SplitId3v1Performer(id3v1?.Artist)),
                _JoinList(xiph?.Performers),
                _JoinList(ape?.Performers),
                _JoinList(riff?.Performers),
                _JoinList(_ReadAppleJoinedList(overlay.Apple, AppleAtomConstants.ArtistAtom)),
                _JoinList(_ReadAsfJoinedPerformers(asf)));
            var albumArtists = Nullables.FirstNonNull(
                _JoinList(id3v2?.AlbumArtists),
                _JoinList(xiph?.AlbumArtists),
                _JoinList(ape?.AlbumArtists),
                _JoinList(riff?.AlbumArtists),
                _JoinList(_ReadAppleJoinedList(overlay.Apple, AppleAtomConstants.AlbumArtistAtom)),
                _JoinList(_ReadAsfJoinedList(asf, "WM/AlbumArtist")));
            var composers = Nullables.FirstNonNull(
                _JoinList(id3v2?.Composers),
                _JoinList(xiph?.Composers),
                _JoinList(ape?.Composers),
                _JoinList(riff?.Composers),
                _JoinList(_ReadAppleJoinedList(overlay.Apple, AppleAtomConstants.ComposerAtom)),
                _JoinList(_ReadAsfJoinedList(asf, "WM/Composer")));
            var genre = Nullables.FirstNonNull(
                _ReadTagFirstGenre(id3v2),
                _ReadId3v1Genre(id3v1),
                _ReadTagFirstGenre(xiph),
                _ReadTagFirstGenre(ape),
                _ReadTagFirstGenre(riff),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.GenreAtom),
                _ReadAsfString(asf, "WM/Genre"));
            var comment = Nullables.FirstNonNull(
                _ReadTagComment(id3v2),
                _ReadId3v1String(id3v1?.Comment),
                _ReadTagComment(xiph),
                _ReadTagComment(ape),
                _ReadTagComment(riff),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.CommentAtom),
                _ReadAsfString(asf, "WM/Description"));
            var lyrics = Nullables.FirstNonNull(
                _ReadTagLyrics(id3v2),
                _ReadTagLyrics(xiph),
                _ReadTagLyrics(ape),
                _ReadTagLyrics(riff),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.LyricsAtom),
                _ReadAsfString(asf, "WM/Lyrics"));
            var copyright = Nullables.FirstNonNull(
                _ReadTagCopyright(id3v2),
                _ReadTagCopyright(xiph),
                _ReadTagCopyright(ape),
                _ReadTagCopyright(riff),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.CopyrightAtom),
                _ReadAsfString(asf, "WM/ProviderCopyright"));
            var grouping = Nullables.FirstNonNull(
                _ReadTagGrouping(id3v2),
                _ReadTagGrouping(xiph),
                _ReadTagGrouping(ape),
                _ReadTagGrouping(riff),
                _ReadApplePlainText(overlay.Apple, AppleAtomConstants.GroupingAtom),
                _ReadAsfString(asf, "WM/ContentGroupDescription"));
            var year = Nullables.FirstNonNull(
                _ReadTagYear(id3v2),
                id3v1?.Year,
                _ReadTagYear(xiph),
                _ReadTagYear(ape),
                _ReadTagYear(riff),
                _ReadAppleYear(overlay.Apple),
                _ReadAsfUInt(asf, "WM/Year"));
            var track = Nullables.FirstNonNull(
                _ReadTagTrack(id3v2),
                id3v1?.Track is null ? null : id3v1.Track,
                _ReadTagTrack(xiph),
                _ReadTagTrack(ape),
                _ReadTagTrack(riff),
                _ReadAppleTrack(overlay.Apple),
                _ReadAsfUInt(asf, "WM/TrackNumber"));
            var trackCount = Nullables.FirstNonNull(
                _ReadTagTrackCount(id3v2),
                _ReadTagTrackCount(xiph),
                _ReadTagTrackCount(ape),
                _ReadTagTrackCount(riff),
                _ReadAppleTrackCount(overlay.Apple),
                _ReadAsfUInt(asf, "WM/TrackTotal"));
            var disc = Nullables.FirstNonNull(
                _ReadTagDisc(id3v2),
                _ReadTagDisc(xiph),
                _ReadTagDisc(ape),
                _ReadTagDisc(riff),
                _ReadAppleDisc(overlay.Apple),
                _ReadAsfUInt(asf, "WM/PartOfSet"));
            var discCount = Nullables.FirstNonNull(
                _ReadTagDiscCount(id3v2),
                _ReadTagDiscCount(xiph),
                _ReadTagDiscCount(ape),
                _ReadTagDiscCount(riff),
                _ReadAppleDiscCount(overlay.Apple),
                _ReadAsfUInt(asf, "WM/TotalDiscs"));

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
        /// Materializes semantics from TagLib's merged façade tag fields (covers RIFF/WAV LIST payloads not modeled as native blocks alone).
        /// </summary>
        /// <param name="tag">Active combined TagLib façade.</param>
        /// <returns>Common fields reconstructed from façade strings/lists and numerics.</returns>
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

        /// <summary>
        /// Copies each field from <paramref name="ambient"/> only where this instance has no substantive value per field (whitespace is treated like absent strings).
        /// </summary>
        /// <param name="ambient">Typically <see cref="FromCombinedTag"/> values not yet reflected in native blocks.</param>
        /// <returns>Combined common tag; equal to <see langword="this"/> when nothing was missing.</returns>
        public CommonAudioTag WithMissingFieldsFilledFrom(CommonAudioTag ambient)
        {
            return new CommonAudioTag(
                Title: _CoalesceAbsentOrWhitespaceString(Title, ambient.Title),
                Album: _CoalesceAbsentOrWhitespaceString(Album, ambient.Album),
                Performers: _CoalesceAbsentOrWhitespaceString(Performers, ambient.Performers),
                AlbumArtists: _CoalesceAbsentOrWhitespaceString(AlbumArtists, ambient.AlbumArtists),
                Composers: _CoalesceAbsentOrWhitespaceString(Composers, ambient.Composers),
                Genre: _CoalesceAbsentOrWhitespaceString(Genre, ambient.Genre),
                Comment: _CoalesceAbsentOrWhitespaceString(Comment, ambient.Comment),
                Lyrics: _CoalesceAbsentOrWhitespaceString(Lyrics, ambient.Lyrics),
                Copyright: _CoalesceAbsentOrWhitespaceString(Copyright, ambient.Copyright),
                Grouping: _CoalesceAbsentOrWhitespaceString(Grouping, ambient.Grouping),
                Year: Year ?? ambient.Year,
                Track: Track ?? ambient.Track,
                TrackCount: TrackCount ?? ambient.TrackCount,
                Disc: Disc ?? ambient.Disc,
                DiscCount: DiscCount ?? ambient.DiscCount);
        }

        /// <summary>Returns <paramref name="projected"/> unless it is absent or whitespace-only, otherwise uses <paramref name="ambient"/> (trimmed).</summary>
        private static string? _CoalesceAbsentOrWhitespaceString(string? projected, string? ambient)
        {
            if (!string.IsNullOrWhiteSpace(projected))
                return projected;

            return string.IsNullOrWhiteSpace(ambient) ? null : ambient.Trim();
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
            catch (ArgumentOutOfRangeException)
            {
                // TagLib can throw when comment packets are truncated or opaque (test doubles, partial reads).
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
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static InfoTag? _TryParseRiffInfo(SerializedTagBlob? blob)
        {
            if (blob is null || blob.CanonicalTagBytes.IsDefaultOrEmpty)
                return null;

            try
            {
                return new InfoTag(new ByteVector([.. blob.CanonicalTagBytes]));
            }
            catch (CorruptFileException)
            {
                return null;
            }
            catch (ArgumentOutOfRangeException)
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

        private static string? _ReadTagTitle(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Title);
        }

        private static string? _ReadTagAlbum(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Album);
        }

        private static string? _ReadTagComment(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Comment);
        }

        private static string? _ReadTagLyrics(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Lyrics);
        }

        private static string? _ReadTagCopyright(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Copyright);
        }

        private static string? _ReadTagGrouping(Tag? tag)
        {
            return tag is null ? null : _NullIfWhitespace(tag.Grouping);
        }

        private static string? _ReadTagFirstGenre(Tag? tag)
        {
            if (tag is null)
                return null;

            return _NullIfWhitespace(tag.FirstGenre);
        }

        private static uint? _ReadTagYear(Tag? tag)
        {
            if (tag is null || tag.Year == 0)
                return null;

            return tag.Year;
        }

        private static uint? _ReadTagTrack(Tag? tag)
        {
            if (tag is null || tag.Track == 0)
                return null;

            return tag.Track;
        }

        private static uint? _ReadTagTrackCount(Tag? tag)
        {
            if (tag is null || tag.TrackCount == 0)
                return null;

            return tag.TrackCount;
        }

        private static uint? _ReadTagDisc(Tag? tag)
        {
            if (tag is null || tag.Disc == 0)
                return null;

            return tag.Disc;
        }

        private static uint? _ReadTagDiscCount(Tag? tag)
        {
            if (tag is null || tag.DiscCount == 0)
                return null;

            return tag.DiscCount;
        }

        private static string? _ReadId3v1String(string? text)
        {
            return _NullIfWhitespace(text);
        }

        private static string[]? _SplitId3v1Performer(string? artist)
        {
            var trimmed = _NullIfWhitespace(artist);
            if (trimmed is null)
                return null;

            return [trimmed];
        }

        private static string? _ReadId3v1Genre(Id3v1TagData? data)
        {
            if (data is null)
                return null;

            var name = Genres.IndexToAudio(data.Genre);
            return _NullIfWhitespace(name);
        }

        private static string? _JoinList(string[]? values)
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

        private static string? _ReadAsfString(TagLib.Asf.Tag? asfTag, string descriptorName)
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

        private static string[]? _ReadAsfJoinedList(TagLib.Asf.Tag? tag, string descriptorName)
        {
            var text = _ReadAsfString(tag, descriptorName);
            if (text is null)
                return null;

            return [text];
        }

        private static string[]? _ReadAsfJoinedPerformers(TagLib.Asf.Tag? tag)
        {
            var author = _ReadAsfString(tag, "WM/Author");
            if (author is not null)
                return [author];

            return _ReadAsfJoinedList(tag, "WM/AlbumArtist");
        }

        private static uint? _ReadAsfUInt(TagLib.Asf.Tag? tag, string descriptorName)
        {
            var text = _ReadAsfString(tag, descriptorName);
            if (text is null)
                return null;

            return uint.TryParse(text.Trim(), out var u) ? u : null;
        }

        private static string? _ReadApplePlainText(AppleTagData? apple, ReadOnlySpan<byte> atomType)
        {
            var values = _ReadAppleAtomValues(apple, atomType);
            if (values.IsDefaultOrEmpty)
                return null;

            return _NullIfWhitespace(values[0]);
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
                if (!row.AtomType.AsSpan().SequenceEqual(atomType))
                    continue;

                return row.Values;
            }

            return default;
        }

        private static uint? _ReadAppleYear(AppleTagData? apple)
        {
            var day = _ReadApplePlainText(apple, AppleAtomConstants.DayAtom);
            return day is not null && uint.TryParse(day.Trim(), out var y) ? y : null;
        }

        /// <remarks>
        /// MP4 track/disc atoms are binary; omit Apple when TagLib-derived numbers are unavailable from text atoms alone.
        /// </remarks>
        private static uint? _ReadAppleTrack(AppleTagData? apple)
        {
            _ = apple;
            return null;
        }

        private static uint? _ReadAppleTrackCount(AppleTagData? apple)
        {
            _ = apple;
            return null;
        }

        private static uint? _ReadAppleDisc(AppleTagData? apple)
        {
            _ = apple;
            return null;
        }

        private static uint? _ReadAppleDiscCount(AppleTagData? apple)
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
