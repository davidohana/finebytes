using System.Globalization;
using Mfr.Models;

namespace Mfr.Filters.Formatting.Tokens.Audio
{
    /// <summary>
    /// Shared implementation for formatter tokens backed by preview <see cref="AudioTagOverlay"/>.
    /// </summary>
    internal abstract class AudioOverlayTokenBase(
        IReadOnlyList<string> names,
        Func<AudioTagOverlay, string> resolvePreview) : IFormatToken
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
                return resolvePreview(item.Preview.AudioTagOverlay);
            };
        }

        internal static string InvariantUintOrEmpty(uint? value)
        {
            return value is null ? string.Empty : value.Value.ToString(CultureInfo.InvariantCulture);
        }

        internal static string NullSafeString(string? value)
        {
            return value ?? string.Empty;
        }
    }

    /// <inheritdoc />
    internal sealed class AudioTitleToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-title&gt;</c>.</summary>
        public AudioTitleToken()
            : base(["audio-title"], tags => NullSafeString(tags.Title))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioArtistToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-artist&gt;</c> (joined performers).</summary>
        public AudioArtistToken()
            : base(["audio-artist"], tags => NullSafeString(tags.Performers))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioAlbumArtistToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-album-artist&gt;</c>.</summary>
        public AudioAlbumArtistToken()
            : base(["audio-album-artist"], tags => NullSafeString(tags.AlbumArtists))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioAlbumToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-album&gt;</c>.</summary>
        public AudioAlbumToken()
            : base(["audio-album"], tags => NullSafeString(tags.Album))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioYearToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-year&gt;</c>.</summary>
        public AudioYearToken()
            : base(["audio-year"], tags => InvariantUintOrEmpty(tags.Year))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioGenreToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-genre&gt;</c>.</summary>
        public AudioGenreToken()
            : base(["audio-genre"], tags => NullSafeString(tags.Genre))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioTrackToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-track&gt;</c>.</summary>
        public AudioTrackToken()
            : base(["audio-track"], tags => InvariantUintOrEmpty(tags.Track))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioTrackCountToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-track-count&gt;</c>.</summary>
        public AudioTrackCountToken()
            : base(["audio-track-count"], tags => InvariantUintOrEmpty(tags.TrackCount))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioDiscToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-disc&gt;</c>.</summary>
        public AudioDiscToken()
            : base(["audio-disc"], tags => InvariantUintOrEmpty(tags.Disc))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioDiscCountToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-disc-count&gt;</c>.</summary>
        public AudioDiscCountToken()
            : base(["audio-disc-count"], tags => InvariantUintOrEmpty(tags.DiscCount))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioCommentToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-comment&gt;</c>.</summary>
        public AudioCommentToken()
            : base(["audio-comment"], tags => NullSafeString(tags.Comment))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioComposerToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-composer&gt;</c>.</summary>
        public AudioComposerToken()
            : base(["audio-composer"], tags => NullSafeString(tags.Composers))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioLyricsToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-lyrics&gt;</c>.</summary>
        public AudioLyricsToken()
            : base(["audio-lyrics"], tags => NullSafeString(tags.Lyrics))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioCopyrightToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-copyright&gt;</c>.</summary>
        public AudioCopyrightToken()
            : base(["audio-copyright"], tags => NullSafeString(tags.Copyright))
        {
        }
    }

    /// <inheritdoc />
    internal sealed class AudioGroupingToken : AudioOverlayTokenBase
    {
        /// <summary>Registers <c>&lt;audio-grouping&gt;</c>.</summary>
        public AudioGroupingToken()
            : base(["audio-grouping"], tags => NullSafeString(tags.Grouping))
        {
        }
    }
}
