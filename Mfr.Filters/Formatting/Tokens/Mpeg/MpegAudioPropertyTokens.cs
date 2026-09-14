using System.Diagnostics;
using Mfr.Models.RenameList.Fields.Mpeg;

namespace Mfr.Filters.Formatting.Tokens.Mpeg
{
    /// <summary>
    /// Shared implementation for no-arg <c>mpeg-*</c> formatter tokens.
    /// </summary>
    internal abstract class MpegAudioPropertyTokenBase(
        IReadOnlyList<string> names,
        MpegAudioPropertyField propertyField
    ) : IFormatToken, IRenameListMappedFormatToken
    {
        /// <summary>
        /// Gets the MPEG audio property this token formats.
        /// </summary>
        internal MpegAudioPropertyField Field => propertyField;

        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = MpegRenameListFields.Group;
            propertyKey = propertyField switch
            {
                MpegAudioPropertyField.Bitrate => MpegRenameListFields.Key.Bitrate,
                MpegAudioPropertyField.Copyright => MpegRenameListFields.Key.Copyright,
                MpegAudioPropertyField.Duration => MpegRenameListFields.Key.Duration,
                MpegAudioPropertyField.DurationSec => MpegRenameListFields.Key.DurationSecs,
                MpegAudioPropertyField.Encoding => MpegRenameListFields.Key.VBR,
                MpegAudioPropertyField.Frequency => MpegRenameListFields.Key.Frequency,
                MpegAudioPropertyField.Layer => MpegRenameListFields.Key.Layer,
                MpegAudioPropertyField.MpegVer => MpegRenameListFields.Key.Level,
                MpegAudioPropertyField.Mode => MpegRenameListFields.Key.Mode,
                MpegAudioPropertyField.Original => MpegRenameListFields.Key.Original,
                MpegAudioPropertyField.Protection => MpegRenameListFields.Key.Protection,
                _ => throw new UnreachableException(),
            };
            return true;
        }

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureTagLibLoaded();
                return MpegAudioPropertiesFormatting.Format(item.Original.Media?.Mpeg, propertyField);
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
    [FormatTokenInfo("Copyright", "Audio\\MP3", MpegRenameListFieldTips.Copyright, "mpeg-copyright")]
    internal sealed class MpegCopyrightToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-copyright&gt;</c>.</summary>
        public MpegCopyrightToken()
            : base(["mpeg-copyright"], MpegAudioPropertyField.Copyright) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Duration", "Audio\\MP3", MpegRenameListFieldTips.Duration, "mpeg-duration")]
    internal sealed class MpegDurationToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-duration&gt;</c>.</summary>
        public MpegDurationToken()
            : base(["mpeg-duration"], MpegAudioPropertyField.Duration) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Duration Seconds", "Audio\\MP3", MpegRenameListFieldTips.DurationSecs, "mpeg-duration-sec")]
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
    [FormatTokenInfo("Frequency", "Audio\\MP3", MpegRenameListFieldTips.Frequency, "mpeg-frequency")]
    internal sealed class MpegFrequencyToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-frequency&gt;</c>.</summary>
        public MpegFrequencyToken()
            : base(["mpeg-frequency"], MpegAudioPropertyField.Frequency) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Layer", "Audio\\MP3", MpegRenameListFieldTips.Layer, "mpeg-layer")]
    internal sealed class MpegLayerToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-layer&gt;</c>.</summary>
        public MpegLayerToken()
            : base(["mpeg-layer"], MpegAudioPropertyField.Layer) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Version", "Audio\\MP3", MpegRenameListFieldTips.Level, "mpeg-ver")]
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
    [FormatTokenInfo("Original", "Audio\\MP3", MpegRenameListFieldTips.Original, "mpeg-original")]
    internal sealed class MpegOriginalToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-original&gt;</c>.</summary>
        public MpegOriginalToken()
            : base(["mpeg-original"], MpegAudioPropertyField.Original) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Protection", "Audio\\MP3", MpegRenameListFieldTips.Protection, "mpeg-protection")]
    internal sealed class MpegProtectionToken : MpegAudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mpeg-protection&gt;</c>.</summary>
        public MpegProtectionToken()
            : base(["mpeg-protection"], MpegAudioPropertyField.Protection) { }
    }
}
