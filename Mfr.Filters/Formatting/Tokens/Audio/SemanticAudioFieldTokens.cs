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
    internal sealed class AudioTitleToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-title&gt;</c>.</summary>
        public AudioTitleToken()
            : base(["audio-title"], SemanticAudioField.Title) { }
    }

    /// <inheritdoc />
    internal sealed class AudioArtistToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-artist&gt;</c> (joined performers).</summary>
        public AudioArtistToken()
            : base(["audio-artist"], SemanticAudioField.Performers) { }
    }

    /// <inheritdoc />
    internal sealed class AudioAlbumArtistToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-album-artist&gt;</c>.</summary>
        public AudioAlbumArtistToken()
            : base(["audio-album-artist"], SemanticAudioField.AlbumArtists) { }
    }

    /// <inheritdoc />
    internal sealed class AudioAlbumToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-album&gt;</c>.</summary>
        public AudioAlbumToken()
            : base(["audio-album"], SemanticAudioField.Album) { }
    }

    /// <inheritdoc />
    internal sealed class AudioYearToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-year&gt;</c>.</summary>
        public AudioYearToken()
            : base(["audio-year"], SemanticAudioField.Year) { }
    }

    /// <inheritdoc />
    internal sealed class AudioGenreToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-genre&gt;</c>.</summary>
        public AudioGenreToken()
            : base(["audio-genre"], SemanticAudioField.Genre) { }
    }

    /// <inheritdoc />
    internal sealed class AudioTrackToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-track&gt;</c>.</summary>
        public AudioTrackToken()
            : base(["audio-track"], SemanticAudioField.Track) { }
    }

    /// <inheritdoc />
    internal sealed class AudioTrackCountToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-track-count&gt;</c>.</summary>
        public AudioTrackCountToken()
            : base(["audio-track-count"], SemanticAudioField.TrackCount) { }
    }

    /// <inheritdoc />
    internal sealed class AudioDiscToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-disc&gt;</c>.</summary>
        public AudioDiscToken()
            : base(["audio-disc"], SemanticAudioField.Disc) { }
    }

    /// <inheritdoc />
    internal sealed class AudioDiscCountToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-disc-count&gt;</c>.</summary>
        public AudioDiscCountToken()
            : base(["audio-disc-count"], SemanticAudioField.DiscCount) { }
    }

    /// <inheritdoc />
    internal sealed class AudioCommentToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-comment&gt;</c>.</summary>
        public AudioCommentToken()
            : base(["audio-comment"], SemanticAudioField.Comment) { }
    }

    /// <inheritdoc />
    internal sealed class AudioComposerToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-composer&gt;</c>.</summary>
        public AudioComposerToken()
            : base(["audio-composer"], SemanticAudioField.Composers) { }
    }

    /// <inheritdoc />
    internal sealed class AudioLyricsToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-lyrics&gt;</c>.</summary>
        public AudioLyricsToken()
            : base(["audio-lyrics"], SemanticAudioField.Lyrics) { }
    }

    /// <inheritdoc />
    internal sealed class AudioCopyrightToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-copyright&gt;</c>.</summary>
        public AudioCopyrightToken()
            : base(["audio-copyright"], SemanticAudioField.Copyright) { }
    }

    /// <inheritdoc />
    internal sealed class AudioGroupingToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-grouping&gt;</c>.</summary>
        public AudioGroupingToken()
            : base(["audio-grouping"], SemanticAudioField.Grouping) { }
    }

    /// <inheritdoc />
    internal sealed class AudioBpmToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-bpm&gt;</c>.</summary>
        public AudioBpmToken()
            : base(["audio-bpm"], SemanticAudioField.BeatsPerMinute) { }
    }

    /// <inheritdoc />
    internal sealed class AudioConductorToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-conductor&gt;</c>.</summary>
        public AudioConductorToken()
            : base(["audio-conductor"], SemanticAudioField.Conductor) { }
    }

    /// <inheritdoc />
    internal sealed class AudioMbArtistIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-artist-id&gt;</c>.</summary>
        public AudioMbArtistIdToken()
            : base(["audio-mb-artist-id"], SemanticAudioField.MusicBrainzArtistId) { }
    }

    /// <inheritdoc />
    internal sealed class AudioMbReleaseIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-release-id&gt;</c>.</summary>
        public AudioMbReleaseIdToken()
            : base(["audio-mb-release-id"], SemanticAudioField.MusicBrainzReleaseId) { }
    }

    /// <inheritdoc />
    internal sealed class AudioMbReleaseArtistIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-release-artist-id&gt;</c>.</summary>
        public AudioMbReleaseArtistIdToken()
            : base(["audio-mb-release-artist-id"], SemanticAudioField.MusicBrainzReleaseArtistId) { }
    }

    /// <inheritdoc />
    internal sealed class AudioMbTrackIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-track-id&gt;</c>.</summary>
        public AudioMbTrackIdToken()
            : base(["audio-mb-track-id"], SemanticAudioField.MusicBrainzTrackId) { }
    }

    /// <inheritdoc />
    internal sealed class AudioMbDiscIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-disc-id&gt;</c>.</summary>
        public AudioMbDiscIdToken()
            : base(["audio-mb-disc-id"], SemanticAudioField.MusicBrainzDiscId) { }
    }

    /// <inheritdoc />
    internal sealed class AudioMbReleaseStatusToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-release-status&gt;</c>.</summary>
        public AudioMbReleaseStatusToken()
            : base(["audio-mb-release-status"], SemanticAudioField.MusicBrainzReleaseStatus) { }
    }

    /// <inheritdoc />
    internal sealed class AudioMbReleaseTypeToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-release-type&gt;</c>.</summary>
        public AudioMbReleaseTypeToken()
            : base(["audio-mb-release-type"], SemanticAudioField.MusicBrainzReleaseType) { }
    }

    /// <inheritdoc />
    internal sealed class AudioMbReleaseCountryToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-mb-release-country&gt;</c>.</summary>
        public AudioMbReleaseCountryToken()
            : base(["audio-mb-release-country"], SemanticAudioField.MusicBrainzReleaseCountry) { }
    }

    /// <inheritdoc />
    internal sealed class AudioMusicIpIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-musicip-id&gt;</c>.</summary>
        public AudioMusicIpIdToken()
            : base(["audio-musicip-id"], SemanticAudioField.MusicIpId) { }
    }

    /// <inheritdoc />
    internal sealed class AudioAmazonIdToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-amazon-id&gt;</c>.</summary>
        public AudioAmazonIdToken()
            : base(["audio-amazon-id"], SemanticAudioField.AmazonId) { }
    }
}
