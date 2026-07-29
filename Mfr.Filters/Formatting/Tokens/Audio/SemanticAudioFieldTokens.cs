using Mfr.Models.Tags;

namespace Mfr.Filters.Formatting.Tokens.Audio
{
    /// <summary>
    /// Shared implementation for formatter tokens backed by <see cref="SemanticAudioField"/> projection.
    /// </summary>
    internal abstract class SemanticAudioFieldTokenBase(IReadOnlyList<string> names, SemanticAudioField field) : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureEmbeddedTagsLoaded();
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
            : base(["audio-title"], SemanticAudioField.Title)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioArtistToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-artist&gt;</c> (joined performers).</summary>
        public AudioArtistToken()
            : base(["audio-artist"], SemanticAudioField.Performers)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioAlbumArtistToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-album-artist&gt;</c>.</summary>
        public AudioAlbumArtistToken()
            : base(["audio-album-artist"], SemanticAudioField.AlbumArtists)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioAlbumToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-album&gt;</c>.</summary>
        public AudioAlbumToken()
            : base(["audio-album"], SemanticAudioField.Album)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioYearToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-year&gt;</c>.</summary>
        public AudioYearToken()
            : base(["audio-year"], SemanticAudioField.Year)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioGenreToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-genre&gt;</c>.</summary>
        public AudioGenreToken()
            : base(["audio-genre"], SemanticAudioField.Genre)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioTrackToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-track&gt;</c>.</summary>
        public AudioTrackToken()
            : base(["audio-track"], SemanticAudioField.Track)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioTrackCountToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-track-count&gt;</c>.</summary>
        public AudioTrackCountToken()
            : base(["audio-track-count"], SemanticAudioField.TrackCount)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioDiscToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-disc&gt;</c>.</summary>
        public AudioDiscToken()
            : base(["audio-disc"], SemanticAudioField.Disc)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioDiscCountToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-disc-count&gt;</c>.</summary>
        public AudioDiscCountToken()
            : base(["audio-disc-count"], SemanticAudioField.DiscCount)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioCommentToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-comment&gt;</c>.</summary>
        public AudioCommentToken()
            : base(["audio-comment"], SemanticAudioField.Comment)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioComposerToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-composer&gt;</c>.</summary>
        public AudioComposerToken()
            : base(["audio-composer"], SemanticAudioField.Composers)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioLyricsToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-lyrics&gt;</c>.</summary>
        public AudioLyricsToken()
            : base(["audio-lyrics"], SemanticAudioField.Lyrics)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioCopyrightToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-copyright&gt;</c>.</summary>
        public AudioCopyrightToken()
            : base(["audio-copyright"], SemanticAudioField.Copyright)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioGroupingToken : SemanticAudioFieldTokenBase
    {
        /// <summary>Registers <c>&lt;audio-grouping&gt;</c>.</summary>
        public AudioGroupingToken()
            : base(["audio-grouping"], SemanticAudioField.Grouping)
        {
        }
    }
}
