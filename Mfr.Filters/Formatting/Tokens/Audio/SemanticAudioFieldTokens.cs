using Mfr.Models.Tags;

namespace Mfr.Filters.Formatting.Tokens.Audio
{
    /// <summary>
    /// Shared implementation for formatter tokens backed by <see cref="SemanticAudioField"/> projection.
    /// </summary>
    internal abstract class SemanticAudioFieldTokenBase(IReadOnlyList<string> names, SemanticAudioField field)
        : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureTagLibLoaded();
                var semantic = SemanticAudioTag.FromOverlay(item.Preview.AudioTagOverlay);
                return SemanticFields.Format(semantic, field);
            };
        }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Title", "Audio\\Tag", "Title from the audio tag overlay", "audio-title")]
    internal sealed class AudioTitleToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-title&gt;</c>.</summary>
        public AudioTitleToken()
            : base(["audio-title"], SemanticAudioField.Title) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Performers", "Audio\\Tag", "Performers from the audio tag overlay", "audio-artist")]
    internal sealed class AudioArtistToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-artist&gt;</c> (joined performers).</summary>
        public AudioArtistToken()
            : base(["audio-artist"], SemanticAudioField.Performers) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Album Artist", "Audio\\Tag", "Album artists from the audio tag overlay", "audio-album-artist")]
    internal sealed class AudioAlbumArtistToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-album-artist&gt;</c>.</summary>
        public AudioAlbumArtistToken()
            : base(["audio-album-artist"], SemanticAudioField.AlbumArtists) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Album", "Audio\\Tag", "Album from the audio tag overlay", "audio-album")]
    internal sealed class AudioAlbumToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-album&gt;</c>.</summary>
        public AudioAlbumToken()
            : base(["audio-album"], SemanticAudioField.Album) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Year", "Audio\\Tag", "Year from the audio tag overlay", "audio-year")]
    internal sealed class AudioYearToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-year&gt;</c>.</summary>
        public AudioYearToken()
            : base(["audio-year"], SemanticAudioField.Year) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Genres", "Audio\\Tag", "Genre from the audio tag overlay", "audio-genre")]
    internal sealed class AudioGenreToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-genre&gt;</c>.</summary>
        public AudioGenreToken()
            : base(["audio-genre"], SemanticAudioField.Genre) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Track", "Audio\\Tag", "Track number from the audio tag overlay", "audio-track")]
    internal sealed class AudioTrackToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-track&gt;</c>.</summary>
        public AudioTrackToken()
            : base(["audio-track"], SemanticAudioField.Track) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Track Count", "Audio\\Tag", "Track count from the audio tag overlay", "audio-track-count")]
    internal sealed class AudioTrackCountToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-track-count&gt;</c>.</summary>
        public AudioTrackCountToken()
            : base(["audio-track-count"], SemanticAudioField.TrackCount) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Disc", "Audio\\Tag", "Disc number from the audio tag overlay", "audio-disc")]
    internal sealed class AudioDiscToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-disc&gt;</c>.</summary>
        public AudioDiscToken()
            : base(["audio-disc"], SemanticAudioField.Disc) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Disc Count", "Audio\\Tag", "Disc count from the audio tag overlay", "audio-disc-count")]
    internal sealed class AudioDiscCountToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-disc-count&gt;</c>.</summary>
        public AudioDiscCountToken()
            : base(["audio-disc-count"], SemanticAudioField.DiscCount) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Comment", "Audio\\Tag", "Comment from the audio tag overlay", "audio-comment")]
    internal sealed class AudioCommentToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-comment&gt;</c>.</summary>
        public AudioCommentToken()
            : base(["audio-comment"], SemanticAudioField.Comment) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Composers", "Audio\\Tag", "Composers from the audio tag overlay", "audio-composer")]
    internal sealed class AudioComposerToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-composer&gt;</c>.</summary>
        public AudioComposerToken()
            : base(["audio-composer"], SemanticAudioField.Composers) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Lyrics", "Audio\\Tag", "Lyrics from the audio tag overlay", "audio-lyrics")]
    internal sealed class AudioLyricsToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-lyrics&gt;</c>.</summary>
        public AudioLyricsToken()
            : base(["audio-lyrics"], SemanticAudioField.Lyrics) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Copyright", "Audio\\Tag", "Copyright from the audio tag overlay", "audio-copyright")]
    internal sealed class AudioCopyrightToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-copyright&gt;</c>.</summary>
        public AudioCopyrightToken()
            : base(["audio-copyright"], SemanticAudioField.Copyright) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Grouping", "Audio\\Tag", "Grouping from the audio tag overlay", "audio-grouping")]
    internal sealed class AudioGroupingToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-grouping&gt;</c>.</summary>
        public AudioGroupingToken()
            : base(["audio-grouping"], SemanticAudioField.Grouping) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Beats Per Minute", "Audio\\Tag", "BPM from the audio tag overlay", "audio-bpm")]
    internal sealed class AudioBpmToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-bpm&gt;</c>.</summary>
        public AudioBpmToken()
            : base(["audio-bpm"], SemanticAudioField.BeatsPerMinute) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Conductor", "Audio\\Tag", "Conductor from the audio tag overlay", "audio-conductor")]
    internal sealed class AudioConductorToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-conductor&gt;</c>.</summary>
        public AudioConductorToken()
            : base(["audio-conductor"], SemanticAudioField.Conductor) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("MusicBrainz Artist Id", "Audio\\Tag", "MusicBrainz artist ID", "audio-mb-artist-id")]
    internal sealed class AudioMbArtistIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-artist-id&gt;</c>.</summary>
        public AudioMbArtistIdToken()
            : base(["audio-mb-artist-id"], SemanticAudioField.MusicBrainzArtistId) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("MusicBrainz Release Id", "Audio\\Tag", "MusicBrainz release ID", "audio-mb-release-id")]
    internal sealed class AudioMbReleaseIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-release-id&gt;</c>.</summary>
        public AudioMbReleaseIdToken()
            : base(["audio-mb-release-id"], SemanticAudioField.MusicBrainzReleaseId) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo(
        "MusicBrainz Release Artist Id",
        "Audio\\Tag",
        "MusicBrainz release artist ID",
        "audio-mb-release-artist-id"
    )]
    internal sealed class AudioMbReleaseArtistIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-release-artist-id&gt;</c>.</summary>
        public AudioMbReleaseArtistIdToken()
            : base(["audio-mb-release-artist-id"], SemanticAudioField.MusicBrainzReleaseArtistId) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("MusicBrainz Track Id", "Audio\\Tag", "MusicBrainz track ID", "audio-mb-track-id")]
    internal sealed class AudioMbTrackIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-track-id&gt;</c>.</summary>
        public AudioMbTrackIdToken()
            : base(["audio-mb-track-id"], SemanticAudioField.MusicBrainzTrackId) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("MusicBrainz Disc Id", "Audio\\Tag", "MusicBrainz disc ID", "audio-mb-disc-id")]
    internal sealed class AudioMbDiscIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-disc-id&gt;</c>.</summary>
        public AudioMbDiscIdToken()
            : base(["audio-mb-disc-id"], SemanticAudioField.MusicBrainzDiscId) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo(
        "MusicBrainz Release Status",
        "Audio\\Tag",
        "MusicBrainz release status",
        "audio-mb-release-status"
    )]
    internal sealed class AudioMbReleaseStatusToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-release-status&gt;</c>.</summary>
        public AudioMbReleaseStatusToken()
            : base(["audio-mb-release-status"], SemanticAudioField.MusicBrainzReleaseStatus) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("MusicBrainz Release Type", "Audio\\Tag", "MusicBrainz release type", "audio-mb-release-type")]
    internal sealed class AudioMbReleaseTypeToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-release-type&gt;</c>.</summary>
        public AudioMbReleaseTypeToken()
            : base(["audio-mb-release-type"], SemanticAudioField.MusicBrainzReleaseType) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo(
        "MusicBrainz Release Country",
        "Audio\\Tag",
        "MusicBrainz release country",
        "audio-mb-release-country"
    )]
    internal sealed class AudioMbReleaseCountryToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-release-country&gt;</c>.</summary>
        public AudioMbReleaseCountryToken()
            : base(["audio-mb-release-country"], SemanticAudioField.MusicBrainzReleaseCountry) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("MusicIP Id", "Audio\\Tag", "MusicIP PUID", "audio-musicip-id")]
    internal sealed class AudioMusicIpIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-musicip-id&gt;</c>.</summary>
        public AudioMusicIpIdToken()
            : base(["audio-musicip-id"], SemanticAudioField.MusicIpId) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Amazon Id", "Audio\\Tag", "Amazon ASIN", "audio-amazon-id")]
    internal sealed class AudioAmazonIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-amazon-id&gt;</c>.</summary>
        public AudioAmazonIdToken()
            : base(["audio-amazon-id"], SemanticAudioField.AmazonId) { }
    }
}
