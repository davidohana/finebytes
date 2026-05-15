using Mfr.Metadata;
using Mfr.Models.Tags;

namespace Mfr.Filters.Formatting.Tokens.Audio
{
    /// <summary>
    /// Shared implementation for formatter tokens backed by preview <see cref="AudioTagOverlay"/> using block-aware semantics.
    /// </summary>
    internal abstract class AudioOverlayTokenBase(IReadOnlyList<string> names, AudioOverlayField field) : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureAudioTagsLoaded();
                var semantic = AudioTagSemanticSurface.FromOverlay(item.Preview.AudioTagOverlay);
                return AudioOverlaySemanticFieldStrings.Format(semantic, field);
            };
        }
    }

    /// <inheritdoc />
    internal sealed class AudioTitleToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-title&gt;</c>.</summary>
        public AudioTitleToken()
            : base(["audio-title"], AudioOverlayField.Title)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioArtistToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-artist&gt;</c> (joined performers).</summary>
        public AudioArtistToken()
            : base(["audio-artist"], AudioOverlayField.Performers)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioAlbumArtistToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-album-artist&gt;</c>.</summary>
        public AudioAlbumArtistToken()
            : base(["audio-album-artist"], AudioOverlayField.AlbumArtists)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioAlbumToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-album&gt;</c>.</summary>
        public AudioAlbumToken()
            : base(["audio-album"], AudioOverlayField.Album)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioYearToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-year&gt;</c>.</summary>
        public AudioYearToken()
            : base(["audio-year"], AudioOverlayField.Year)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioGenreToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-genre&gt;</c>.</summary>
        public AudioGenreToken()
            : base(["audio-genre"], AudioOverlayField.Genre)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioTrackToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-track&gt;</c>.</summary>
        public AudioTrackToken()
            : base(["audio-track"], AudioOverlayField.Track)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioTrackCountToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-track-count&gt;</c>.</summary>
        public AudioTrackCountToken()
            : base(["audio-track-count"], AudioOverlayField.TrackCount)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioDiscToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-disc&gt;</c>.</summary>
        public AudioDiscToken()
            : base(["audio-disc"], AudioOverlayField.Disc)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioDiscCountToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-disc-count&gt;</c>.</summary>
        public AudioDiscCountToken()
            : base(["audio-disc-count"], AudioOverlayField.DiscCount)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioCommentToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-comment&gt;</c>.</summary>
        public AudioCommentToken()
            : base(["audio-comment"], AudioOverlayField.Comment)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioComposerToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-composer&gt;</c>.</summary>
        public AudioComposerToken()
            : base(["audio-composer"], AudioOverlayField.Composers)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioLyricsToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-lyrics&gt;</c>.</summary>
        public AudioLyricsToken()
            : base(["audio-lyrics"], AudioOverlayField.Lyrics)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioCopyrightToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-copyright&gt;</c>.</summary>
        public AudioCopyrightToken()
            : base(["audio-copyright"], AudioOverlayField.Copyright)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioGroupingToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-grouping&gt;</c>.</summary>
        public AudioGroupingToken()
            : base(["audio-grouping"], AudioOverlayField.Grouping)
        {
        }
    }
}
