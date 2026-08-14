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
                item.EnsureMediaPropertiesLoaded();
                return MpegAudioPropertiesFormatting.Format(item.Original.Media?.Mpeg, field);
            };
        }
    }

    /// <inheritdoc />
    internal sealed class MpegBitrateToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-bitrate&gt;</c>.</summary>
        public MpegBitrateToken()
            : base(["mpeg-bitrate"], MpegAudioPropertyField.Bitrate)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MpegCopyrightToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-copyright&gt;</c>.</summary>
        public MpegCopyrightToken()
            : base(["mpeg-copyright"], MpegAudioPropertyField.Copyright)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MpegDurationToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-duration&gt;</c>.</summary>
        public MpegDurationToken()
            : base(["mpeg-duration"], MpegAudioPropertyField.Duration)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MpegDurationSecToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-duration-sec&gt;</c>.</summary>
        public MpegDurationSecToken()
            : base(["mpeg-duration-sec"], MpegAudioPropertyField.DurationSec)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MpegEncodingToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-encoding&gt;</c>.</summary>
        public MpegEncodingToken()
            : base(["mpeg-encoding"], MpegAudioPropertyField.Encoding)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MpegFrequencyToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-frequency&gt;</c>.</summary>
        public MpegFrequencyToken()
            : base(["mpeg-frequency"], MpegAudioPropertyField.Frequency)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MpegLayerToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-layer&gt;</c>.</summary>
        public MpegLayerToken()
            : base(["mpeg-layer"], MpegAudioPropertyField.Layer)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MpegVerToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-ver&gt;</c>.</summary>
        public MpegVerToken()
            : base(["mpeg-ver"], MpegAudioPropertyField.MpegVer)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MpegModeToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-mode&gt;</c>.</summary>
        public MpegModeToken()
            : base(["mpeg-mode"], MpegAudioPropertyField.Mode)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MpegOriginalToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-original&gt;</c>.</summary>
        public MpegOriginalToken()
            : base(["mpeg-original"], MpegAudioPropertyField.Original)
        {
        }
    }

    /// <inheritdoc />
    internal sealed class MpegProtectionToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-protection&gt;</c>.</summary>
        public MpegProtectionToken()
            : base(["mpeg-protection"], MpegAudioPropertyField.Protection)
        {
        }
    }
}
