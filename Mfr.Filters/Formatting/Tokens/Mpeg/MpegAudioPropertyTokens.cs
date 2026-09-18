using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Mpeg;

namespace Mfr.Filters.Formatting.Tokens.Mpeg
{
    /// <summary>
    /// Shared implementation for no-arg <c>mp3-*</c> formatter tokens.
    /// </summary>
    internal abstract class MpegAudioPropertyTokenBase(
        IReadOnlyList<string> names,
        MpegAudioPropertyField propertyField
    ) : IFormatToken, IRenameListMappedFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = MpegRenameListFields.Group;
            propertyKey = MpegPropertyRenameListField.CatalogPropertyKey(propertyField);
            return true;
        }

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureTagLibLoaded();
                return MpegAudioPropertiesFormatting.Format(
                    item.Original.Media?.Mpeg,
                    propertyField,
                    PropertyDisplayContext.Token
                );
            };
        }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Bitrate", "Audio\\MP3", "Bitrate of MPEG audio (VBR-prefixed when applicable)", "mp3-bitrate")]
    internal sealed class MpegBitrateToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-bitrate&gt;</c>.</summary>
        public MpegBitrateToken()
            : base(["mp3-bitrate"], MpegAudioPropertyField.Bitrate) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Copyright", "Audio\\MP3", MpegRenameListFieldTips.Copyright, "mp3-copyright")]
    internal sealed class MpegCopyrightToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-copyright&gt;</c>.</summary>
        public MpegCopyrightToken()
            : base(["mp3-copyright"], MpegAudioPropertyField.Copyright) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Duration", "Audio\\MP3", MpegRenameListFieldTips.Duration, "mp3-duration")]
    internal sealed class MpegDurationToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-duration&gt;</c>.</summary>
        public MpegDurationToken()
            : base(["mp3-duration"], MpegAudioPropertyField.Duration) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Duration Seconds", "Audio\\MP3", MpegRenameListFieldTips.DurationSecs, "mp3-duration-sec")]
    internal sealed class MpegDurationSecToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-duration-sec&gt;</c>.</summary>
        public MpegDurationSecToken()
            : base(["mp3-duration-sec"], MpegAudioPropertyField.DurationSec) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Encoding", "Audio\\MP3", "CBR or VBR encoding", "mp3-encoding")]
    internal sealed class MpegEncodingToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-encoding&gt;</c>.</summary>
        public MpegEncodingToken()
            : base(["mp3-encoding"], MpegAudioPropertyField.Encoding) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Frequency", "Audio\\MP3", MpegRenameListFieldTips.Frequency, "mp3-frequency")]
    internal sealed class MpegFrequencyToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-frequency&gt;</c>.</summary>
        public MpegFrequencyToken()
            : base(["mp3-frequency"], MpegAudioPropertyField.Frequency) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Layer", "Audio\\MP3", MpegRenameListFieldTips.Layer, "mp3-layer")]
    internal sealed class MpegLayerToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-layer&gt;</c>.</summary>
        public MpegLayerToken()
            : base(["mp3-layer"], MpegAudioPropertyField.Layer) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Version", "Audio\\MP3", MpegRenameListFieldTips.Level, "mp3-ver")]
    internal sealed class MpegVerToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-ver&gt;</c>.</summary>
        public MpegVerToken()
            : base(["mp3-ver"], MpegAudioPropertyField.MpegVer) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Mode", "Audio\\MP3", "Channel mode", "mp3-mode")]
    internal sealed class MpegModeToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-mode&gt;</c>.</summary>
        public MpegModeToken()
            : base(["mp3-mode"], MpegAudioPropertyField.Mode) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Original", "Audio\\MP3", MpegRenameListFieldTips.Original, "mp3-original")]
    internal sealed class MpegOriginalToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-original&gt;</c>.</summary>
        public MpegOriginalToken()
            : base(["mp3-original"], MpegAudioPropertyField.Original) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Protection", "Audio\\MP3", MpegRenameListFieldTips.Protection, "mp3-protection")]
    internal sealed class MpegProtectionToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-protection&gt;</c>.</summary>
        public MpegProtectionToken()
            : base(["mp3-protection"], MpegAudioPropertyField.Protection) { }
    }
}
