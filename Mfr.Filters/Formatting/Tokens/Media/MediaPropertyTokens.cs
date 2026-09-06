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
                item.EnsureTagLibLoaded();
                return MediaPropertiesFormatting.Format(item.Original.Media, field);
            };
        }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Mime Type", "Media", "MIME type from TagLib media properties", "media-mime")]
    internal sealed class MediaMimeToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-mime&gt;</c>.</summary>
        public MediaMimeToken()
            : base(["media-mime"], MediaPropertyField.MimeType) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo(
        "Possibly Corrupt",
        "Media",
        "Whether TagLib reports the file as possibly corrupt",
        "media-corrupt"
    )]
    internal sealed class MediaCorruptToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-corrupt&gt;</c>.</summary>
        public MediaCorruptToken()
            : base(["media-corrupt"], MediaPropertyField.Corrupt) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Duration", "Media", "Duration as h:mm:ss", "media-duration")]
    internal sealed class MediaDurationToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-duration&gt;</c>.</summary>
        public MediaDurationToken()
            : base(["media-duration"], MediaPropertyField.Duration) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Duration Seconds", "Media", "Duration in whole seconds", "media-duration-sec")]
    internal sealed class MediaDurationSecToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-duration-sec&gt;</c>.</summary>
        public MediaDurationSecToken()
            : base(["media-duration-sec"], MediaPropertyField.DurationSec) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Media Types", "Media", "TagLib media-type flags text", "media-types")]
    internal sealed class MediaTypesToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-types&gt;</c>.</summary>
        public MediaTypesToken()
            : base(["media-types"], MediaPropertyField.MediaTypes) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Description", "Media", "Codec description", "media-description")]
    internal sealed class MediaDescriptionToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-description&gt;</c>.</summary>
        public MediaDescriptionToken()
            : base(["media-description"], MediaPropertyField.Description) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Audio Bitrate", "Media", "Audio bitrate in kbps", "media-audio-bitrate")]
    internal sealed class MediaAudioBitrateToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-audio-bitrate&gt;</c>.</summary>
        public MediaAudioBitrateToken()
            : base(["media-audio-bitrate"], MediaPropertyField.AudioBitrate) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Sample Rate", "Media", "Audio sample rate in Hz", "media-samplerate")]
    internal sealed class MediaSampleRateToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-samplerate&gt;</c>.</summary>
        public MediaSampleRateToken()
            : base(["media-samplerate"], MediaPropertyField.SampleRate) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Bits Per Sample", "Media", "Bits per sample", "media-bits-per-sample")]
    internal sealed class MediaBitsPerSampleToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-bits-per-sample&gt;</c>.</summary>
        public MediaBitsPerSampleToken()
            : base(["media-bits-per-sample"], MediaPropertyField.BitsPerSample) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Audio Channels", "Media", "Channel count", "media-channels")]
    internal sealed class MediaChannelsToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-channels&gt;</c>.</summary>
        public MediaChannelsToken()
            : base(["media-channels"], MediaPropertyField.Channels) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Video Width", "Media", "Video width in pixels", "media-video-width")]
    internal sealed class MediaVideoWidthToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-video-width&gt;</c>.</summary>
        public MediaVideoWidthToken()
            : base(["media-video-width"], MediaPropertyField.VideoWidth) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Video Height", "Media", "Video height in pixels", "media-video-height")]
    internal sealed class MediaVideoHeightToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-video-height&gt;</c>.</summary>
        public MediaVideoHeightToken()
            : base(["media-video-height"], MediaPropertyField.VideoHeight) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Photo Width", "Media", "Photo width in pixels", "media-photo-width")]
    internal sealed class MediaPhotoWidthToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-photo-width&gt;</c>.</summary>
        public MediaPhotoWidthToken()
            : base(["media-photo-width"], MediaPropertyField.PhotoWidth) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Photo Height", "Media", "Photo height in pixels", "media-photo-height")]
    internal sealed class MediaPhotoHeightToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-photo-height&gt;</c>.</summary>
        public MediaPhotoHeightToken()
            : base(["media-photo-height"], MediaPropertyField.PhotoHeight) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Photo Quality", "Media", "Photo quality value", "media-photo-quality")]
    internal sealed class MediaPhotoQualityToken : MediaPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;media-photo-quality&gt;</c>.</summary>
        public MediaPhotoQualityToken()
            : base(["media-photo-quality"], MediaPropertyField.PhotoQuality) { }
    }
}
