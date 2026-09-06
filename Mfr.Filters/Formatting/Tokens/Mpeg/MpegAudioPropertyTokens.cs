namespace Mfr.Filters.Formatting.Tokens.Mpeg
{
    /// <summary>
    /// Shared implementation for no-arg <c>mpeg-*</c> formatter tokens.
    /// </summary>
    internal abstract class MpegAudioPropertyTokenBase(IReadOnlyList<string> names, MpegAudioPropertyField field)
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
                return MpegAudioPropertiesFormatting.Format(item.Original.Media?.Mpeg, field);
            };
        }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Bitrate", "Audio\\MP3", "Bitrate of MPEG audio (VBR-prefixed when applicable)", "mpeg-bitrate")]
    internal sealed class MpegBitrateToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-bitrate&gt;</c>.</summary>
        public MpegBitrateToken()
            : base(["mpeg-bitrate"], MpegAudioPropertyField.Bitrate) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Copyright", "Audio\\MP3", "Whether the MPEG audio is copyrighted", "mpeg-copyright")]
    internal sealed class MpegCopyrightToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-copyright&gt;</c>.</summary>
        public MpegCopyrightToken()
            : base(["mpeg-copyright"], MpegAudioPropertyField.Copyright) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Duration", "Audio\\MP3", "MPEG header duration as h:mm:ss", "mpeg-duration")]
    internal sealed class MpegDurationToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-duration&gt;</c>.</summary>
        public MpegDurationToken()
            : base(["mpeg-duration"], MpegAudioPropertyField.Duration) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Duration Seconds", "Audio\\MP3", "MPEG header duration in whole seconds", "mpeg-duration-sec")]
    internal sealed class MpegDurationSecToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-duration-sec&gt;</c>.</summary>
        public MpegDurationSecToken()
            : base(["mpeg-duration-sec"], MpegAudioPropertyField.DurationSec) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Encoding", "Audio\\MP3", "CBR or VBR encoding", "mpeg-encoding")]
    internal sealed class MpegEncodingToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-encoding&gt;</c>.</summary>
        public MpegEncodingToken()
            : base(["mpeg-encoding"], MpegAudioPropertyField.Encoding) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Frequency", "Audio\\MP3", "Sample rate (Hz) from MPEG header", "mpeg-frequency")]
    internal sealed class MpegFrequencyToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-frequency&gt;</c>.</summary>
        public MpegFrequencyToken()
            : base(["mpeg-frequency"], MpegAudioPropertyField.Frequency) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Layer", "Audio\\MP3", "MPEG layer (I / II / III)", "mpeg-layer")]
    internal sealed class MpegLayerToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-layer&gt;</c>.</summary>
        public MpegLayerToken()
            : base(["mpeg-layer"], MpegAudioPropertyField.Layer) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Version", "Audio\\MP3", "MPEG version (1 / 2 / 2.5)", "mpeg-ver")]
    internal sealed class MpegVerToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-ver&gt;</c>.</summary>
        public MpegVerToken()
            : base(["mpeg-ver"], MpegAudioPropertyField.MpegVer) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Mode", "Audio\\MP3", "Channel mode", "mpeg-mode")]
    internal sealed class MpegModeToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-mode&gt;</c>.</summary>
        public MpegModeToken()
            : base(["mpeg-mode"], MpegAudioPropertyField.Mode) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Original", "Audio\\MP3", "Whether the MPEG original bit is set", "mpeg-original")]
    internal sealed class MpegOriginalToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-original&gt;</c>.</summary>
        public MpegOriginalToken()
            : base(["mpeg-original"], MpegAudioPropertyField.Original) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Protection", "Audio\\MP3", "Whether CRC protection is set", "mpeg-protection")]
    internal sealed class MpegProtectionToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-protection&gt;</c>.</summary>
        public MpegProtectionToken()
            : base(["mpeg-protection"], MpegAudioPropertyField.Protection) { }
    }
}
