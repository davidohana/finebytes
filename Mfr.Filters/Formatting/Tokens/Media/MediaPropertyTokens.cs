namespace Mfr.Filters.Formatting.Tokens.Media
{
    /// <summary>
    /// Shared implementation for no-arg <c>media-*</c> formatter tokens.
    /// </summary>
    internal abstract class MediaPropertyTokenBase(IReadOnlyList<string> names, MediaPropertyField field) : IFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureMediaPropertiesLoaded();
                return MediaPropertiesFormatting.Format(item.Original.Media, field);
            };
        }
    }

    /// <inheritdoc />
    internal sealed class MediaMimeToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-mime&gt;</c>.</summary>
        public MediaMimeToken()
            : base(["media-mime"], MediaPropertyField.MimeType)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaCorruptToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-corrupt&gt;</c>.</summary>
        public MediaCorruptToken()
            : base(["media-corrupt"], MediaPropertyField.Corrupt)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaDurationToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-duration&gt;</c>.</summary>
        public MediaDurationToken()
            : base(["media-duration"], MediaPropertyField.Duration)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaDurationSecToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-duration-sec&gt;</c>.</summary>
        public MediaDurationSecToken()
            : base(["media-duration-sec"], MediaPropertyField.DurationSec)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaTypesToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-types&gt;</c>.</summary>
        public MediaTypesToken()
            : base(["media-types"], MediaPropertyField.MediaTypes)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaDescriptionToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-description&gt;</c>.</summary>
        public MediaDescriptionToken()
            : base(["media-description"], MediaPropertyField.Description)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaAudioBitrateToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-audio-bitrate&gt;</c>.</summary>
        public MediaAudioBitrateToken()
            : base(["media-audio-bitrate"], MediaPropertyField.AudioBitrate)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaSampleRateToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-samplerate&gt;</c>.</summary>
        public MediaSampleRateToken()
            : base(["media-samplerate"], MediaPropertyField.SampleRate)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaBitsPerSampleToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-bits-per-sample&gt;</c>.</summary>
        public MediaBitsPerSampleToken()
            : base(["media-bits-per-sample"], MediaPropertyField.BitsPerSample)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaChannelsToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-channels&gt;</c>.</summary>
        public MediaChannelsToken()
            : base(["media-channels"], MediaPropertyField.Channels)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaVideoWidthToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-video-width&gt;</c>.</summary>
        public MediaVideoWidthToken()
            : base(["media-video-width"], MediaPropertyField.VideoWidth)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaVideoHeightToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-video-height&gt;</c>.</summary>
        public MediaVideoHeightToken()
            : base(["media-video-height"], MediaPropertyField.VideoHeight)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaPhotoWidthToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-photo-width&gt;</c>.</summary>
        public MediaPhotoWidthToken()
            : base(["media-photo-width"], MediaPropertyField.PhotoWidth)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaPhotoHeightToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-photo-height&gt;</c>.</summary>
        public MediaPhotoHeightToken()
            : base(["media-photo-height"], MediaPropertyField.PhotoHeight)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MediaPhotoQualityToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-photo-quality&gt;</c>.</summary>
        public MediaPhotoQualityToken()
            : base(["media-photo-quality"], MediaPropertyField.PhotoQuality)
        {
        }
    }
}
