using Mfr.Models.RenameList;
using Mfr.Models.RenameList.Fields.Mp3;

namespace Mfr.Filters.Formatting.Tokens.Mp3
{
    /// <summary>
    /// Shared implementation for no-arg <c>mp3-*</c> formatter tokens.
    /// </summary>
    internal abstract class Mp3AudioPropertyTokenBase(IReadOnlyList<string> names, Mp3AudioPropertyField propertyField)
        : IFormatToken,
            IRenameListMappedFormatToken
    {
        /// <inheritdoc />
        public IReadOnlyList<string> Names => names;

        /// <inheritdoc />
        public bool TryGetFixedField(out string groupId, out string propertyKey)
        {
            groupId = Mp3RenameListFields.Group;
            propertyKey = Mp3PropertyRenameListField.CatalogPropertyKey(propertyField);
            return true;
        }

        /// <inheritdoc />
        public Formatter Compile(string tokenArgs)
        {
            FormatOptionsParsing.RequireNoArgument(tokenArgs, FormatOptionsParsing.TokenDisplayName(this));

            return item =>
            {
                item.EnsureTagLibLoaded();
                return Mp3AudioPropertiesFormatting.Format(
                    item.Original.Media?.Mp3,
                    propertyField,
                    PropertyDisplayContext.Token
                );
            };
        }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Bitrate", "Audio\\MP3", "Bitrate of MPEG audio (VBR-prefixed when applicable)", "mp3-bitrate")]
    internal sealed class Mp3BitrateToken : Mp3AudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-bitrate&gt;</c>.</summary>
        public Mp3BitrateToken()
            : base(["mp3-bitrate"], Mp3AudioPropertyField.Bitrate) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Copyright", "Audio\\MP3", Mp3RenameListFieldTips.Copyright, "mp3-copyright")]
    internal sealed class Mp3CopyrightToken : Mp3AudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-copyright&gt;</c>.</summary>
        public Mp3CopyrightToken()
            : base(["mp3-copyright"], Mp3AudioPropertyField.Copyright) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Duration", "Audio\\MP3", Mp3RenameListFieldTips.Duration, "mp3-duration")]
    internal sealed class Mp3DurationToken : Mp3AudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-duration&gt;</c>.</summary>
        public Mp3DurationToken()
            : base(["mp3-duration"], Mp3AudioPropertyField.Duration) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Duration Seconds", "Audio\\MP3", Mp3RenameListFieldTips.DurationSecs, "mp3-duration-sec")]
    internal sealed class Mp3DurationSecToken : Mp3AudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-duration-sec&gt;</c>.</summary>
        public Mp3DurationSecToken()
            : base(["mp3-duration-sec"], Mp3AudioPropertyField.DurationSec) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Encoding", "Audio\\MP3", "CBR or VBR encoding", "mp3-encoding")]
    internal sealed class Mp3EncodingToken : Mp3AudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-encoding&gt;</c>.</summary>
        public Mp3EncodingToken()
            : base(["mp3-encoding"], Mp3AudioPropertyField.Encoding) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Frequency", "Audio\\MP3", Mp3RenameListFieldTips.Frequency, "mp3-frequency")]
    internal sealed class Mp3FrequencyToken : Mp3AudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-frequency&gt;</c>.</summary>
        public Mp3FrequencyToken()
            : base(["mp3-frequency"], Mp3AudioPropertyField.Frequency) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Layer", "Audio\\MP3", Mp3RenameListFieldTips.Layer, "mp3-layer")]
    internal sealed class Mp3LayerToken : Mp3AudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-layer&gt;</c>.</summary>
        public Mp3LayerToken()
            : base(["mp3-layer"], Mp3AudioPropertyField.Layer) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Version", "Audio\\MP3", Mp3RenameListFieldTips.Level, "mp3-ver")]
    internal sealed class Mp3VerToken : Mp3AudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-ver&gt;</c>.</summary>
        public Mp3VerToken()
            : base(["mp3-ver"], Mp3AudioPropertyField.Ver) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Mode", "Audio\\MP3", "Channel mode", "mp3-mode")]
    internal sealed class Mp3ModeToken : Mp3AudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-mode&gt;</c>.</summary>
        public Mp3ModeToken()
            : base(["mp3-mode"], Mp3AudioPropertyField.Mode) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Original", "Audio\\MP3", Mp3RenameListFieldTips.Original, "mp3-original")]
    internal sealed class Mp3OriginalToken : Mp3AudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-original&gt;</c>.</summary>
        public Mp3OriginalToken()
            : base(["mp3-original"], Mp3AudioPropertyField.Original) { }
    }

    /// <inheritdoc />
    [FormatTokenInfo("Protection", "Audio\\MP3", Mp3RenameListFieldTips.Protection, "mp3-protection")]
    internal sealed class Mp3ProtectionToken : Mp3AudioPropertyTokenBase
    {
        /// <summary>Registers <c>&lt;mp3-protection&gt;</c>.</summary>
        public Mp3ProtectionToken()
            : base(["mp3-protection"], Mp3AudioPropertyField.Protection) { }
    }
}
