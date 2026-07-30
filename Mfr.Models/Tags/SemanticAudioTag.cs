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
    /// Common cross-format audio fields derived from structured <see cref="AudioTagOverlay"/> tag blocks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generic read priority when projecting from an overlay: Id3v2 → Id3v1 → Xiph → Ape → RiffInfo → Apple → Asf.
    /// </para>
    /// <para>
    /// Generic write (via <see cref="AudioTagOverlay.MergeSemantic"/>) broadcasts each field onto every present block.
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
    /// <param name="BeatsPerMinute">Tempo in beats per minute when non-zero in source tags.</param>
    /// <param name="Conductor">Conductor or director.</param>
    /// <param name="MusicBrainzArtistId">MusicBrainz artist ID.</param>
    /// <param name="MusicBrainzReleaseId">MusicBrainz release (album) ID.</param>
    /// <param name="MusicBrainzReleaseArtistId">MusicBrainz release (album) artist ID.</param>
    /// <param name="MusicBrainzTrackId">MusicBrainz track ID.</param>
    /// <param name="MusicBrainzDiscId">MusicBrainz disc ID.</param>
    /// <param name="MusicBrainzReleaseStatus">MusicBrainz release status.</param>
    /// <param name="MusicBrainzReleaseType">MusicBrainz release type.</param>
    /// <param name="MusicBrainzReleaseCountry">MusicBrainz release country.</param>
    /// <param name="MusicIpId">MusicIP PUID.</param>
    /// <param name="AmazonId">Amazon ASIN.</param>
    public sealed record SemanticAudioTag(
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
        uint? DiscCount,
        uint? BeatsPerMinute,
        string? Conductor,
        string? MusicBrainzArtistId,
        string? MusicBrainzReleaseId,
        string? MusicBrainzReleaseArtistId,
        string? MusicBrainzTrackId,
        string? MusicBrainzDiscId,
        string? MusicBrainzReleaseStatus,
        string? MusicBrainzReleaseType,
        string? MusicBrainzReleaseCountry,
        string? MusicIpId,
        string? AmazonId)
    {
        /// <summary>
        /// Projects merged semantic values from structured tag blocks only.
        /// </summary>
        /// <param name="overlay">Overlay whose blocks are interpreted; must not be <see langword="null"/>.</param>
        /// <returns>Projected common fields.</returns>
        public static SemanticAudioTag FromOverlay(AudioTagOverlay overlay)
        {
            ArgumentNullException.ThrowIfNull(overlay);

            var title = Nullables.FirstNonNull(
                _Id3v2Singleton(overlay.Id3v2, "TIT2"),
                overlay.Id3v1?.Title.TrimmedOrNull(),
                _XiphFirst(overlay.Xiph, "TITLE"),
                _ApeFirst(overlay.Ape, "Title"),
                _Riff(overlay.RiffInfo, "INAM"),
                _ReadApplePlainText(overlay.Apple, AppleAtomIds.Title),
                _Asf(overlay.Asf, AsfDescriptorNames.Title));
            var album = Nullables.FirstNonNull(
                _Id3v2Singleton(overlay.Id3v2, "TALB"),
                overlay.Id3v1?.Album.TrimmedOrNull(),
                _XiphFirst(overlay.Xiph, "ALBUM"),
                _ApeFirst(overlay.Ape, "Album"),
                _Riff(overlay.RiffInfo, "IPRD"),
                _ReadApplePlainText(overlay.Apple, AppleAtomIds.Album),
                _Asf(overlay.Asf, AsfDescriptorNames.Album));
            var performers = Nullables.FirstNonNull(
                _Id3v2Joined(overlay.Id3v2, "TPE1"),
                overlay.Id3v1?.Artist.TrimmedOrNull(),
                _XiphJoined(overlay.Xiph, "ARTIST"),
                _ApeJoined(overlay.Ape, "Artist"),
                _Riff(overlay.RiffInfo, "IART"),
                DelimitedText.JoinOrNull(_ReadAppleAtomValues(overlay.Apple, AppleAtomIds.Artist)),
                _Asf(overlay.Asf, AsfDescriptorNames.Author));
            var albumArtists = Nullables.FirstNonNull(
                _Id3v2Joined(overlay.Id3v2, "TPE2"),
                _XiphJoined(overlay.Xiph, "ALBUMARTIST"),
                _ApeJoined(overlay.Ape, "Album Artist"),
                DelimitedText.JoinOrNull(_ReadAppleAtomValues(overlay.Apple, AppleAtomIds.AlbumArtist)),
                _Asf(overlay.Asf, AsfDescriptorNames.AlbumArtist));
            var composers = Nullables.FirstNonNull(
                _Id3v2Joined(overlay.Id3v2, "TCOM"),
                _XiphJoined(overlay.Xiph, "COMPOSER"),
                _ApeJoined(overlay.Ape, "Composer"),
                DelimitedText.JoinOrNull(_ReadAppleAtomValues(overlay.Apple, AppleAtomIds.Composer)),
                _Asf(overlay.Asf, AsfDescriptorNames.Composer));
            var genre = Nullables.FirstNonNull(
                _Id3v2Singleton(overlay.Id3v2, "TCON"),
                _ReadId3v1Genre(overlay.Id3v1),
                _XiphFirst(overlay.Xiph, "GENRE"),
                _ApeFirst(overlay.Ape, "Genre"),
                _Riff(overlay.RiffInfo, "IGNR"),
                _ReadApplePlainText(overlay.Apple, AppleAtomIds.Genre),
                _Asf(overlay.Asf, AsfDescriptorNames.Genre));
            var comment = Nullables.FirstNonNull(
                _Id3v2PrimaryMulti(overlay.Id3v2, "COMM"),
                overlay.Id3v1?.Comment.TrimmedOrNull(),
                _XiphFirst(overlay.Xiph, "DESCRIPTION") ?? _XiphFirst(overlay.Xiph, "COMMENT"),
                _ApeFirst(overlay.Ape, "Comment"),
                _Riff(overlay.RiffInfo, "ICMT"),
                _ReadApplePlainText(overlay.Apple, AppleAtomIds.Comment),
                _Asf(overlay.Asf, AsfDescriptorNames.Comment));
            var lyrics = Nullables.FirstNonNull(
                _Id3v2PrimaryMulti(overlay.Id3v2, "USLT"),
                _XiphFirst(overlay.Xiph, "LYRICS") ?? _XiphFirst(overlay.Xiph, "UNSYNCEDLYRICS"),
                _ApeFirst(overlay.Ape, "Lyrics"),
                _ReadApplePlainText(overlay.Apple, AppleAtomIds.Lyrics),
                _Asf(overlay.Asf, AsfDescriptorNames.Lyrics));
            var copyright = Nullables.FirstNonNull(
                _Id3v2Singleton(overlay.Id3v2, "TCOP"),
                _XiphFirst(overlay.Xiph, "COPYRIGHT"),
                _ApeFirst(overlay.Ape, "Copyright"),
                _Riff(overlay.RiffInfo, "ICOP"),
                _ReadApplePlainText(overlay.Apple, AppleAtomIds.Copyright),
                _Asf(overlay.Asf, AsfDescriptorNames.Copyright));
            var grouping = Nullables.FirstNonNull(
                _Id3v2Singleton(overlay.Id3v2, "TIT1"),
                _XiphFirst(overlay.Xiph, "GROUPING") ?? _XiphFirst(overlay.Xiph, "CONTENTGROUP"),
                _ApeFirst(overlay.Ape, "Grouping"),
                _ReadApplePlainText(overlay.Apple, AppleAtomIds.Grouping),
                _Asf(overlay.Asf, AsfDescriptorNames.Grouping));
            var year = Nullables.FirstNonNull(
                _Id3v2Year(overlay.Id3v2),
                overlay.Id3v1?.Year,
                _ParseUInt(_XiphFirst(overlay.Xiph, "DATE") ?? _XiphFirst(overlay.Xiph, "YEAR")),
                _ParseUInt(_ApeFirst(overlay.Ape, "Year")),
                _ParseUInt(_Riff(overlay.RiffInfo, "ICRD")),
                _ReadAppleYear(overlay.Apple),
                _ParseUInt(_Asf(overlay.Asf, AsfDescriptorNames.Year)));
            var (id3Track, id3TrackCount) = _Id3v2TrackPair(overlay.Id3v2, "TRCK");
            var track = Nullables.FirstNonNull(
                id3Track,
                overlay.Id3v1?.Track is null ? null : overlay.Id3v1.Track,
                _ParseUInt(_XiphFirst(overlay.Xiph, "TRACKNUMBER")),
                _ParseUInt(_ApeFirst(overlay.Ape, "Track")),
                _ParseUInt(_Riff(overlay.RiffInfo, "ITRK")),
                _ParseUInt(_Asf(overlay.Asf, AsfDescriptorNames.TrackNumber)));
            var trackCount = Nullables.FirstNonNull(
                id3TrackCount,
                _ParseUInt(_XiphFirst(overlay.Xiph, "TRACKTOTAL") ?? _XiphFirst(overlay.Xiph, "TOTALTRACKS")),
                _ParseUInt(_ApeFirst(overlay.Ape, "TrackCount")),
                _ParseUInt(_Asf(overlay.Asf, AsfDescriptorNames.TrackTotal)));
            var (id3Disc, id3DiscCount) = _Id3v2TrackPair(overlay.Id3v2, "TPOS");
            var (asfDisc, asfDiscCount) = _ParseAsfPartOfSet(_Asf(overlay.Asf, AsfDescriptorNames.PartOfSet));
            var disc = Nullables.FirstNonNull(
                id3Disc,
                _ParseUInt(_XiphFirst(overlay.Xiph, "DISCNUMBER")),
                _ParseUInt(_ApeFirst(overlay.Ape, "Disc")),
                asfDisc);
            var discCount = Nullables.FirstNonNull(
                id3DiscCount,
                _ParseUInt(_XiphFirst(overlay.Xiph, "DISCTOTAL") ?? _XiphFirst(overlay.Xiph, "TOTALDISCS")),
                _ParseUInt(_ApeFirst(overlay.Ape, "DiscCount")),
                asfDiscCount);
            var beatsPerMinute = Nullables.FirstNonNull(
                _ParseUInt(_Id3v2Singleton(overlay.Id3v2, "TBPM")),
                _ParseUInt(_XiphFirst(overlay.Xiph, "BPM") ?? _XiphFirst(overlay.Xiph, "TEMPO")),
                _ParseUInt(_ApeFirst(overlay.Ape, "BPM")),
                _ParseUInt(_Asf(overlay.Asf, AsfDescriptorNames.BeatsPerMinute)));
            var conductor = Nullables.FirstNonNull(
                _Id3v2Singleton(overlay.Id3v2, "TPE3"),
                _XiphFirst(overlay.Xiph, "CONDUCTOR"),
                _ApeFirst(overlay.Ape, "Conductor"),
                _ReadApplePlainText(overlay.Apple, AppleAtomIds.Conductor),
                _Asf(overlay.Asf, AsfDescriptorNames.Conductor));
            var musicBrainzArtistId = _ReadCatalogField(overlay, SemanticAudioField.MusicBrainzArtistId);
            var musicBrainzReleaseId = _ReadCatalogField(overlay, SemanticAudioField.MusicBrainzReleaseId);
            var musicBrainzReleaseArtistId = _ReadCatalogField(overlay, SemanticAudioField.MusicBrainzReleaseArtistId);
            var musicBrainzTrackId = _ReadCatalogField(overlay, SemanticAudioField.MusicBrainzTrackId);
            var musicBrainzDiscId = _ReadCatalogField(overlay, SemanticAudioField.MusicBrainzDiscId);
            var musicBrainzReleaseStatus = _ReadCatalogField(overlay, SemanticAudioField.MusicBrainzReleaseStatus);
            var musicBrainzReleaseType = _ReadCatalogField(overlay, SemanticAudioField.MusicBrainzReleaseType);
            var musicBrainzReleaseCountry = _ReadCatalogField(overlay, SemanticAudioField.MusicBrainzReleaseCountry);
            var musicIpId = _ReadCatalogField(overlay, SemanticAudioField.MusicIpId);
            var amazonId = _ReadCatalogField(overlay, SemanticAudioField.AmazonId);

            return new SemanticAudioTag(
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
                DiscCount: discCount,
                BeatsPerMinute: beatsPerMinute,
                Conductor: conductor,
                MusicBrainzArtistId: musicBrainzArtistId,
                MusicBrainzReleaseId: musicBrainzReleaseId,
                MusicBrainzReleaseArtistId: musicBrainzReleaseArtistId,
                MusicBrainzTrackId: musicBrainzTrackId,
                MusicBrainzDiscId: musicBrainzDiscId,
                MusicBrainzReleaseStatus: musicBrainzReleaseStatus,
                MusicBrainzReleaseType: musicBrainzReleaseType,
                MusicBrainzReleaseCountry: musicBrainzReleaseCountry,
                MusicIpId: musicIpId,
                AmazonId: amazonId);
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
                || DiscCount is not null
                || BeatsPerMinute is not null
                || Conductor is not null
                || MusicBrainzArtistId is not null
                || MusicBrainzReleaseId is not null
                || MusicBrainzReleaseArtistId is not null
                || MusicBrainzTrackId is not null
                || MusicBrainzDiscId is not null
                || MusicBrainzReleaseStatus is not null
                || MusicBrainzReleaseType is not null
                || MusicBrainzReleaseCountry is not null
                || MusicIpId is not null
                || AmazonId is not null;
        }

        private static string? _ReadCatalogField(AudioTagOverlay overlay, SemanticAudioField field)
        {
            var row = _CatalogRow(field);
            return Nullables.FirstNonNull(
                _Id3v2Txxx(overlay.Id3v2, row.Id3v2TxxxDescription),
                _XiphFirst(overlay.Xiph, row.XiphKey),
                _ApeFirst(overlay.Ape, row.ApeKey),
                _Asf(overlay.Asf, row.AsfDescriptor));
        }

        private static AudioCatalogFieldMaps.CatalogKeyRow _CatalogRow(SemanticAudioField field)
        {
            foreach (var row in AudioCatalogFieldMaps.All)
            {
                if (row.Field == field)
                    return row;
            }

            throw new ArgumentOutOfRangeException(nameof(field), field, "Not a catalog semantic field.");
        }

        private static string? _Id3v2Txxx(Id3v2TagData? data, string description)
        {
            if (data is null)
                return null;

            foreach (var frame in data.Frames)
            {
                if (!string.Equals(frame.FrameId, "TXXX", StringComparison.Ordinal))
                    continue;

                if (!string.Equals(frame.Description, description, StringComparison.Ordinal))
                    continue;

                return frame.TextValues.Length == 0 ? null : frame.TextValues[0].TrimmedOrNull();
            }

            return null;
        }

        private static string? _Id3v2Singleton(Id3v2TagData? data, string frameId)
        {
            if (data is null)
                return null;

            foreach (var frame in data.Frames)
            {
                if (!string.Equals(frame.FrameId, frameId, StringComparison.Ordinal))
                    continue;

                return frame.TextValues.Length == 0 ? null : frame.TextValues[0].TrimmedOrNull();
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

                return DelimitedText.JoinOrNull(frame.TextValues);
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

            return primary.TextValues[0].TrimmedOrNull();
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

                return row.Values.Length == 0 ? null : row.Values[0].TrimmedOrNull();
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

                return DelimitedText.JoinOrNull(row.Values);
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
                    return row.Value.TrimmedOrNull();
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
                    return row.Value.TrimmedOrNull();
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

        private static (uint? Disc, uint? DiscCount) _ParseAsfPartOfSet(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return (null, null);

            var parts = text.Split('/');
            var disc = parts.Length >= 1 ? _ParseUInt(parts[0]) : null;
            var discCount = parts.Length >= 2 ? _ParseUInt(parts[1]) : null;
            return (disc, discCount);
        }

        private static string? _ReadId3v1Genre(Id3v1TagData? data)
        {
            if (data is null)
                return null;

            return Id3v1Genres.IndexToAudio(data.Genre).TrimmedOrNull();
        }

        private static string? _ReadApplePlainText(AppleTagData? apple, ReadOnlySpan<byte> atomType)
        {
            var values = _ReadAppleAtomValues(apple, atomType);
            return values.IsDefaultOrEmpty ? null : values[0].TrimmedOrNull();
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
            var day = _ReadApplePlainText(apple, AppleAtomIds.Day);
            return day is not null && uint.TryParse(day.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y) && y != 0
                ? y
                : null;
        }
    }
}
